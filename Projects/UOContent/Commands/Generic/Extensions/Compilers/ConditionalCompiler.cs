using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Server.Commands.Generic
{
    public interface IConditional
    {
        bool Verify(object obj);
    }

    public interface ICondition
    {
        // `target` is the object under test, already cast to the conditional's type (so it is
        // null when the cast failed -- TypeCondition, always first, is what rejects that).
        Expression Build(ParameterExpression target);
    }

    public sealed class TypeCondition : ICondition
    {
        public static TypeCondition Default = new();

        Expression ICondition.Build(ParameterExpression target) =>
            Expression.ReferenceNotEqual(target, Expression.Constant(null, target.Type));
    }

    public abstract class PropertyCondition : ICondition
    {
        protected bool m_Not;
        protected Property m_Property;

        public PropertyCondition(Property property, bool not)
        {
            m_Property = property;
            m_Not = not;
        }

        public abstract Expression Build(ParameterExpression target);

        // A binding like Message.Number dereferences Message first, and Message is null on most
        // objects a sweep walks. That is "no match" rather than a crash -- and it stays "no match"
        // under negation, so the guard wraps the test after `not` has been applied to it.
        protected Expression Guarded(ParameterExpression target, Func<Expression, Expression> test) =>
            PropertyExpressions.Chain(
                target,
                m_Property,
                value =>
                {
                    var result = test(value);
                    return m_Not ? Expression.Not(result) : result;
                },
                Expression.Constant(false)
            );
    }

    public enum StringOperator
    {
        Equal,
        NotEqual,

        Contains,
        StartsWith,
        EndsWith
    }

    public sealed class StringCondition : PropertyCondition
    {
        private readonly bool m_IgnoreCase;
        private readonly StringOperator m_Operator;
        private readonly object m_Value;

        public StringCondition(Property property, bool not, StringOperator op, object value, bool ignoreCase)
            : base(property, not)
        {
            m_Operator = op;
            m_Value = value;
            m_IgnoreCase = ignoreCase;
        }

        public override Expression Build(ParameterExpression target)
        {
            if (m_Property.Type != typeof(string))
            {
                throw new InvalidOperationException("String operators require a string property.");
            }

            var inverse = m_Operator == StringOperator.NotEqual;

            var methodName = m_Operator switch
            {
                StringOperator.Equal or StringOperator.NotEqual => m_IgnoreCase ? "InsensitiveEquals" : "EqualsOrdinal",
                StringOperator.Contains                         => m_IgnoreCase ? "InsensitiveContains" : "ContainsOrdinal",
                StringOperator.StartsWith                       => m_IgnoreCase ? "InsensitiveStartsWith" : "StartsWithOrdinal",
                StringOperator.EndsWith                         => m_IgnoreCase ? "InsensitiveEndsWith" : "EndsWithOrdinal",
                _                                               => throw new InvalidOperationException("Invalid string comparison operator.")
            };

            var helper = (m_IgnoreCase ? typeof(InsensitiveStringHelpers) : typeof(OrdinalStringHelpers)).GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), typeof(string)],
                null
            );

            var constant = PropertyExpressions.Constant(typeof(string), m_Value);

            return Guarded(
                target,
                value =>
                {
                    Expression test = Expression.Call(helper, value, constant);

                    // The equality helpers handle a null of their own; the rest need the guard.
                    if (m_Operator is not (StringOperator.Equal or StringOperator.NotEqual))
                    {
                        test = Expression.AndAlso(
                            Expression.ReferenceNotEqual(value, Expression.Constant(null, typeof(string))),
                            test
                        );
                    }

                    return inverse ? Expression.Not(test) : test;
                }
            );
        }
    }

    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        Greater,
        GreaterEqual,
        Lesser,
        LesserEqual
    }

    public sealed class ComparisonCondition : PropertyCondition
    {
        private readonly ComparisonOperator m_Operator;
        private readonly object m_Value;

        public ComparisonCondition(Property property, bool not, ComparisonOperator op, object value)
            : base(property, not)
        {
            m_Operator = op;
            m_Value = value;
        }

        public override Expression Build(ParameterExpression target)
        {
            var constant = PropertyExpressions.Constant(m_Property.Type, m_Value);

            return Guarded(
                target,
                value =>
                {
                    if (PropertyExpressions.TryRelational(value, constant, m_Operator, out var test))
                    {
                        return test;
                    }

                    // This type is -not- comparable. We can only support == and != operations.
                    return m_Operator switch
                    {
                        ComparisonOperator.Equal    => PropertyExpressions.ValueEquals(value, constant),
                        ComparisonOperator.NotEqual => Expression.Not(PropertyExpressions.ValueEquals(value, constant)),
                        ComparisonOperator.Greater or ComparisonOperator.GreaterEqual
                            or ComparisonOperator.Lesser or ComparisonOperator.LesserEqual =>
                            throw new InvalidOperationException("Property does not support relational comparisons."),
                        _ => throw new InvalidOperationException("Invalid operator.")
                    };
                }
            );
        }
    }

    public static class ConditionalCompiler
    {
        private sealed class CompiledConditional : IConditional
        {
            private readonly Func<object, bool> _verify;

            public CompiledConditional(Func<object, bool> verify) => _verify = verify;

            public bool Verify(object obj) => _verify(obj);
        }

        /// <summary>
        /// Compiles a conjunction of conditions over <paramref name="objectType" /> into a single
        /// delegate. The conditions short-circuit left to right, so <see cref="TypeCondition" />
        /// comes first and the rest can assume a non-null, correctly typed target.
        /// </summary>
        public static IConditional Compile(Type objectType, ICondition[] conditions) =>
            new CompiledConditional(Build(objectType, conditions).Compile());

        public static Expression<Func<object, bool>> Build(Type objectType, ICondition[] conditions) =>
            Lambda(objectType, target => Conjunction(target, conditions));

        /// <summary>
        /// A disjunction of conjunctions -- <c>(a and b) or (c and d)</c> -- as one lambda, for
        /// callers that would otherwise compile every group separately and loop over them.
        /// </summary>
        public static Expression<Func<object, bool>> Build(Type objectType, ICondition[][] groups) =>
            Lambda(
                objectType,
                target =>
                {
                    Expression body = groups.Length > 0 ? Conjunction(target, groups[0]) : Expression.Constant(false);

                    for (var i = 1; i < groups.Length; ++i)
                    {
                        body = Expression.OrElse(body, Conjunction(target, groups[i]));
                    }

                    return body;
                }
            );

        private static Expression Conjunction(ParameterExpression target, ICondition[] conditions)
        {
            Expression body = conditions.Length > 0 ? conditions[0].Build(target) : Expression.Constant(true);

            for (var i = 1; i < conditions.Length; ++i)
            {
                body = Expression.AndAlso(body, conditions[i].Build(target));
            }

            return body;
        }

        private static Expression<Func<object, bool>> Lambda(Type objectType, Func<ParameterExpression, Expression> body)
        {
            var obj = Expression.Parameter(typeof(object), "obj");
            var target = Expression.Variable(objectType, "target");

            return Expression.Lambda<Func<object, bool>>(
                Expression.Block(
                    [target],
                    Expression.Assign(target, Expression.TypeAs(obj, objectType)),
                    body(target)
                ),
                obj
            );
        }
    }
}
