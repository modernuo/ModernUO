using System;
using Server;
using Server.Commands;
using Server.Commands.Generic;
using Xunit;

namespace UOContent.Tests.Commands;

// The conditional compiler emits into a fresh dynamic assembly for every command invocation --
// there is no per-type cache, so `Run` (which can never be unloaded) grows the process by one
// assembly per `[global where`. And PropertyValue could only load a handful of primitive constant
// types, so a comparison against anything narrower or unsigned than int threw instead of running.
public class EmitterRobustnessTests
{
    public class Subject
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public byte Tiny { get; set; } = 5;

        [CommandProperty(AccessLevel.GameMaster)]
        public sbyte Signed { get; set; } = -5;

        [CommandProperty(AccessLevel.GameMaster)]
        public short Small { get; set; } = -300;

        [CommandProperty(AccessLevel.GameMaster)]
        public ushort Key { get; set; } = 40000;

        [CommandProperty(AccessLevel.GameMaster)]
        public uint Big { get; set; } = 3_000_000_000;

        [CommandProperty(AccessLevel.GameMaster)]
        public ulong Huge { get; set; } = 18_000_000_000_000_000_000;
    }

    private static bool Check(string binding, ComparisonOperator op, string value)
    {
        var prop = new Property(binding);
        prop.BindTo(typeof(Subject), PropertyAccess.Read);

        var compiled = ConditionalCompiler.Compile(
            typeof(Subject),
            [TypeCondition.Default, new ComparisonCondition(prop, false, op, value)]
        );

        return compiled.Verify(new Subject());
    }

    [Theory]
    [InlineData("Tiny", "5")]
    [InlineData("Signed", "-5")]
    [InlineData("Small", "-300")]
    [InlineData("Key", "40000")]
    [InlineData("Big", "3000000000")]
    [InlineData("Huge", "18000000000000000000")]
    public void NarrowAndUnsignedIntegersCompareByEquality(string binding, string value)
    {
        Assert.True(Check(binding, ComparisonOperator.Equal, value));
        Assert.False(Check(binding, ComparisonOperator.NotEqual, value));
    }

    [Theory]
    [InlineData("Tiny", "4")]
    [InlineData("Signed", "-6")]
    [InlineData("Small", "-301")]
    [InlineData("Key", "39999")]
    [InlineData("Big", "2999999999")]
    [InlineData("Huge", "17999999999999999999")]
    public void NarrowAndUnsignedIntegersCompareRelationally(string binding, string value)
    {
        Assert.True(Check(binding, ComparisonOperator.Greater, value));
        Assert.False(Check(binding, ComparisonOperator.Lesser, value));
    }

    // Unsigned values above the signed range must not wrap into a negative comparison.
    [Fact]
    public void UnsignedComparisonsDoNotWrapThroughSignedMath()
    {
        Assert.True(Check("Big", ComparisonOperator.Greater, "2147483647"));
        Assert.True(Check("Huge", ComparisonOperator.Greater, "9223372036854775807"));
    }

    [Fact]
    public void EmittedAssembliesAreCollectible()
    {
        var prop = new Property("Tiny");
        prop.BindTo(typeof(Subject), PropertyAccess.Read);

        var compiled = ConditionalCompiler.Build(
            typeof(Subject),
            [TypeCondition.Default, new ComparisonCondition(prop, false, ComparisonOperator.Equal, "5")]
        ).Compile();

        var method = compiled.Method;

        Assert.True(
            method.IsCollectible,
            "The conditional compiler compiles one delegate per command invocation; code that is "
            + $"not collectible ({method.GetType().Name} in {method.Module}) can never be unloaded, "
            + "so every [global where would grow the process for good."
        );
    }
}
