using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Server.Commands;
using Server.Commands.Generic;
using Server.Logging;

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
/// <c>Expression.Compile</c>, and neither touches game state. An entity-typed value compares by
/// serial and never looks the entity up. Property names resolve through
/// <see cref="CommandPropertyGuard" /> at the searcher's access level, and each leaf is classified
/// as worker-safe or loop-only (see <see cref="SearchPredicate" />).
/// </remarks>
public static class AdvancedSearchConditions
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(AdvancedSearchConditions));

    // Beyond this many leaves the safety mask no longer fits, so every leaf runs on the loop.
    private const int MaxMaskedLeaves = 64;

    /// <summary>
    /// Per-search memo shared by every worker. Predicates are keyed twice: by the runtime type
    /// seen, and by the type the predicate was actually compiled for -- the most derived type
    /// that declares one of the properties -- plus which leaves are worker-safe, so every subclass
    /// of Item that does not hide <c>Hue</c> shares one compiled <c>Hue = 5</c>.
    /// </summary>
    public sealed class Cache
    {
        internal readonly ConcurrentDictionary<Type, SearchPredicate> ByRuntimeType = new();
        internal readonly ConcurrentDictionary<(Type Type, ulong SafeMask), SearchPredicate> ByCompiledType = new();

        public Cache(AccessLevel accessLevel) => AccessLevel = accessLevel;

        public AccessLevel AccessLevel { get; }
    }

    public static SearchPredicate GetPredicate(Cache cache, Type runtimeType, string propertyTest) =>
        cache.ByRuntimeType.GetOrAdd(
            runtimeType,
            static (type, state) => Build(state.cache, type, state.propertyTest),
            (cache, propertyTest)
        );

    /// <summary>Compiles the full predicate without a shared cache. Test seam and one-off use.</summary>
    public static Func<object, bool> Compile(
        Type runtimeType, string propertyTest, AccessLevel accessLevel = AccessLevel.Owner
    )
    {
        var predicate = GetPredicate(new Cache(accessLevel), runtimeType, propertyTest);

        return predicate.Loop ?? predicate.Worker;
    }

    private static SearchPredicate Build(Cache cache, Type runtimeType, string propertyTest)
    {
        var groups = Parse(cache.AccessLevel, runtimeType, propertyTest, out var compiledType, out var safe, out var mask);

        if (groups == null)
        {
            return SearchPredicate.Never;
        }

        return cache.ByCompiledType.GetOrAdd(
            (compiledType, mask),
            static (key, state) => CompileSplit(key.Type, state.groups, state.safe),
            (groups, safe)
        );
    }

    private static SearchPredicate CompileSplit(Type compiledType, ICondition[][] groups, bool[][] safe)
    {
        try
        {
            var allSafe = true;
            var workerGroups = new ICondition[groups.Length][];

            for (var g = 0; g < groups.Length; ++g)
            {
                // Index 0 is the TypeCondition; it stays so the worker predicate keeps its null guard.
                var kept = new List<ICondition>(groups[g].Length) { groups[g][0] };

                for (var i = 1; i < groups[g].Length; ++i)
                {
                    if (safe[g][i])
                    {
                        kept.Add(groups[g][i]);
                    }
                    else
                    {
                        allSafe = false;
                    }
                }

                workerGroups[g] = kept.ToArray();
            }

            if (allSafe)
            {
                return new SearchPredicate(ConditionalCompiler.Build(compiledType, groups).Compile(), null);
            }

            return new SearchPredicate(
                ConditionalCompiler.Build(compiledType, workerGroups).Compile(),
                ConditionalCompiler.Build(compiledType, groups).Compile()
            );
        }
        catch (Exception)
        {
            return SearchPredicate.Never;
        }
    }

    // OR of ANDs, which is what splitting on '|' and then on '@' yields. A group with a dead leaf
    // is dropped; no live group left means nothing can match, reported as null. `safe` parallels
    // the groups (index 0 is the TypeCondition) and `mask` carries one bit per live leaf, in
    // order, for the compiled-type cache key.
    private static ICondition[][] Parse(
        AccessLevel level, Type runtimeType, string propertyTest, out Type compiledType, out bool[][] safe, out ulong mask
    )
    {
        Type mostDerived = null;

        var groups = new List<ICondition[]>();
        var safeGroups = new List<bool[]>();
        var leafCount = 0;
        mask = 0;

        foreach (var orPart in propertyTest.Split('|'))
        {
            var group = new List<ICondition> { TypeCondition.Default };
            var groupSafe = new List<bool> { true };
            var alive = true;

            foreach (var andPart in orPart.Split('@'))
            {
                var leaf = Leaf(level, runtimeType, andPart, out var declaringType, out var leafSafe);

                if (leaf == null)
                {
                    alive = false;
                    break;
                }

                group.Add(leaf);
                groupSafe.Add(leafSafe);

                // Every declaring type is an ancestor of the runtime type (or the type itself), so
                // they nest; the predicate is compiled for the most derived one any leaf needs and
                // serves every runtime type that resolves the same properties.
                if (mostDerived == null || declaringType.IsAssignableTo(mostDerived))
                {
                    mostDerived = declaringType;
                }
            }

            if (!alive)
            {
                continue;
            }

            for (var i = 1; i < groupSafe.Count; ++i)
            {
                if (groupSafe[i] && leafCount < MaxMaskedLeaves)
                {
                    mask |= 1UL << leafCount;
                }

                ++leafCount;
            }

            groups.Add(group.ToArray());
            safeGroups.Add(groupSafe.ToArray());
        }

        compiledType = mostDerived ?? runtimeType;

        if (leafCount > MaxMaskedLeaves)
        {
            mask = 0;

            for (var g = 0; g < safeGroups.Count; ++g)
            {
                for (var i = 1; i < safeGroups[g].Length; ++i)
                {
                    safeGroups[g][i] = false;
                }
            }
        }

        safe = safeGroups.ToArray();

        return groups.Count > 0 ? groups.ToArray() : null;
    }

    // A leaf that throws while binding is a dead leaf, so the groups beside it still work. Leaves
    // are bound once per runtime type per search, which bounds the warning.
    private static ICondition Leaf(
        AccessLevel level, Type runtimeType, ReadOnlySpan<char> expression, out Type declaringType, out bool workerSafe
    )
    {
        try
        {
            return BindLeaf(level, runtimeType, expression, out declaringType, out workerSafe);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "AdvancedSearch: could not bind {Leaf} on {Type}; treating it as no match", expression.ToString(), runtimeType);
            declaringType = runtimeType;
            workerSafe = false;
            return null;
        }
    }

    private static ICondition BindLeaf(
        AccessLevel level, Type runtimeType, ReadOnlySpan<char> expression, out Type declaringType, out bool workerSafe
    )
    {
        declaringType = runtimeType;
        workerSafe = false;
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

        var chain = CommandPropertyGuard.ResolveChain(
            runtimeType, TrimSegments(propertyName), level, PropertyAccess.Read, false, out _
        );

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
        else if (typeof(IEntity).IsAssignableFrom(type))
        {
            condition = EntityLeaf(property, negate, op, value);
        }
        else
        {
            condition = ComparisonLeaf(property, negate, op, value);
        }

        if (condition == null || !Probe(condition, runtimeType))
        {
            return null;
        }

        workerSafe = IsSimpleValue(type) && PureGetterClassifier.IsPure(chain, runtimeType);

        return condition;
    }

    // A dotted name tolerates spaces around each link ("Child . Value").
    private static string TrimSegments(ReadOnlySpan<char> name)
    {
        if (name.IndexOf('.') == -1)
        {
            return name.ToString();
        }

        var segments = name.ToString().Split('.');

        for (var i = 0; i < segments.Length; ++i)
        {
            segments[i] = segments[i].Trim();
        }

        return string.Join('.', segments);
    }

    // Types whose comparison runs no user code: the compiler reads them with operators and
    // framework CompareTo/Equals, so only the getter chain decides whether a leaf is worker-safe.
    private static bool IsSimpleValue(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Serial)
               || type == typeof(TimeSpan) || type == typeof(DateTime) || type == typeof(Point3D)
               || type == typeof(Point2D) || typeof(IEntity).IsAssignableFrom(type);
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

    // An entity compares by serial: a value naming an entity that does not exist matches nothing,
    // and no lookup into the world ever happens. `null` tests the reference itself.
    private static ICondition EntityLeaf(Property property, bool negate, string op, string value)
    {
        var comparison = MapOperator(op);

        if (comparison == null)
        {
            return null;
        }

        var equality = comparison is ComparisonOperator.Equal or ComparisonOperator.NotEqual;

        // Ordering by serial is what Item and Mobile's own CompareTo does; a type without one
        // (an interface) has no ordering, so that leaf is dead.
        if (!equality && !PropertyExpressions.TryCompare(
                Expression.Default(property.Type), Expression.Default(property.Type), 1, out _
            ))
        {
            return null;
        }

        if (value == "null")
        {
            return new EntityCondition(property, negate, comparison.Value, null);
        }

        // A value Types cannot read as a serial can come back as something other than a Serial.
        return Types.TryParse(typeof(Serial), value, out var parsed) == null && parsed is Serial serial
            ? new EntityCondition(property, negate, comparison.Value, serial)
            : null;
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

    /// <summary>
    /// <c>prop != null &amp;&amp; prop.Serial &lt;op&gt; value</c>, <c>!=</c> being the negation of
    /// <c>=</c>, or a null test when the value is <c>null</c>.
    /// </summary>
    private sealed class EntityCondition : ICondition
    {
        private static readonly MethodInfo _serial = typeof(ISerializable).GetProperty(nameof(ISerializable.Serial))!.GetMethod!;

        private readonly Property _property;
        private readonly bool _not;
        private readonly ComparisonOperator _operator;
        private readonly Serial? _value;

        public EntityCondition(Property property, bool not, ComparisonOperator op, Serial? value)
        {
            _property = property;
            _not = not;
            _operator = op;
            _value = value;
        }

        public Expression Build(ParameterExpression target) =>
            PropertyExpressions.Chain(
                target,
                _property,
                read =>
                {
                    var entity = Expression.Variable(read.Type, "entity");
                    var present = Expression.ReferenceNotEqual(entity, Expression.Constant(null, entity.Type));
                    Expression test;

                    if (_value == null)
                    {
                        // Null ordering as the comparison compiler gives it: null vs null is 0 and a
                        // present reference sorts below null (-1), so test that against 0.
                        test = _operator switch
                        {
                            ComparisonOperator.Equal        => Expression.Not(present),
                            ComparisonOperator.NotEqual     => present,
                            ComparisonOperator.Lesser       => present,
                            ComparisonOperator.LesserEqual  => Expression.Constant(true),
                            ComparisonOperator.Greater      => Expression.Constant(false),
                            _                               => Expression.Not(present)
                        };
                    }
                    else
                    {
                        PropertyExpressions.TryRelational(
                            Expression.Call(Expression.Convert(entity, typeof(ISerializable)), _serial),
                            Expression.Constant(_value.Value),
                            _operator,
                            out var relation
                        );

                        // != is the exact negation of =, so a null reference is "not equal".
                        test = _operator == ComparisonOperator.NotEqual
                            ? Expression.OrElse(Expression.Not(present), relation)
                            : Expression.AndAlso(present, relation);
                    }

                    return Expression.Block(
                        [entity],
                        Expression.Assign(entity, read),
                        _not ? Expression.Not(test) : test
                    );
                },
                Expression.Constant(false)
            );
    }
}
