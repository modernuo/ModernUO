using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Server.Commands.Generic
{
    public static class DistinctCompiler
    {
        private sealed class DistinctComparer<T> : IComparer<T>, IEqualityComparer<T>
        {
            private readonly Comparison<T> _compare;
            private readonly Func<T, int> _hash;

            public DistinctComparer(Comparison<T> compare, Func<T, int> hash)
            {
                _compare = compare;
                _hash = hash;
            }

            public int Compare(T x, T y) => _compare(x, y);

            public bool Equals(T x, T y) => _compare(x, y) == 0;

            public int GetHashCode(T obj) => _hash(obj);
        }

        /// <summary>
        /// A comparer that treats two objects as the same when every one of
        /// <paramref name="props" /> reads equal on both. Ordering is the sort compiler's, all
        /// ascending, so the result doubles as an <see cref="IEqualityComparer{T}" />.
        /// </summary>
        public static IComparer<T> Compile<T>(Type objectType, Property[] props)
        {
            var signs = new int[props.Length];
            Array.Fill(signs, 1);

            return new DistinctComparer<T>(
                SortCompiler.Build<T>(objectType, props, signs).Compile(),
                BuildHash<T>(objectType, props).Compile()
            );
        }

        // XOR of each property's hash; a null reference hashes to 0 and an int hashes to itself.
        public static Expression<Func<T, int>> BuildHash<T>(Type objectType, Property[] props)
        {
            var arg = Expression.Parameter(typeof(T), "obj");
            var target = Expression.Variable(objectType, "target");

            Expression hash = Expression.Constant(0);

            for (var i = 0; i < props.Length; ++i)
            {
                var part = HashOf(target, props[i]);

                hash = i == 0 ? part : Expression.ExclusiveOr(hash, part);
            }

            return Expression.Lambda<Func<T, int>>(
                Expression.Block(
                    [target],
                    Expression.Assign(target, Expression.TypeAs(arg, objectType)),
                    hash
                ),
                arg
            );
        }

        private static Expression HashOf(Expression target, Property prop)
        {
            var read = PropertyExpressions.ChainOrDefault(target, prop);
            var type = prop.Type;

            if (type == typeof(int))
            {
                return read;
            }

            var value = Expression.Variable(type, prop.Binding);
            var getHashCode = type.GetMethod("GetHashCode", Type.EmptyTypes) ?? typeof(object).GetMethod("GetHashCode", Type.EmptyTypes)!;

            Expression hash = Expression.Call(value, getHashCode);

            if (!type.IsValueType)
            {
                hash = Expression.Condition(
                    Expression.ReferenceNotEqual(value, Expression.Constant(null, type)),
                    hash,
                    Expression.Constant(0)
                );
            }

            return Expression.Block([value], Expression.Assign(value, read), hash);
        }
    }
}
