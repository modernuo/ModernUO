using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Server.Engines.AdvancedSearch;

/// <summary>
/// Decides from a getter's IL whether it only reads state, so a search worker may call it off the loop.
/// A "no" is always safe (the read stays on the loop); a "yes" must never be wrong.
/// </summary>
/// <remarks>
/// Known accepted gap: the first <c>ldsfld</c> of a type can run its static constructor. The server has
/// run them all long before any search, so this is documented rather than handled.
/// </remarks>
public static class PureGetterClassifier
{
    private const BindingFlags DeclaredMethods = BindingFlags.Public | BindingFlags.NonPublic |
                                                 BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static readonly Dictionary<short, OpCode> _opCodes = BuildOpCodes();

    private static readonly ConcurrentDictionary<(RuntimeMethodHandle, Type), bool> _memo = new();
    // Built once per declaring type: the game assemblies are all loaded at startup, before any search.
    private static readonly ConcurrentDictionary<Type, Type[]> _derivedTypes = new();

    // An unhandled StackOverflow on a worker thread kills the process, so deep call chains are rejected.
    private const int MaxDepth = 64;

    // Per-thread so concurrent search workers never see each other's in-progress methods.
    [ThreadStatic]
    private static Dictionary<(RuntimeMethodHandle, Type), int> _inProgress;

    public static bool IsPure(MethodInfo method, Type receiverType)
    {
        if (method == null)
        {
            return false;
        }

        var target = receiverType != null ? ResolveOverride(method, receiverType) : method;
        if (target == null)
        {
            return false;
        }

        var low = int.MaxValue;
        return Classify(target, receiverType, ref low);
    }

    public static bool IsPure(PropertyInfo[] chain, Type runtimeType)
    {
        if (chain == null || chain.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < chain.Length; i++)
        {
            var getter = chain[i].GetGetMethod(true);
            if (getter == null)
            {
                return false;
            }

            var low = int.MaxValue;
            bool pure;
            if (i == 0)
            {
                var resolved = ResolveOverride(getter, runtimeType);
                pure = resolved != null && Classify(resolved, runtimeType, ref low);
            }
            else if (IsOverridable(getter))
            {
                pure = AllOverridesPure(getter, ref low);
            }
            else
            {
                pure = Classify(getter, null, ref low);
            }

            if (!pure)
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<short, OpCode> BuildOpCodes()
    {
        var table = new Dictionary<short, OpCode>();
        var fields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
        for (var i = 0; i < fields.Length; i++)
        {
            if (fields[i].GetValue(null) is OpCode op)
            {
                table[op.Value] = op;
            }
        }

        return table;
    }

    private static bool IsOverridable(MethodInfo m) =>
        m.IsVirtual && !m.IsFinal && !(m.DeclaringType?.IsSealed ?? false);

    private static bool Classify(MethodInfo method, Type receiverType, ref int low)
    {
        var key = (method.MethodHandle, receiverType);
        if (_memo.TryGetValue(key, out var known))
        {
            return known;
        }

        var stack = _inProgress ??= new Dictionary<(RuntimeMethodHandle, Type), int>();
        if (stack.TryGetValue(key, out var ancestorDepth))
        {
            // Coinductive: an impure instruction in the cycle is still found in some member's own body.
            low = Math.Min(low, ancestorDepth);
            return true;
        }

        var depth = stack.Count;
        if (depth >= MaxDepth)
        {
            return false;
        }

        stack[key] = depth;
        var myLow = int.MaxValue;
        bool pure;
        try
        {
            pure = Analyze(method, receiverType, ref myLow);
        }
        catch (Exception)
        {
            pure = false;
        }
        finally
        {
            stack.Remove(key);
        }

        // A pure verdict that leaned on an ancestor still in progress is provisional until that ancestor
        // finishes; an impure verdict is final either way.
        if (!pure || myLow >= depth)
        {
            _memo[key] = pure;
        }

        if (myLow < depth)
        {
            low = Math.Min(low, myLow);
        }

        return pure;
    }

    private static bool Analyze(MethodInfo method, Type receiverType, ref int low)
    {
        // The JIT replaces an intrinsic's body, which is often a placeholder that only calls itself.
        if (IsJitIntrinsic(method))
        {
            return false;
        }

        var body = method.GetMethodBody();
        if (body == null)
        {
            return false;
        }

        var il = body.GetILAsByteArray();
        if (il == null)
        {
            return false;
        }

        var module = method.Module;
        var typeArgs = method.DeclaringType is { IsGenericType: true } ? method.DeclaringType.GetGenericArguments() : null;
        var methodArgs = method.IsGenericMethod ? method.GetGenericArguments() : null;

        var branchTargets = FindBranchTargets(il);
        if (branchTargets == null)
        {
            return false;
        }

        var prev = default(OpCode);
        var prevOffset = 0;
        var hasPrev = false;
        var i = 0;
        while (i < il.Length)
        {
            var instructionStart = i;
            short code = il[i++];
            if (code == 0xFE)
            {
                code = (short)(0xFE00 | il[i++]);
            }

            if (!_opCodes.TryGetValue(code, out var op))
            {
                return false;
            }

            var operandStart = i;
            i += OperandSize(op, il, i);

            if (op == OpCodes.Call || op == OpCodes.Callvirt)
            {
                var token = BitConverter.ToInt32(il, operandStart);
                // Another path can reach the call between the receiver load and the call, so the receiver is
                // only provably `this` when no branch lands in (prevOffset, call].
                var straightLine = hasPrev && prev == OpCodes.Ldarg_0;
                for (var k = prevOffset + 1; straightLine && k <= instructionStart; k++)
                {
                    straightLine = !branchTargets[k];
                }

                if (!CallIsPure(method, receiverType, module.ResolveMethod(token, typeArgs, methodArgs), op, straightLine, ref low))
                {
                    return false;
                }
            }
            else if (op == OpCodes.Newobj)
            {
                var ctor = module.ResolveMethod(BitConverter.ToInt32(il, operandStart), typeArgs, methodArgs);
                if (!IsPureValueTypeCtor(ctor))
                {
                    return false;
                }
            }
            else if (op == OpCodes.Initobj)
            {
                // Only a local may be reset; initobj through a field or argument address is a store.
                if (!hasPrev || (prev != OpCodes.Ldloca && prev != OpCodes.Ldloca_S))
                {
                    return false;
                }
            }
            else if (!IsAllowedOp(op))
            {
                return false;
            }

            // Prefixes and nops sit between the receiver load and the call, so they must not hide it.
            if (op != OpCodes.Nop && op != OpCodes.Constrained)
            {
                prev = op;
                prevOffset = instructionStart;
                hasPrev = true;
            }
        }

        return true;
    }

    private static int OperandSize(OpCode op, byte[] il, int at) => op.OperandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, at),
        _ => 4
    };

    // Marks every offset some branch or switch can jump to; null when the IL cannot be decoded.
    private static bool[] FindBranchTargets(byte[] il)
    {
        var targets = new bool[il.Length + 1];
        var i = 0;
        while (i < il.Length)
        {
            short code = il[i++];
            if (code == 0xFE)
            {
                code = (short)(0xFE00 | il[i++]);
            }

            if (!_opCodes.TryGetValue(code, out var op))
            {
                return null;
            }

            var operandStart = i;
            i += OperandSize(op, il, i);
            if (op.OperandType == OperandType.ShortInlineBrTarget)
            {
                Mark(targets, i + (sbyte)il[operandStart]);
            }
            else if (op.OperandType == OperandType.InlineBrTarget)
            {
                Mark(targets, i + BitConverter.ToInt32(il, operandStart));
            }
            else if (op.OperandType == OperandType.InlineSwitch)
            {
                var n = BitConverter.ToInt32(il, operandStart);
                for (var k = 0; k < n; k++)
                {
                    Mark(targets, i + BitConverter.ToInt32(il, operandStart + 4 + 4 * k));
                }
            }
        }

        return targets;
    }

    private static void Mark(bool[] targets, int offset)
    {
        if (offset >= 0 && offset < targets.Length)
        {
            targets[offset] = true;
        }
    }

    private static bool CallIsPure(
        MethodInfo caller, Type receiverType, MethodBase calleeBase, OpCode callOp, bool receiverLoadedFromThis,
        ref int low
    )
    {
        if (calleeBase is not MethodInfo callee)
        {
            return false;
        }

        if (IsKnownPureBcl(callee))
        {
            // A by-ref argument lets an allowlisted method store through ldflda/ldloca addresses.
            return !HasMutableByRef(callee);
        }

        // Outside the game assemblies a by-ref store can hide behind an intrinsic or native body, so the
        // IL cannot prove the address is only read.
        if (HasMutableByRef(callee) && !IsGameAssembly(callee.Module.Assembly))
        {
            return false;
        }

        var onThis = !caller.IsStatic && !callee.IsStatic && callee.GetParameters().Length == 0 &&
                     receiverLoadedFromThis;

        // `call` is non-virtual dispatch even on a virtual method (base.Foo()).
        if (callOp == OpCodes.Call || !IsOverridable(callee))
        {
            return Classify(callee, onThis ? receiverType : null, ref low);
        }

        if (onThis && receiverType != null)
        {
            var resolved = ResolveOverride(callee, receiverType);
            return resolved != null && Classify(resolved, receiverType, ref low);
        }

        return AllOverridesPure(callee, ref low);
    }

    private static bool HasMutableByRef(MethodInfo m)
    {
        var parameters = m.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType.IsByRef && !(parameters[i].IsIn && !parameters[i].IsOut))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsJitIntrinsic(MethodInfo m)
    {
        // The attribute is internal to CoreLib, so it is matched by name.
        var attributes = m.GetCustomAttributesData();
        for (var i = 0; i < attributes.Count; i++)
        {
            if (attributes[i].AttributeType.FullName == "System.Runtime.CompilerServices.IntrinsicAttribute")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Server, UOContent and the assemblies loaded from the configured assembly directories.</summary>
    internal static bool IsGameAssembly(Assembly assembly)
    {
        if (assembly == typeof(Item).Assembly || assembly == typeof(PureGetterClassifier).Assembly)
        {
            return true;
        }

        var loaded = AssemblyHandler.Assemblies;
        return loaded != null && Array.IndexOf(loaded, assembly) >= 0;
    }

    // Struct construction allocates nothing on the heap and these constructors only assign fields.
    private static bool IsPureValueTypeCtor(MethodBase ctor)
    {
        var t = ctor.DeclaringType;
        if (ctor is not ConstructorInfo || t == null || !t.IsValueType)
        {
            return false;
        }

        var def = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
        return def == typeof(Nullable<>) || def == typeof(TimeSpan) || def == typeof(DateTime) ||
               def == typeof(Point2D) || def == typeof(Point3D) || def == typeof(Serial);
    }

    private static bool IsAllowedOp(OpCode op)
    {
        if (op.OperandType is OperandType.ShortInlineBrTarget or OperandType.InlineBrTarget)
        {
            // leave exists only to exit try regions, which this classifier does not model.
            return !op.Name.StartsWith("leave", StringComparison.Ordinal);
        }

        var n = op.Name;
        if (n.StartsWith("conv.", StringComparison.Ordinal) || n.StartsWith("ldc.", StringComparison.Ordinal) ||
            n.StartsWith("ldarg", StringComparison.Ordinal) && n != "ldarga" && n != "ldarga.s" ||
            n.StartsWith("ldloc", StringComparison.Ordinal) || n.StartsWith("stloc", StringComparison.Ordinal) ||
            n.StartsWith("ldelem", StringComparison.Ordinal) && n != "ldelema")
        {
            return true;
        }

        return n is "nop" or "ldfld" or "ldflda" or "ldsfld" or "ldlen" or "ldnull" or "ldstr" or
            "add" or "add.ovf" or "add.ovf.un" or "sub" or "sub.ovf" or "sub.ovf.un" or
            "mul" or "mul.ovf" or "mul.ovf.un" or "div" or "div.un" or "rem" or "rem.un" or
            "and" or "or" or "xor" or "shl" or "shr" or "shr.un" or "neg" or "not" or
            "ceq" or "cgt" or "cgt.un" or "clt" or "clt.un" or
            "dup" or "pop" or "ret" or "throw" or "switch" or
            "box" or "unbox.any" or "isinst" or "castclass" or "constrained.";
    }

    private static bool IsKnownPureBcl(MethodInfo m)
    {
        var t = m.DeclaringType;
        if (t == null)
        {
            return false;
        }

        var def = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
        var name = m.Name;

        if (def == typeof(string))
        {
            return name is "op_Equality" or "op_Inequality" or "IsNullOrEmpty" or "IsNullOrWhiteSpace" or
                "get_Length" or "get_Chars";
        }

        if (def.IsPrimitive)
        {
            var parameters = m.GetParameters();
            return name == "CompareTo" && parameters.Length == 1 && parameters[0].ParameterType == def;
        }

        if (def == typeof(Math) || def == typeof(MathF))
        {
            return true;
        }

        if (def == typeof(Enum))
        {
            return name == "HasFlag";
        }

        if (def == typeof(Nullable<>))
        {
            return name is "get_HasValue" or "get_Value" or "GetValueOrDefault";
        }

        if (def == typeof(TimeSpan) || def == typeof(DateTime) || def == typeof(DateTimeOffset))
        {
            return m.IsStatic || name.StartsWith("get_", StringComparison.Ordinal) ||
                   name.StartsWith("op_", StringComparison.Ordinal);
        }

        if (def == typeof(Serial) || def == typeof(Point3D) || def == typeof(Point2D))
        {
            return name.StartsWith("get_", StringComparison.Ordinal) || name.StartsWith("op_", StringComparison.Ordinal);
        }

        return false;
    }

    // The override of `m` that an instance of `type` runs; null when it cannot be determined, which callers treat as impure.
    private static MethodInfo ResolveOverride(MethodInfo m, Type type)
    {
        if (!m.IsVirtual || type == null)
        {
            return m;
        }

        try
        {
            // No concrete instance to resolve against, so nothing can be proven about what would run.
            if (type.IsInterface || type.IsAbstract)
            {
                return null;
            }

            if (m.DeclaringType.IsInterface)
            {
                var mapped = MapInterfaceMethods(type, m);
                return mapped.Count == 1 ? mapped[0] : null;
            }

            var baseDef = m.GetBaseDefinition();
            for (var cur = type; cur != null; cur = cur.BaseType)
            {
                var declared = cur.GetMethods(DeclaredMethods);
                for (var i = 0; i < declared.Length; i++)
                {
                    var c = declared[i];
                    if (c.IsVirtual && c.Name == m.Name && SameMethod(c.GetBaseDefinition(), baseDef))
                    {
                        return c;
                    }
                }
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    private static bool SameMethod(MethodInfo a, MethodInfo b) =>
        a.Module == b.Module && a.MetadataToken == b.MetadataToken;

    // A constructed interface maps exactly; an open one (inside a generic class) maps every instantiation.
    private static List<MethodInfo> MapInterfaceMethods(Type type, MethodInfo interfaceMethod, bool byDefinition = false)
    {
        var result = new List<MethodInfo>();
        var declaring = interfaceMethod.DeclaringType;
        var exact = !byDefinition && (!declaring.IsGenericType || !declaring.ContainsGenericParameters);
        var interfaces = type.GetInterfaces();
        for (var i = 0; i < interfaces.Length; i++)
        {
            var iface = interfaces[i];
            if (exact ? iface != declaring : !SameDefinition(iface, declaring))
            {
                continue;
            }

            var map = type.GetInterfaceMap(iface);
            for (var k = 0; k < map.InterfaceMethods.Length; k++)
            {
                if (SameMethod(map.InterfaceMethods[k], interfaceMethod))
                {
                    result.Add(map.TargetMethods[k]);
                }
            }
        }

        return result;
    }

    private static bool SameDefinition(Type a, Type b) =>
        (a.IsGenericType ? a.GetGenericTypeDefinition() : a) == (b.IsGenericType ? b.GetGenericTypeDefinition() : b);

    // The callee itself plus every override or implementation in the loaded assemblies must be pure.
    private static bool AllOverridesPure(MethodInfo callee, ref int low)
    {
        // The callee's own body is checked first: most rejections need no assembly scan.
        if (!callee.IsAbstract && !Classify(callee, null, ref low))
        {
            return false;
        }

        var declaring = callee.DeclaringType;
        var derived = GetDerivedTypes(declaring);
        var found = false;
        for (var t = 0; t < derived.Length; t++)
        {
            var type = derived[t];
            if (declaring.IsInterface)
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                List<MethodInfo> targets;
                try
                {
                    targets = MapInterfaceMethods(type, callee);

                    // Generic implementers (class G<T> : IFoo<T>) and variance (IFoo<string> reached via
                    // IFoo<object>) never match exactly; they may still run, so map by definition.
                    if (targets.Count == 0)
                    {
                        targets = MapInterfaceMethods(type, callee, true);
                    }

                    if (targets.Count == 0)
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    return false;
                }

                for (var k = 0; k < targets.Count; k++)
                {
                    found = true;
                    if (!Classify(targets[k], null, ref low))
                    {
                        return false;
                    }
                }

                continue;
            }

            MethodInfo[] methods;
            try
            {
                methods = type.GetMethods(DeclaredMethods);
            }
            catch (Exception)
            {
                return false;
            }

            var baseDef = callee.GetBaseDefinition();
            for (var i = 0; i < methods.Length; i++)
            {
                var m = methods[i];
                if (!m.IsVirtual || m.IsAbstract || m.Name != callee.Name || !SameMethod(m.GetBaseDefinition(), baseDef))
                {
                    continue;
                }

                found = true;
                if (!Classify(m, null, ref low))
                {
                    return false;
                }
            }
        }

        // An abstract/interface member with nothing found cannot be proven pure.
        return found || !callee.IsAbstract;
    }

    private static Type[] GetDerivedTypes(Type declaring)
    {
        return _derivedTypes.GetOrAdd(
            declaring,
            static d =>
            {
                var definition = d.IsGenericType ? d.GetGenericTypeDefinition() : d;
                var declaringAssembly = d.Assembly;
                var declaringName = declaringAssembly.GetName().Name;

                // Framework types are reached through facades under other assembly names, so a reference
                // by name proves nothing; only game assemblies are pruned.
                var scanAll = !IsGameAssembly(declaringAssembly);
                var result = new List<Type>();

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (var a = 0; a < assemblies.Length; a++)
                {
                    var assembly = assemblies[a];
                    if (assembly.IsDynamic)
                    {
                        continue;
                    }

                    // Only assemblies that reference the declaring assembly can derive from it.
                    if (!scanAll && assembly != declaringAssembly && !References(assembly, declaringName))
                    {
                        continue;
                    }

                    var types = SafeTypes(assembly);
                    for (var k = 0; k < types.Length; k++)
                    {
                        var type = types[k];
                        if (type != d && DerivesFrom(type, d, definition))
                        {
                            result.Add(type);
                        }
                    }
                }

                return result.ToArray();
            }
        );
    }

    private static bool References(Assembly assembly, string name)
    {
        try
        {
            var refs = assembly.GetReferencedAssemblies();
            for (var i = 0; i < refs.Length; i++)
            {
                if (refs[i].Name == name)
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {
            return true;
        }

        return false;
    }

    private static bool DerivesFrom(Type type, Type declaring, Type definition)
    {
        try
        {
            if (declaring.IsInterface)
            {
                var interfaces = type.GetInterfaces();
                for (var i = 0; i < interfaces.Length; i++)
                {
                    if (SameDefinition(interfaces[i], definition))
                    {
                        return true;
                    }
                }

                return false;
            }

            for (var cur = type.BaseType; cur != null; cur = cur.BaseType)
            {
                if (SameDefinition(cur, definition))
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {
            // An unloadable ancestor means we cannot rule this type out.
            return true;
        }

        return false;
    }

    private static Type[] SafeTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            var loaded = new List<Type>();
            for (var i = 0; i < e.Types.Length; i++)
            {
                if (e.Types[i] != null)
                {
                    loaded.Add(e.Types[i]);
                }
            }

            return loaded.ToArray();
        }
        catch (Exception)
        {
            return [];
        }
    }
}
