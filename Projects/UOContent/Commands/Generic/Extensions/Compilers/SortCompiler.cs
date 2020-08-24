using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Server.Utilities;

namespace Server.Commands.Generic
{
    public sealed class OrderInfo
    {
        private int m_Order;

        public OrderInfo(Property property, bool isAscending)
        {
            Property = property;

            IsAscending = isAscending;
        }

        public Property Property { get; set; }

        public bool IsAscending
        {
            get => m_Order > 0;
            set => m_Order = value ? +1 : -1;
        }

        public bool IsDescending
        {
            get => m_Order < 0;
            set => m_Order = value ? -1 : +1;
        }

        public int Sign
        {
            get => Math.Sign(m_Order);
            set
            {
                m_Order = Math.Sign(value);

                if (m_Order == 0)
                    throw new InvalidOperationException("Sign cannot be zero.");
            }
        }
    }

    public static class SortCompiler
    {
        public static IComparer<T> Compile<T>(AssemblyEmitter assembly, Type objectType, OrderInfo[] orders)
        {
            TypeBuilder typeBuilder = assembly.DefineType(
                "__sort",
                TypeAttributes.Public,
                typeof(T));
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

                for (int i = 0; i < orders.Length; ++i)
                {
                    if (i > 0)
                    {
                        emitter.LoadLocal(v);
                        emitter.BranchIfTrue(end);
                    }

                    OrderInfo orderInfo = orders[i];

                    Property prop = orderInfo.Property;
                    int sign = orderInfo.Sign;

                    emitter.LoadLocal(a);
                    emitter.Chain(prop);

                    bool couldCompare =
                        emitter.CompareTo(sign, () =>
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
            }

            Type comparerType = typeBuilder.CreateType();
            return (IComparer<T>)ActivatorUtil.CreateInstance(comparerType);
        }
    }
}
