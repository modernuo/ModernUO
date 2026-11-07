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
    private static readonly ConcurrentDictionary<Type, Type[]> _derivedTypes = new();

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
                pure = Classify(ResolveOverride(getter, runtimeType), runtimeType, ref low);
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
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode op)
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

        var prev = default(OpCode);
        var hasPrev = false;
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
                return false;
            }

            var operandStart = i;
            i += op.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, i),
                _ => 4
            };

            if (op == OpCodes.Call || op == OpCodes.Callvirt)
            {
                var token = BitConverter.ToInt32(il, operandStart);
                if (!CallIsPure(method, receiverType, module.ResolveMethod(token, typeArgs, methodArgs), op, hasPrev, prev, ref low))
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
                hasPrev = true;
            }
        }

        return true;
    }

    private static bool CallIsPure(
        MethodInfo caller, Type receiverType, MethodBase calleeBase, OpCode callOp, bool hasPrev, OpCode prev,
        ref int low
    )
    {
        if (calleeBase is not MethodInfo callee)
        {
            return false;
        }

        if (IsKnownPureBcl(callee))
        {
            return true;
        }

        var onThis = !caller.IsStatic && !callee.IsStatic && callee.GetParameters().Length == 0 &&
                     hasPrev && prev == OpCodes.Ldarg_0;

        // `call` is non-virtual dispatch even on a virtual method (base.Foo()).
        if (callOp == OpCodes.Call || !IsOverridable(callee))
        {
            return Classify(callee, onThis ? receiverType : null, ref low);
        }

        if (onThis && receiverType != null)
        {
            return Classify(ResolveOverride(callee, receiverType), receiverType, ref low);
        }

        return AllOverridesPure(callee, ref low);
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

    // The override of `m` that an instance of `type` runs.
    private static MethodInfo ResolveOverride(MethodInfo m, Type type)
    {
        if (!m.IsVirtual || type == null)
        {
            return m;
        }

        try
        {
            if (m.DeclaringType.IsInterface)
            {
                if (!type.IsInterface && !type.IsAbstract)
                {
                    var mapped = MapInterfaceMethod(type, m);
                    if (mapped != null)
                    {
                        return mapped;
                    }
                }

                return m;
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
            // Unresolvable: fall back to the declared method.
        }

        return m;
    }

    private static bool SameMethod(MethodInfo a, MethodInfo b) =>
        a.Module == b.Module && a.MetadataToken == b.MetadataToken;

    private static MethodInfo MapInterfaceMethod(Type type, MethodInfo interfaceMethod)
    {
        var interfaces = type.GetInterfaces();
        for (var i = 0; i < interfaces.Length; i++)
        {
            var iface = interfaces[i];
            if (!SameDefinition(iface, interfaceMethod.DeclaringType))
            {
                continue;
            }

            var map = type.GetInterfaceMap(iface);
            for (var k = 0; k < map.InterfaceMethods.Length; k++)
            {
                if (SameMethod(map.InterfaceMethods[k], interfaceMethod))
                {
                    return map.TargetMethods[k];
                }
            }
        }

        return null;
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

                MethodInfo target;
                try
                {
                    target = MapInterfaceMethod(type, callee);
                }
                catch (Exception)
                {
                    return false;
                }

                if (target == null)
                {
                    continue;
                }

                found = true;
                if (!Classify(target, null, ref low))
                {
                    return false;
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
                var scanAll = declaringAssembly == typeof(object).Assembly;
                var result = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.IsDynamic)
                    {
                        continue;
                    }

                    // Only assemblies that reference the declaring assembly can derive from it. CoreLib is
                    // reached through facades, so its derivatives can be anywhere.
                    if (!scanAll && assembly != declaringAssembly && !References(assembly, declaringName))
                    {
                        continue;
                    }

                    foreach (var type in SafeTypes(assembly))
                    {
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
