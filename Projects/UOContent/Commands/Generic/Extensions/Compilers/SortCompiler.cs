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
                {
                    throw new InvalidOperationException("Sign cannot be zero.");
                }
            }
        }
    }

    public static class SortCompiler
    {
        public static IComparer<T> Compile<T>(AssemblyEmitter assembly, Type objectType, OrderInfo[] orders)
        {
            var typeBuilder = assembly.DefineType(
                "__sort",
                TypeAttributes.Public,
                typeof(T)
            );
            {
                var ctor = typeBuilder.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard,
                    Type.EmptyTypes
                );

                var il = ctor.GetILGenerator();

                // : base()
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(
                    OpCodes.Call,
                    typeof(T).GetConstructor(Type.EmptyTypes) ??
                    throw new Exception($"Could not find empty constructor for type {typeof(T).FullName}")
                );

                // return;
                il.Emit(OpCodes.Ret);
            }

            typeBuilder.AddInterfaceImplementation(typeof(IComparer<T>));
            {
                var emitter = new MethodEmitter(typeBuilder);

                emitter.Define(
                    /*  name  */ "Compare",
                    /*  attr  */
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    /* return */
                    typeof(int),
                    /* params */
                    new[] { typeof(T), typeof(T) }
                );

                var a = emitter.CreateLocal(objectType);
                var b = emitter.CreateLocal(objectType);

                var v = emitter.CreateLocal(typeof(int));

                emitter.LoadArgument(1);
                emitter.CastAs(objectType);
                emitter.StoreLocal(a);

                emitter.LoadArgument(2);
                emitter.CastAs(objectType);
                emitter.StoreLocal(b);

                emitter.Load(0);
                emitter.StoreLocal(v);

                var end = emitter.CreateLabel();

                for (var i = 0; i < orders.Length; ++i)
                {
                    if (i > 0)
                    {
                        emitter.LoadLocal(v);
                        emitter.BranchIfTrue(end);
                    }

                    var orderInfo = orders[i];

                    var prop = orderInfo.Property;
                    var sign = orderInfo.Sign;

                    emitter.LoadLocal(a);
                    emitter.Chain(prop);

                    var couldCompare =
                        emitter.CompareTo(
                            sign,
                            () =>
                            {
                                emitter.LoadLocal(b);
                                emitter.Chain(prop);
                            }
                        );

                    if (!couldCompare)
                    {
                        throw new InvalidOperationException("Property is not comparable.");
                    }

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
                        }
                    ) ?? throw new Exception($"No Compare method found for type {typeof(T).FullName}")
                );
            }

            var comparerType = typeBuilder.CreateType();
            return comparerType.CreateInstance<IComparer<T>>();
        }
    }
}
