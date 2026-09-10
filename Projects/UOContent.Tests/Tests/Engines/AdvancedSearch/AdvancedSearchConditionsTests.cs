using System;
using Server;
using Server.Engines.AdvancedSearch;
using Xunit;

namespace UOContent.Tests;

// The Advanced Search property test compiles through the same conditions a `where` clause does.
// The grammar is the gump's own; these pin its operators and precedence, and that a leaf which
// cannot be resolved or parsed is "no match" rather than an exception on the search worker.
public class AdvancedSearchConditionsTests
{
    public sealed class Inner
    {
        public int Value { get; set; } = 7;
    }

    public sealed class LegacyParseType
    {
        public string Value { get; private init; }
        public static LegacyParseType Parse(string s) => new() { Value = s };
        public override bool Equals(object obj) => obj is LegacyParseType o && o.Value == Value;
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
    }

    public class Subject
    {
        public int Hue { get; set; } = 5;
        public string Name { get; set; } = "Moongate";
        public bool Movable { get; set; } = true;
        public bool Visible { get; set; }
        public double Weight { get; set; } = 0.1 + 0.2;
        public float Ratio { get; set; } = 0.1f;
        public TimeSpan Delay { get; set; } = TimeSpan.FromMinutes(5);
        public Guid Id { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public Layer Layer { get; set; } = Layer.OneHanded;
        public object Reference { get; set; } = new();
        public LegacyParseType Legacy { get; set; } = LegacyParseType.Parse("alpha");
        public Inner Child { get; set; } = new();
        public int? Maybe { get; set; }
    }

    public sealed class Derived : Subject
    {
    }

    private static bool Check(string test, Subject subject = null) =>
        AdvancedSearchConditions.Compile(typeof(Subject), test)(subject ?? new Subject());

    [Theory]
    [InlineData("Hue=abc")]         // not a number
    [InlineData("Hue=99999999999")] // overflows int
    [InlineData("Hue=0xZZ")]        // bad hex
    [InlineData("Layer=Bogus")]     // not an enum member
    [InlineData("Delay=notaspan")]
    [InlineData("Bogus=1")]         // no such property
    [InlineData("Hue=")]            // no value
    [InlineData("Hue")]             // no operator
    public void UnusableLeafIsNoMatchAndDoesNotThrow(string test)
    {
        var ex = Record.Exception(() => Assert.False(Check(test)));
        Assert.Null(ex);
    }

    // The old evaluator negated a failed comparison, so `~Hue=abc` matched every entity.
    [Fact]
    public void UnusableLeafStaysNoMatchUnderNegation()
    {
        Assert.False(Check("~Hue=abc"));
        Assert.False(Check("~Bogus=1"));
    }

    [Fact]
    public void NumericOperators()
    {
        Assert.True(Check("Hue=5"));
        Assert.True(Check("Hue==5"));
        Assert.True(Check("Hue=0x5"));
        Assert.False(Check("Hue!=5"));
        Assert.True(Check("Hue!4"));
        Assert.True(Check("Hue>4"));
        Assert.True(Check("Hue<6"));
        Assert.True(Check("Hue>=5"));
        Assert.True(Check("Hue<=5"));
        Assert.False(Check("Hue~5"));
    }

    [Fact]
    public void NegationPrefix()
    {
        Assert.False(Check("~Hue=5"));
        Assert.True(Check("~Hue=4"));
    }

    // '|' binds looser than '@'.
    [Theory]
    [InlineData("Movable=1", true)]
    [InlineData("Visible=1", false)]
    [InlineData("Visible=1@Visible=1|Movable=1", true)] // (F&&F)||T
    [InlineData("Movable=1|Visible=1@Visible=1", true)] // T||(F&&F)
    [InlineData("Movable=1@Visible=1", false)]
    [InlineData("Movable=1@Movable=1", true)]
    [InlineData("Visible=1|Visible=1", false)]
    public void Precedence(string test, bool expected)
    {
        Assert.Equal(expected, Check(test));
    }

    [Theory]
    [InlineData("Name=Moongate", true)]
    [InlineData("Name=moongate", false)]
    [InlineData("Name!Moongate", false)]
    [InlineData("Name>Moon", true)]
    [InlineData("Name<gate", true)]
    [InlineData("Name~ong", true)]
    [InlineData("Name~>moon", true)]
    [InlineData("Name~<GATE", true)]
    [InlineData("Name~~ONG", true)]
    [InlineData("Name~=MOONGATE", true)]
    [InlineData("Name~!MOONGATE", false)]
    [InlineData("Name>=Moon", false)] // no such string operator
    public void StringOperators(string test, bool expected)
    {
        Assert.Equal(expected, Check(test));
    }

    [Fact]
    public void StringNullIsTheNullStringForEqualityAndTextOtherwise()
    {
        var unnamed = new Subject { Name = null };

        Assert.True(Check("Name=null", unnamed));
        Assert.False(Check("Name=null"));
        Assert.False(Check("Name~null", unnamed));
        Assert.True(Check("Name~null", new Subject { Name = "nullable" }));
    }

    [Fact]
    public void BooleanAcceptsSwitchWordsAndOnlyEquality()
    {
        Assert.True(Check("Movable=true"));
        Assert.True(Check("Movable=1"));
        Assert.True(Check("Movable=on"));
        Assert.True(Check("Movable=Enabled"));
        Assert.True(Check("Visible=off"));
        Assert.False(Check("Movable=maybe"));
        Assert.False(Check("Movable>0"));
    }

    [Fact]
    public void EnumIgnoresCaseAndOrdersByValue()
    {
        Assert.True(Check("Layer=onehanded"));
        Assert.True(Check("Layer>Invalid"));
        Assert.False(Check("Layer=TwoHanded"));
    }

    // Floating point compares to a tolerance derived from the typed value.
    [Fact]
    public void FloatingPointUsesEpsilon()
    {
        Assert.True(Check("Weight=0.3"));
        Assert.False(Check("Weight=0.31"));
        Assert.True(Check("Weight>0.2"));
        Assert.True(Check("Weight<=0.3"));
        Assert.True(Check("Ratio=0.1"));
        Assert.False(Check("Ratio=0.2"));
    }

    [Fact]
    public void TimeSpanParsesAndCompares()
    {
        Assert.True(Check("Delay=00:05:00"));
        Assert.False(Check("Delay=00:10:00"));
        Assert.True(Check("Delay>00:01:00"));
    }

    [Fact]
    public void ValueTypeWithoutHotPathParsesThroughTypes()
    {
        Assert.True(Check("Id=00000000-0000-0000-0000-000000000001"));
        Assert.False(Check("Id=00000000-0000-0000-0000-000000000002"));
    }

    // A pre-IParsable type with only a static Parse(string) is still searchable, compared against
    // a real parsed instance rather than the raw text.
    [Fact]
    public void LegacyParseStringParsesThroughTypes()
    {
        Assert.True(Check("Legacy=alpha"));
        Assert.False(Check("Legacy=beta"));
    }

    // A reference type with no CompareTo answers equality only; ordering is no match, not a throw.
    [Fact]
    public void ReferenceTypeOrderingIsNoMatchAndDoesNotThrow()
    {
        var ex = Record.Exception(() => Assert.False(Check("Reference>whatever")));
        Assert.Null(ex);
    }

    [Fact]
    public void DottedNameWalksIntoTheProperty()
    {
        Assert.True(Check("Child.Value=7"));
        Assert.False(Check("Child.Value=8"));
        Assert.False(Check("Child.Value=7", new Subject { Child = null }));
        Assert.False(Check("~Child.Value=7", new Subject { Child = null }));
    }

    [Fact]
    public void NullableCompares()
    {
        Assert.True(Check("Maybe=null"));
        Assert.False(Check("Maybe=5"));
        Assert.True(Check("Maybe=5", new Subject { Maybe = 5 }));
        Assert.True(Check("Maybe>4", new Subject { Maybe = 5 }));
    }

    // Two runtime types resolving the same declared property share one compiled predicate.
    [Fact]
    public void SubclassesShareThePredicateCompiledForTheDeclaringType()
    {
        var cache = new AdvancedSearchConditions.Cache();

        var forBase = AdvancedSearchConditions.GetPredicate(cache, typeof(Subject), "Hue=5");
        var forDerived = AdvancedSearchConditions.GetPredicate(cache, typeof(Derived), "Hue=5");

        Assert.Same(forBase, forDerived);
        Assert.True(forDerived(new Derived()));
    }
}
