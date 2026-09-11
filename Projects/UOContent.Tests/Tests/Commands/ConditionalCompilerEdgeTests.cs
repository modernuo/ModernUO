using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Commands.Generic;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands;

// Two shapes the IL emitter got wrong or could not express. A chained binding ending in a value
// type (Message.Number) reused a temp while its value was still live, so every pair compared
// equal and `sort by` was a no-op. A struct with no CompareTo fell through to a raw Ceq, which is
// not valid IL for a non-primitive struct. Both are pinned here against the expression-tree port.
[Collection("Sequential UOContent Tests")]
public class ConditionalCompilerEdgeTests : IDisposable
{
    private readonly List<Item> _items = [];

    public void Dispose()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Delete();
        }

        _items.Clear();
    }

    private SkillTeleporter Teleporter(TextDefinition message)
    {
        var tp = new SkillTeleporter { Message = message };
        _items.Add(tp);
        return tp;
    }

    private static Property Bind(Type type, string binding)
    {
        var prop = new Property(binding);
        prop.BindTo(type, PropertyAccess.Read);
        return prop;
    }

    [Fact]
    public void SortingOnAChainedValueTypeOrdersValues()
    {
        var low = Teleporter(TextDefinition.Of(1060847));
        var high = Teleporter(TextDefinition.Of(1060848));

        var comparer = SortCompiler.Compile<object>(
            typeof(SkillTeleporter),
            [new OrderInfo(Bind(typeof(SkillTeleporter), "Message.Number"), true)]
        );

        Assert.True(comparer.Compare(low, high) < 0);
        Assert.True(comparer.Compare(high, low) > 0);
        Assert.Equal(0, comparer.Compare(low, low));
    }

    [Fact]
    public void DistinctOnAChainedValueTypeSeparatesValues()
    {
        var low = Teleporter(TextDefinition.Of(1060847));
        var high = Teleporter(TextDefinition.Of(1060848));
        var alsoLow = Teleporter(TextDefinition.Of(1060847));

        var comparer = DistinctCompiler.Compile<object>(
            typeof(SkillTeleporter),
            [Bind(typeof(SkillTeleporter), "Message.Number")]
        );

        Assert.NotEqual(0, comparer.Compare(low, high));
        Assert.Equal(0, comparer.Compare(low, alsoLow));
    }

    public class Subject
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D Bounds { get; set; } = new(new Point2D(1, 2), new Point2D(3, 4));
    }

    private static bool Check(ComparisonOperator op, string value)
    {
        var compiled = ConditionalCompiler.Compile(
            typeof(Subject),
            [TypeCondition.Default, new ComparisonCondition(Bind(typeof(Subject), "Bounds"), false, op, value)]
        );

        return compiled.Verify(new Subject());
    }

    [Fact]
    public void NonComparableStructComparesByValueEquality()
    {
        Assert.True(Check(ComparisonOperator.Equal, "(1, 2)+(3, 4)"));
        Assert.False(Check(ComparisonOperator.Equal, "(1, 2)+(3, 5)"));
        Assert.False(Check(ComparisonOperator.NotEqual, "(1, 2)+(3, 4)"));
        Assert.True(Check(ComparisonOperator.NotEqual, "(1, 2)+(3, 5)"));
    }

    // Only == and != are meaningful without a CompareTo; a relational operator is an error at
    // compile time, not garbage at run time.
    [Theory]
    [InlineData(ComparisonOperator.Greater)]
    [InlineData(ComparisonOperator.GreaterEqual)]
    [InlineData(ComparisonOperator.Lesser)]
    [InlineData(ComparisonOperator.LesserEqual)]
    public void NonComparableStructRejectsRelationalOperators(ComparisonOperator op)
    {
        Assert.Throws<InvalidOperationException>(() => Check(op, "(1, 2)+(3, 4)"));
    }
}
