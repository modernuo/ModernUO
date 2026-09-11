using System;
using System.Collections.Generic;
using System.Linq.Expressions;

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
        public static IComparer<T> Compile<T>(Type objectType, OrderInfo[] orders)
        {
            var properties = new Property[orders.Length];
            var signs = new int[orders.Length];

            for (var i = 0; i < orders.Length; ++i)
            {
                properties[i] = orders[i].Property;
                signs[i] = orders[i].Sign;
            }

            return Comparer<T>.Create(Build<T>(objectType, properties, signs).Compile());
        }

        /// <summary>
        /// A <see cref="Comparison{T}" /> over <paramref name="properties" />, taken in order: the
        /// first property that orders the two objects decides, each multiplied by its sign. Both
        /// arguments are cast to <paramref name="objectType" /> first; the bindings are read from
        /// that.
        /// </summary>
        public static Expression<Comparison<T>> Build<T>(Type objectType, Property[] properties, int[] signs)
        {
            var x = Expression.Parameter(typeof(T), "x");
            var y = Expression.Parameter(typeof(T), "y");

            var a = Expression.Variable(objectType, "a");
            var b = Expression.Variable(objectType, "b");

            return Expression.Lambda<Comparison<T>>(
                Expression.Block(
                    [a, b],
                    Expression.Assign(a, Expression.TypeAs(x, objectType)),
                    Expression.Assign(b, Expression.TypeAs(y, objectType)),
                    Ordered(a, b, properties, signs, 0)
                ),
                x,
                y
            );
        }

        private static Expression Ordered(Expression a, Expression b, Property[] properties, int[] signs, int index)
        {
            if (index >= properties.Length)
            {
                return Expression.Constant(0);
            }

            var prop = properties[index];

            var couldCompare = PropertyExpressions.TryCompare(
                PropertyExpressions.ChainOrDefault(a, prop),
                PropertyExpressions.ChainOrDefault(b, prop),
                signs[index],
                out var comparison
            );

            if (!couldCompare)
            {
                throw new InvalidOperationException("Property is not comparable.");
            }

            if (index == properties.Length - 1)
            {
                return comparison;
            }

            var v = Expression.Variable(typeof(int), "v");

            return Expression.Block(
                [v],
                Expression.Assign(v, comparison),
                Expression.Condition(
                    Expression.NotEqual(v, Expression.Constant(0)),
                    v,
                    Ordered(a, b, properties, signs, index + 1)
                )
            );
        }
    }
}
