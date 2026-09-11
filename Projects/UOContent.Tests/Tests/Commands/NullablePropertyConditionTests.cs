using System;
using Server;
using Server.Commands;
using Server.Commands.Generic;
using Xunit;

namespace UOContent.Tests.Commands;

// A `where` clause against a Nullable<T> property. No [CommandProperty] in the tree is nullable
// yet, so nothing is broken in practice -- but the conditional compiler should not fall over on
// one either: a set value compares as its underlying type, an unset one equals `null` and
// satisfies no relation, just as C#'s lifted operators would have it.
public class NullablePropertyConditionTests
{
    public class Subject
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int? Count { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan? Delay { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName? Skill { get; set; }
    }

    private static bool Check(Subject subject, string binding, ComparisonOperator op, string value)
    {
        var prop = new Property(binding);
        prop.BindTo(typeof(Subject), PropertyAccess.Read);

        var conditional = new ObjectConditional(
            typeof(Subject),
            [[TypeCondition.Default, new ComparisonCondition(prop, false, op, value)]]
        );

        return conditional.CheckCondition(subject);
    }

    [Fact]
    public void SetValueComparesByEquality()
    {
        var subject = new Subject { Count = 5 };

        Assert.True(Check(subject, "Count", ComparisonOperator.Equal, "5"));
        Assert.False(Check(subject, "Count", ComparisonOperator.Equal, "4"));
        Assert.True(Check(subject, "Count", ComparisonOperator.NotEqual, "4"));
        Assert.False(Check(subject, "Count", ComparisonOperator.NotEqual, "5"));
    }

    [Fact]
    public void SetValueComparesRelationally()
    {
        var subject = new Subject { Count = 5 };

        Assert.True(Check(subject, "Count", ComparisonOperator.Greater, "4"));
        Assert.False(Check(subject, "Count", ComparisonOperator.Lesser, "4"));
        Assert.True(Check(subject, "Count", ComparisonOperator.GreaterEqual, "5"));
        Assert.True(Check(subject, "Count", ComparisonOperator.LesserEqual, "5"));
    }

    [Fact]
    public void UnsetValueEqualsNull()
    {
        Assert.True(Check(new Subject(), "Count", ComparisonOperator.Equal, "null"));
        Assert.False(Check(new Subject { Count = 5 }, "Count", ComparisonOperator.Equal, "null"));
        Assert.True(Check(new Subject { Count = 5 }, "Count", ComparisonOperator.NotEqual, "null"));
    }

    // Lifted semantics: null is never greater, lesser, or equal to a value -- only unequal.
    [Fact]
    public void UnsetValueSatisfiesNoRelation()
    {
        var subject = new Subject();

        Assert.False(Check(subject, "Count", ComparisonOperator.Equal, "0"));
        Assert.True(Check(subject, "Count", ComparisonOperator.NotEqual, "0"));
        Assert.False(Check(subject, "Count", ComparisonOperator.Greater, "0"));
        Assert.False(Check(subject, "Count", ComparisonOperator.GreaterEqual, "0"));
        Assert.False(Check(subject, "Count", ComparisonOperator.Lesser, "0"));
        Assert.False(Check(subject, "Count", ComparisonOperator.LesserEqual, "0"));
    }

    // A struct that compares through CompareTo rather than a primitive operator.
    [Fact]
    public void NullableStructComparesThroughCompareTo()
    {
        var set = new Subject { Delay = TimeSpan.FromSeconds(5) };

        Assert.True(Check(set, "Delay", ComparisonOperator.Equal, "00:00:05"));
        Assert.True(Check(set, "Delay", ComparisonOperator.Greater, "00:00:04"));
        Assert.False(Check(set, "Delay", ComparisonOperator.Lesser, "00:00:04"));
        Assert.False(Check(set, "Delay", ComparisonOperator.Equal, "null"));

        var unset = new Subject();

        Assert.True(Check(unset, "Delay", ComparisonOperator.Equal, "null"));
        Assert.False(Check(unset, "Delay", ComparisonOperator.Greater, "00:00:04"));
        Assert.False(Check(unset, "Delay", ComparisonOperator.Lesser, "00:00:04"));
    }

    [Fact]
    public void NullableEnumComparesByNameAndOrder()
    {
        var set = new Subject { Skill = SkillName.Magery };

        Assert.True(Check(set, "Skill", ComparisonOperator.Equal, "Magery"));
        Assert.False(Check(set, "Skill", ComparisonOperator.Equal, "Anatomy"));
        Assert.True(Check(set, "Skill", ComparisonOperator.Greater, "Alchemy"));
        Assert.False(Check(set, "Skill", ComparisonOperator.Equal, "null"));

        var unset = new Subject();

        Assert.True(Check(unset, "Skill", ComparisonOperator.Equal, "null"));
        Assert.False(Check(unset, "Skill", ComparisonOperator.Equal, "Magery"));
        Assert.False(Check(unset, "Skill", ComparisonOperator.Greater, "Alchemy"));
    }

    // `sort by` on a nullable: values order by the underlying type and an unset value takes a
    // consistent place at one end, the same convention a null reference already had.
    [Fact]
    public void SortingOnANullableOrdersValuesAndPlacesUnsetConsistently()
    {
        var prop = new Property("Count");
        prop.BindTo(typeof(Subject), PropertyAccess.Read);

        var comparer = SortCompiler.Compile<object>(typeof(Subject), [new OrderInfo(prop, true)]);

        var low = new Subject { Count = 1 };
        var high = new Subject { Count = 2 };
        var unset = new Subject();

        Assert.True(comparer.Compare(low, high) < 0);
        Assert.True(comparer.Compare(high, low) > 0);
        Assert.Equal(0, comparer.Compare(low, low));

        var forward = comparer.Compare(high, unset);

        Assert.NotEqual(0, forward);
        Assert.Equal(-Math.Sign(forward), Math.Sign(comparer.Compare(unset, high)));
        Assert.Equal(0, comparer.Compare(unset, new Subject()));
    }
}
