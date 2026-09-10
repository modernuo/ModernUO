using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Server.Commands.Generic;

namespace Server.Engines.AdvancedSearch;

/// <summary>
/// Turns a property-test string from the Advanced Search gump into a compiled predicate. The
/// grammar is the gump's own -- <c>~</c> negates a leaf, <c>@</c> is AND, <c>|</c> is OR and binds
/// looser, and the string operators double up (<c>&gt;</c> is "starts with" on a string) -- but
/// each leaf becomes the same <see cref="ICondition" /> a <c>where</c> clause compiles, so there is
/// one comparison engine. A leaf that cannot be resolved or parsed is simply "no match".
/// </summary>
/// <remarks>
/// Runs on the search workers, off the game loop: binding is reflection, compiling is
/// <c>Expression.Compile</c>, and neither touches game state. The one exception is a value that
/// names an entity by serial, which <see cref="Types.TryParse" /> resolves through the world --
/// the same read the previous per-entity evaluator made, now made once per type instead.
/// </remarks>
public static class AdvancedSearchConditions
{
    // Runtime type -> its public readable instance properties, for the case-insensitive name scan.
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _properties = new();

    private static readonly Func<object, bool> _never = static _ => false;

    /// <summary>
    /// Per-search memo shared by every worker. Predicates are keyed twice: by the runtime type
    /// seen, and by the type the predicate was actually compiled for -- the most derived type
    /// that declares one of the properties -- so every subclass of Item that does not hide
    /// <c>Hue</c> shares one compiled <c>Hue = 5</c>.
    /// </summary>
    public sealed class Cache
    {
        internal readonly ConcurrentDictionary<Type, Func<object, bool>> ByRuntimeType = new();
        internal readonly ConcurrentDictionary<Type, Func<object, bool>> ByCompiledType = new();
    }

    public static Func<object, bool> GetPredicate(Cache cache, Type runtimeType, string propertyTest) =>
        cache.ByRuntimeType.GetOrAdd(
            runtimeType,
            static (type, state) => Build(state.cache, type, state.propertyTest),
            (cache, propertyTest)
        );

    /// <summary>Compiles without a cache. Test seam and one-off use.</summary>
    public static Func<object, bool> Compile(Type runtimeType, string propertyTest) =>
        Build(new Cache(), runtimeType, propertyTest);

    private static Func<object, bool> Build(Cache cache, Type runtimeType, string propertyTest)
    {
        var groups = Parse(runtimeType, propertyTest, out var compiledType);

        if (groups == null)
        {
            return _never;
        }

        return cache.ByCompiledType.GetOrAdd(
            compiledType,
            static (type, groups) =>
            {
                try
                {
                    return ConditionalCompiler.Build(type, groups).Compile();
                }
                catch (Exception)
                {
                    return _never;
                }
            },
            groups
        );
    }

    // OR of ANDs, which is what splitting on '|' and then on '@' yields. A group with a dead leaf
    // is dropped; no live group left means nothing can match, reported as null.
    private static ICondition[][] Parse(Type runtimeType, string propertyTest, out Type compiledType)
    {
        Type mostDerived = null;

        var groups = new List<ICondition[]>();

        foreach (var orPart in propertyTest.Split('|'))
        {
            var group = new List<ICondition> { TypeCondition.Default };
            var alive = true;

            foreach (var andPart in orPart.Split('@'))
            {
                var leaf = Leaf(runtimeType, andPart, out var declaringType);

                if (leaf == null)
                {
                    alive = false;
                    break;
                }

                group.Add(leaf);

                // Every declaring type is an ancestor of the runtime type (or the type itself), so
                // they nest; the predicate is compiled for the most derived one any leaf needs and
                // serves every runtime type that resolves the same properties.
                if (mostDerived == null || declaringType.IsAssignableTo(mostDerived))
                {
                    mostDerived = declaringType;
                }
            }

            if (alive)
            {
                groups.Add(group.ToArray());
            }
        }

        compiledType = mostDerived ?? runtimeType;

        return groups.Count > 0 ? groups.ToArray() : null;
    }

    private static ICondition Leaf(Type runtimeType, ReadOnlySpan<char> expression, out Type declaringType)
    {
        declaringType = runtimeType;
        expression = expression.Trim();

        if (expression.Length == 0)
        {
            return null;
        }

        var negate = false;

        if (expression[0] == '~')
        {
            negate = true;
            expression = expression[1..];
        }

        var operatorSpan = AdvancedSearchUtilities.FindOperatorIndex(expression, out var operatorIndex);

        if (operatorSpan.Length == 0)
        {
            return null;
        }

        var propertyName = expression[..operatorIndex].Trim();
        var valuePart = expression[(operatorIndex + operatorSpan.Length)..].Trim();

        if (valuePart.Length == 0)
        {
            return null;
        }

        var chain = Resolve(runtimeType, propertyName);

        if (chain == null)
        {
            return null;
        }

        declaringType = chain[0].DeclaringType!;

        var property = new Property(chain);
        var type = property.Type;
        var op = operatorSpan.ToString();
        var value = valuePart.ToString();

        ICondition condition;

        if (type == typeof(string))
        {
            condition = StringLeaf(property, negate, op, value);
        }
        else if (type == typeof(double) || type == typeof(float))
        {
            condition = EpsilonLeaf(property, negate, op, value);
        }
        else
        {
            condition = ComparisonLeaf(property, negate, op, value);
        }

        return condition != null && Probe(condition, runtimeType) ? condition : null;
    }

    // A leaf the compiler rejects -- a relational operator on a type with no CompareTo -- is
    // "no match" for that leaf, not an error for the whole search.
    private static bool Probe(ICondition condition, Type runtimeType)
    {
        try
        {
            condition.Build(Expression.Parameter(runtimeType, "probe"));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static ICondition StringLeaf(Property property, bool negate, string op, string value)
    {
        var (stringOp, ignoreCase) = op switch
        {
            "=" or "==" => (StringOperator.Equal, false),
            "!" or "!=" => (StringOperator.NotEqual, false),
            ">"         => (StringOperator.StartsWith, false),
            "<"         => (StringOperator.EndsWith, false),
            "~"         => (StringOperator.Contains, false),
            "~>"        => (StringOperator.StartsWith, true),
            "~<"        => (StringOperator.EndsWith, true),
            "~~"        => (StringOperator.Contains, true),
            "~="        => (StringOperator.Equal, true),
            "~!"        => (StringOperator.NotEqual, true),
            _           => ((StringOperator?)null, false)
        };

        if (stringOp == null)
        {
            return null;
        }

        // `null` is the null string for equality, as it is in a where clause; for the substring
        // operators, where a null needle means nothing, it is the four-letter word.
        if (value == "null" && stringOp is not (StringOperator.Equal or StringOperator.NotEqual))
        {
            value = @"@""null""";
        }

        return new StringCondition(property, negate, stringOp.Value, value, ignoreCase);
    }

    private static ICondition EpsilonLeaf(Property property, bool negate, string op, string value)
    {
        var comparison = MapOperator(op);

        if (comparison == null)
        {
            return null;
        }

        // A float property parses its value as a float first, so the widened constant carries the
        // same rounding the property's own value does.
        double parsed;

        if (property.Type == typeof(float))
        {
            if (!float.TryParse(value, null, out var f))
            {
                return null;
            }

            parsed = f;
        }
        else if (!double.TryParse(value, null, out parsed))
        {
            return null;
        }

        return new EpsilonCondition(property, negate, comparison.Value, parsed, AdvancedSearchUtilities.CalculateEpsilon(value));
    }

    private static ICondition ComparisonLeaf(Property property, bool negate, string op, string value)
    {
        var comparison = MapOperator(op);

        if (comparison == null)
        {
            return null;
        }

        var type = property.Type;
        var underlying = Nullable.GetUnderlyingType(type);
        object parsed;

        if (value == "null" && (underlying != null || !type.IsValueType))
        {
            parsed = null;
        }
        else if (underlying == null && type == typeof(bool))
        {
            // The gump accepts the switch words as well as the literals.
            if (comparison is not (ComparisonOperator.Equal or ComparisonOperator.NotEqual))
            {
                return null;
            }

            parsed = value.ToLowerInvariant() switch
            {
                "true" or "1" or "enabled" or "on"    => true,
                "false" or "0" or "disabled" or "off" => false,
                _                                     => null
            };

            if (parsed == null)
            {
                return null;
            }
        }
        else if (Types.TryParse(underlying ?? type, value, out parsed) != null)
        {
            return null;
        }

        return new ComparisonCondition(property, negate, comparison.Value, parsed);
    }

    private static ComparisonOperator? MapOperator(string op) =>
        op switch
        {
            "=" or "==" => ComparisonOperator.Equal,
            "!" or "!=" => ComparisonOperator.NotEqual,
            ">"         => ComparisonOperator.Greater,
            "<"         => ComparisonOperator.Lesser,
            ">="        => ComparisonOperator.GreaterEqual,
            "<="        => ComparisonOperator.LesserEqual,
            _           => null
        };

    // Case-insensitive, first readable match per link, the way the gump has always resolved a
    // name. A dotted name walks into the property's type.
    private static PropertyInfo[] Resolve(Type type, ReadOnlySpan<char> name)
    {
        var count = name.Count('.') + 1;
        var chain = new PropertyInfo[count];

        for (var i = 0; i < count; ++i)
        {
            var dot = name.IndexOf('.');
            var segment = dot == -1 ? name : name[..dot];
            name = dot == -1 ? default : name[(dot + 1)..];

            var found = Find(type, segment.Trim());

            if (found == null)
            {
                return null;
            }

            chain[i] = found;
            type = found.PropertyType;
        }

        return chain;
    }

    private static PropertyInfo Find(Type type, ReadOnlySpan<char> name)
    {
        var properties = _properties.GetOrAdd(type, static t => Readable(t));

        for (var i = 0; i < properties.Length; ++i)
        {
            if (name.InsensitiveEquals(properties[i].Name))
            {
                return properties[i];
            }
        }

        return null;
    }

    private static PropertyInfo[] Readable(Type type)
    {
        var all = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var readable = new List<PropertyInfo>(all.Length);

        for (var i = 0; i < all.Length; ++i)
        {
            if (all[i].CanRead && all[i].GetIndexParameters().Length == 0)
            {
                readable.Add(all[i]);
            }
        }

        return readable.ToArray();
    }

    /// <summary>
    /// A floating-point comparison with the tolerance the gump derives from the typed value: a
    /// value with no decimal point compares to within 1E-10, one with ten or more decimals to
    /// within the last digit typed.
    /// </summary>
    private sealed class EpsilonCondition : ICondition
    {
        private static readonly MethodInfo _abs = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(double)])!;

        private readonly Property _property;
        private readonly bool _not;
        private readonly ComparisonOperator _operator;
        private readonly double _value;
        private readonly double _epsilon;

        public EpsilonCondition(Property property, bool not, ComparisonOperator op, double value, double epsilon)
        {
            _property = property;
            _not = not;
            _operator = op;
            _value = value;
            _epsilon = epsilon;
        }

        public Expression Build(ParameterExpression target) =>
            PropertyExpressions.Chain(
                target,
                _property,
                read =>
                {
                    var value = read.Type == typeof(double) ? read : Expression.Convert(read, typeof(double));
                    var constant = Expression.Constant(_value);
                    var epsilon = Expression.Constant(_epsilon);
                    var distance = Expression.Call(_abs, Expression.Subtract(value, constant));

                    Expression test = _operator switch
                    {
                        ComparisonOperator.Equal        => Expression.LessThan(distance, epsilon),
                        ComparisonOperator.NotEqual     => Expression.GreaterThanOrEqual(distance, epsilon),
                        ComparisonOperator.Greater      => Expression.GreaterThan(value, Expression.Add(constant, epsilon)),
                        ComparisonOperator.Lesser       => Expression.LessThan(value, Expression.Subtract(constant, epsilon)),
                        ComparisonOperator.GreaterEqual => Expression.GreaterThanOrEqual(value, Expression.Subtract(constant, epsilon)),
                        ComparisonOperator.LesserEqual  => Expression.LessThanOrEqual(value, Expression.Add(constant, epsilon)),
                        _                               => throw new InvalidOperationException("Invalid comparison operator.")
                    };

                    return _not ? Expression.Not(test) : test;
                },
                Expression.Constant(false)
            );
    }
}
