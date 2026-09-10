using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Server.Commands.Generic;

/// <summary>
/// Expression-tree fragments over a bound <see cref="Property" /> chain, shared by the
/// conditional, sort and distinct compilers. Everything here builds an <see cref="Expression" />;
/// the compilers assemble those into a lambda and hand <c>Compile()</c> the codegen.
/// </summary>
public static class PropertyExpressions
{
    private static readonly MethodInfo _objectEquals = typeof(object).GetMethod(
        nameof(object.Equals),
        BindingFlags.Public | BindingFlags.Static,
        [typeof(object), typeof(object)]
    )!;

    /// <summary>
    /// Walks a property binding. A binding of more than one property (<c>Message.Number</c>)
    /// dereferences each link in turn, and any link but the last can legitimately be null -- an
    /// unset TextDefinition, an unparented item. Each reference-typed intermediate link is stored
    /// once and null-checked; a null there yields <paramref name="whenUnreadable" /> in place of
    /// whatever <paramref name="onValue" /> would have built from the final link.
    /// </summary>
    public static Expression Chain(
        Expression target,
        Property prop,
        Func<Expression, Expression> onValue,
        Expression whenUnreadable
    ) => ChainFrom(target, prop.Chain, 0, onValue, whenUnreadable);

    private static Expression ChainFrom(
        Expression current,
        PropertyInfo[] chain,
        int index,
        Func<Expression, Expression> onValue,
        Expression whenUnreadable
    )
    {
        var link = Expression.Property(current, chain[index]);

        // The last link is the value being tested, so a null there is the caller's business.
        if (index == chain.Length - 1)
        {
            return onValue(link);
        }

        if (link.Type.IsValueType)
        {
            return ChainFrom(link, chain, index + 1, onValue, whenUnreadable);
        }

        var local = Expression.Variable(link.Type, chain[index].Name);

        return Expression.Block(
            [local],
            Expression.Assign(local, link),
            Expression.Condition(
                Expression.ReferenceNotEqual(local, Expression.Constant(null, local.Type)),
                ChainFrom(local, chain, index + 1, onValue, whenUnreadable),
                whenUnreadable
            )
        );
    }

    /// <summary>
    /// Walks a binding for a caller that has no way to express "no match" -- ordering and
    /// grouping, where the value itself is the answer rather than a yes or no. An unreadable
    /// link yields <c>default(T)</c>, which is what a null link along the way amounts to; the
    /// comparers these feed already handle a null value.
    /// </summary>
    public static Expression ChainOrDefault(Expression target, Property prop) =>
        Chain(target, prop, static value => value, Expression.Default(prop.Type));

    /// <summary>
    /// Equality for the "not comparable" path, which supports only == and !=. Reference equality
    /// would miss a type whose equality is by value -- <see cref="TextDefinition" /> among them --
    /// so this is static <c>object.Equals</c>, which honors the override and is null-safe on
    /// either side. Value types box; they only reach here when they have no <c>CompareTo</c>.
    /// </summary>
    public static Expression ValueEquals(Expression a, Expression b) =>
        Expression.Call(_objectEquals, Box(a), Box(b));

    private static Expression Box(Expression e) =>
        e.Type == typeof(object) ? e : Expression.Convert(e, typeof(object));

    /// <summary>
    /// A boolean test of <paramref name="a" /> against <paramref name="b" />. Integral primitives
    /// and enums compare with the operator itself -- lifted over <c>Nullable&lt;T&gt;</c>, and on
    /// the unsigned types as unsigned; nothing here widens to a signed type. Everything else goes
    /// through <see cref="TryCompare" /> and tests the sign of the result. False when the type
    /// has no <c>CompareTo</c> at all, in which case only equality is meaningful.
    /// </summary>
    public static bool TryRelational(Expression a, Expression b, ComparisonOperator op, out Expression test)
    {
        var type = a.Type;
        var nonNullable = Nullable.GetUnderlyingType(type) ?? type;

        if (nonNullable.IsEnum)
        {
            // Equal/NotEqual are defined on enums; the relational operators are not, so those
            // read the underlying integer. Convert lifts over Nullable<E> on its own.
            if (op is not (ComparisonOperator.Equal or ComparisonOperator.NotEqual))
            {
                var underlying = Enum.GetUnderlyingType(nonNullable);

                if (nonNullable != type)
                {
                    underlying = typeof(Nullable<>).MakeGenericType(underlying);
                }

                a = Expression.Convert(a, underlying);
                b = Expression.Convert(b, underlying);
            }

            test = Relational(a, b, op);
            return true;
        }

        if (IsIntegral(nonNullable))
        {
            test = Relational(a, b, op);
            return true;
        }

        if (!TryCompare(a, b, 1, out var comparison))
        {
            test = null;
            return false;
        }

        test = Relational(comparison, Expression.Constant(0), op);
        return true;
    }

    private static Expression Relational(Expression a, Expression b, ComparisonOperator op) =>
        op switch
        {
            ComparisonOperator.Equal        => Expression.Equal(a, b),
            ComparisonOperator.NotEqual     => Expression.NotEqual(a, b),
            ComparisonOperator.Greater      => Expression.GreaterThan(a, b),
            ComparisonOperator.GreaterEqual => Expression.GreaterThanOrEqual(a, b),
            ComparisonOperator.Lesser       => Expression.LessThan(a, b),
            ComparisonOperator.LesserEqual  => Expression.LessThanOrEqual(a, b),
            _                               => throw new InvalidOperationException("Invalid comparison operator.")
        };

    private static bool IsIntegral(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(uint) || type == typeof(ulong)
        || type == typeof(short) || type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte);

    /// <summary>
    /// An <c>int</c>-valued comparison of <paramref name="a" /> against <paramref name="b" />
    /// with <c>CompareTo</c> semantics, multiplied by <paramref name="sign" />. A null on either
    /// side of a reference or nullable type is handled here rather than in the callee:
    /// <c>null.CompareTo(null) = 0</c>, <c>real.CompareTo(null) = -sign</c>,
    /// <c>null.CompareTo(real) = +sign</c>. False when the type has no <c>CompareTo</c>.
    /// </summary>
    public static bool TryCompare(Expression a, Expression b, int sign, out Expression comparison)
    {
        var type = a.Type;

        // Both sides are read more than once below; pin them so a chained binding is walked once.
        var left = Expression.Variable(type, "left");
        var right = Expression.Variable(type, "right");

        if (!TryCompareValues(left, right, sign, out var body))
        {
            comparison = null;
            return false;
        }

        comparison = Expression.Block(
            [left, right],
            Expression.Assign(left, a),
            Expression.Assign(right, b),
            body
        );

        return true;
    }

    private static bool TryCompareValues(Expression a, Expression b, int sign, out Expression comparison)
    {
        var type = a.Type;
        var underlying = Nullable.GetUnderlyingType(type);

        if (underlying != null)
        {
            if (!TryCompareValues(Expression.Property(a, "Value"), Expression.Property(b, "Value"), sign, out var inner))
            {
                comparison = null;
                return false;
            }

            comparison = NullAware(Expression.Property(a, "HasValue"), Expression.Property(b, "HasValue"), inner, sign);
            return true;
        }

        if (type.IsEnum)
        {
            var integer = Enum.GetUnderlyingType(type);

            return TryCompareValues(Expression.Convert(a, integer), Expression.Convert(b, integer), sign, out comparison);
        }

        var compareTo = FindCompareTo(type);

        if (compareTo == null)
        {
            comparison = null;
            return false;
        }

        var parameterType = compareTo.GetParameters()[0].ParameterType;
        var argument = parameterType == type ? b : Expression.Convert(b, parameterType);

        Expression call = Expression.Call(a, compareTo, argument);

        if (sign == -1)
        {
            call = Expression.Negate(call);
        }

        if (type.IsValueType)
        {
            comparison = call;
            return true;
        }

        var nil = Expression.Constant(null, type);

        comparison = NullAware(Expression.ReferenceNotEqual(a, nil), Expression.ReferenceNotEqual(b, nil), call, sign);
        return true;
    }

    private static Expression NullAware(Expression aHasValue, Expression bHasValue, Expression compare, int sign) =>
        Expression.Condition(
            aHasValue,
            Expression.Condition(bHasValue, compare, Expression.Constant(-sign)),
            Expression.Condition(bHasValue, Expression.Constant(sign), Expression.Constant(0))
        );

    private static MethodInfo FindCompareTo(Type type)
    {
        var compareTo = type.GetMethod("CompareTo", [type]);

        if (compareTo != null)
        {
            return compareTo;
        }

        /* There's a scenario where we might be trying to use CompareTo on an interface
         * which, while it doesn't explicitly implement CompareTo itself, is said to
         * extend IComparable indirectly. The implementation is implicitly passed off
         * to implementers, so the interface's own GetMethod("CompareTo") returns null.
         */
        var ifaces = type.FindInterfaces(
            static (iface, _) => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IComparable<>),
            null
        );

        for (var i = 0; i < ifaces.Length; ++i)
        {
            if (ifaces[i].GetGenericArguments()[0].IsAssignableFrom(type))
            {
                return ifaces[i].GetMethod("CompareTo", [type]);
            }
        }

        return typeof(IComparable).IsAssignableFrom(type)
            ? typeof(IComparable).GetMethod("CompareTo", [typeof(object)])
            : null;
    }

    /// <summary>
    /// The right-hand side of a condition as a typed constant. A string is parsed the way the
    /// props gump would parse it: <c>null</c> for a reference or nullable type, <c>@"null"</c>
    /// for the literal string, names for enums, hex with a <c>0x</c> prefix for the numerics,
    /// and the type's own static <c>Parse</c> for everything else.
    /// </summary>
    public static ConstantExpression Constant(Type type, object value)
    {
        if (value is string text)
        {
            value = Parse(type, text);
        }

        return Expression.Constant(value, type);
    }

    private static object Parse(Type type, string text)
    {
        var underlying = Nullable.GetUnderlyingType(type);

        if (text == "null" && (underlying != null || !type.IsValueType))
        {
            return null;
        }

        var target = underlying ?? type;

        if (target == typeof(string))
        {
            return text == @"@""null""" ? "null" : text;
        }

        if (target.IsEnum)
        {
            return Enum.Parse(target, text, true);
        }

        if (target == typeof(bool))
        {
            return bool.Parse(text);
        }

        var parseNumber = target.GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            Types.ParseStringNumericParamTypes,
            null
        );

        if (parseNumber != null)
        {
            var style = NumberStyles.Integer;

            if (text.InsensitiveStartsWith("0x"))
            {
                style = NumberStyles.HexNumber;
                text = text[2..];
            }

            return parseNumber.Invoke(null, [text, style]);
        }

        var parseGeneral = target.GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            Types.ParseStringParamTypes,
            null
        );

        if (parseGeneral != null)
        {
            return parseGeneral.Invoke(null, [text, null]);
        }

        throw new InvalidOperationException($"Unable to convert string \"{text}\" into type '{type}'.");
    }
}
