using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Server.Utilities;

namespace Server.Commands.Generic
{
    public static class DistinctCompiler
    {
        public static IComparer<T> Compile<T>(AssemblyEmitter assembly, Type objectType, Property[] props)
        {
            TypeBuilder typeBuilder = assembly.DefineType(
                "__distinct",
                TypeAttributes.Public,
                typeof(object));
            {
                ConstructorBuilder ctor = typeBuilder.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard,
                    Type.EmptyTypes);

                ILGenerator il = ctor.GetILGenerator();

                // : base()
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(T).GetConstructor(Type.EmptyTypes) ??
                                      throw new Exception(
                                          $"Could not find empty constructor for type {typeof(T).FullName}"));

                // return;
                il.Emit(OpCodes.Ret);
            }

            typeBuilder.AddInterfaceImplementation(typeof(IComparer<T>));

            MethodBuilder compareMethod;
            {
                MethodEmitter emitter = new MethodEmitter(typeBuilder);

                emitter.Define(
                    /*  name  */ "Compare",
                    /*  attr  */ MethodAttributes.Public | MethodAttributes.Virtual,
                    /* return */ typeof(int),
                    /* params */ new[] { typeof(T), typeof(T) });

                LocalBuilder a = emitter.CreateLocal(objectType);
                LocalBuilder b = emitter.CreateLocal(objectType);

                LocalBuilder v = emitter.CreateLocal(typeof(int));

                emitter.LoadArgument(1);
                emitter.CastAs(objectType);
                emitter.StoreLocal(a);

                emitter.LoadArgument(2);
                emitter.CastAs(objectType);
                emitter.StoreLocal(b);

                emitter.Load(0);
                emitter.StoreLocal(v);

                Label end = emitter.CreateLabel();

                for (int i = 0; i < props.Length; ++i)
                {
                    if (i > 0)
                    {
                        emitter.LoadLocal(v);
                        emitter.BranchIfTrue(end);
                    }

                    Property prop = props[i];

                    emitter.LoadLocal(a);
                    emitter.Chain(prop);

                    bool couldCompare =
                        emitter.CompareTo(1, () =>
                        {
                            emitter.LoadLocal(b);
                            emitter.Chain(prop);
                        });

                    if (!couldCompare)
                        throw new InvalidOperationException("Property is not comparable.");

                    emitter.StoreLocal(v);
                }

                emitter.MarkLabel(end);

                emitter.LoadLocal(v);
                emitter.Return();

                typeBuilder.DefineMethodOverride(
                    emitter.Method,
                    typeof(IComparer<T>).GetMethod(
                        "Compare",
                        new[]
                        {
                            typeof(T),
                            typeof(T)
                        }) ?? throw new Exception($"No Compare method found for type {typeof(T).FullName}"));

                compareMethod = emitter.Method;
            }

            typeBuilder.AddInterfaceImplementation(typeof(IEqualityComparer<T>));
            {
                MethodEmitter emitter = new MethodEmitter(typeBuilder);

                emitter.Define(
                    /*  name  */ "Equals",
                    /*  attr  */ MethodAttributes.Public | MethodAttributes.Virtual,
                    /* return */ typeof(bool),
                    /* params */ new[] { typeof(T), typeof(T) });

                emitter.Generator.Emit(OpCodes.Ldarg_0);
                emitter.Generator.Emit(OpCodes.Ldarg_1);
                emitter.Generator.Emit(OpCodes.Ldarg_2);

                emitter.Generator.Emit(OpCodes.Call, compareMethod);

                emitter.Generator.Emit(OpCodes.Ldc_I4_0);

                emitter.Generator.Emit(OpCodes.Ceq);

                emitter.Generator.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(
                    emitter.Method,
                    typeof(IEqualityComparer<T>).GetMethod(
                        "Equals",
                        new[]
                        {
                            typeof(T),
                            typeof(T)
                        }) ?? throw new Exception($"No Equals method found for type {typeof(T).FullName}"));
            }

            {
                MethodEmitter emitter = new MethodEmitter(typeBuilder);

                emitter.Define(
                    /*  name  */ "GetHashCode",
                    /*  attr  */ MethodAttributes.Public | MethodAttributes.Virtual,
                    /* return */ typeof(int),
                    /* params */ new[] { typeof(T) });

                LocalBuilder obj = emitter.CreateLocal(objectType);

                emitter.LoadArgument(1);
                emitter.CastAs(objectType);
                emitter.StoreLocal(obj);

                for (int i = 0; i < props.Length; ++i)
                {
                    Property prop = props[i];

                    emitter.LoadLocal(obj);
                    emitter.Chain(prop);

                    Type active = emitter.Active;

                    MethodInfo getHashCode = active.GetMethod("GetHashCode", Type.EmptyTypes);

                    getHashCode ??= typeof(T).GetMethod("GetHashCode", Type.EmptyTypes);

                    if (active != typeof(int))
                    {
                        if (!active.IsValueType)
                        {
                            LocalBuilder value = emitter.AcquireTemp(active);

                            Label valueNotNull = emitter.CreateLabel();
                            Label done = emitter.CreateLabel();

                            emitter.StoreLocal(value);
                            emitter.LoadLocal(value);

                            emitter.BranchIfTrue(valueNotNull);

                            emitter.Load(0);
                            emitter.Pop(typeof(int));

                            emitter.Branch(done);

                            emitter.MarkLabel(valueNotNull);

                            emitter.LoadLocal(value);
                            emitter.Call(getHashCode);

                            emitter.ReleaseTemp(value);

                            emitter.MarkLabel(done);
                        }
                        else
                        {
                            emitter.Call(getHashCode);
                        }
                    }

                    if (i > 0)
                        emitter.Xor();
                }

                emitter.Return();

                typeBuilder.DefineMethodOverride(
                    emitter.Method,
                    typeof(IEqualityComparer<T>).GetMethod(
                        "GetHashCode",
                        new[]
                        {
                            typeof(T)
                        }) ?? throw new Exception($"No GetHashCode method found for type {typeof(T).FullName}"));
            }

            Type comparerType = typeBuilder.CreateType();

            return (IComparer<T>)ActivatorUtil.CreateInstance(comparerType);
        }
    }
}
