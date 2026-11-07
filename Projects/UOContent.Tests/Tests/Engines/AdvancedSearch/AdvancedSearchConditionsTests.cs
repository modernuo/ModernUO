using System;
using Server;
using Server.Engines.AdvancedSearch;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

// The Advanced Search property test compiles through the same conditions a `where` clause does.
// The grammar is the gump's own; these pin its operators and precedence, and that a leaf which
// cannot be resolved or parsed is "no match" rather than an exception on the search worker.
public class AdvancedSearchConditionsTests
{
    public sealed class Inner
    {
        [CommandProperty(AccessLevel.GameMaster)] public int Value { get; set; } = 7;
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
        [CommandProperty(AccessLevel.GameMaster)] public int Hue { get; set; } = 5;
        [CommandProperty(AccessLevel.GameMaster)] public string Name { get; set; } = "Moongate";
        [CommandProperty(AccessLevel.GameMaster)] public bool Movable { get; set; } = true;
        [CommandProperty(AccessLevel.GameMaster)] public bool Visible { get; set; }
        [CommandProperty(AccessLevel.GameMaster)] public double Weight { get; set; } = 0.1 + 0.2;
        [CommandProperty(AccessLevel.GameMaster)] public float Ratio { get; set; } = 0.1f;
        [CommandProperty(AccessLevel.GameMaster)] public TimeSpan Delay { get; set; } = TimeSpan.FromMinutes(5);
        [CommandProperty(AccessLevel.GameMaster)] public Guid Id { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
        [CommandProperty(AccessLevel.GameMaster)] public Layer Layer { get; set; } = Layer.OneHanded;
        [CommandProperty(AccessLevel.GameMaster)] public object Reference { get; set; } = new();
        [CommandProperty(AccessLevel.GameMaster)] public LegacyParseType Legacy { get; set; } = LegacyParseType.Parse("alpha");
        [CommandProperty(AccessLevel.GameMaster)] public Inner Child { get; set; } = new();
        [CommandProperty(AccessLevel.GameMaster)] public int? Maybe { get; set; }
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
        Assert.False(Check(test));
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
        Assert.False(Check("Reference>whatever"));
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
        var cache = new AdvancedSearchConditions.Cache(AccessLevel.GameMaster);

        var forBase = AdvancedSearchConditions.GetPredicate(cache, typeof(Subject), "Hue=5").Worker;
        var forDerived = AdvancedSearchConditions.GetPredicate(cache, typeof(Derived), "Hue=5").Worker;

        Assert.Same(forBase, forDerived);
        Assert.True(forDerived(new Derived()));
    }

    public class ImpureSubject : Subject
    {
        private object _x;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Lazy
        {
            get
            {
                _x ??= new object();
                return 1;
            }
        }
    }

    public class PlainSubject
    {
        public int Plain { get; set; } = 1;

        [CommandProperty(AccessLevel.Administrator)]
        public int Secret { get; set; } = 1;

        [CommandProperty(AccessLevel.GameMaster)]
        public RestrictedInner Link { get; set; } = new();
    }

    public sealed class RestrictedInner
    {
        [CommandProperty(AccessLevel.Administrator)]
        public int Value { get; set; } = 7;
    }

    public class PureBase
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public string Name => Describe();

        public virtual string Describe() => "x";
    }

    public sealed class PureDerived : PureBase
    {
    }

    public sealed class ImpureDerived : PureBase
    {
        private object _cache;

        public override string Describe()
        {
            _cache ??= new object();
            return "x";
        }
    }

    [Fact]
    public void PureTest_HasNoLoopPredicate()
    {
        var predicate = AdvancedSearchConditions.GetPredicate(new AdvancedSearchConditions.Cache(AccessLevel.GameMaster), typeof(Subject), "Hue=5");

        Assert.False(predicate.NeedsLoop);
        Assert.True(predicate.Worker(new Subject()));
    }

    [Fact]
    public void ImpureLeaf_DefersToLoop()
    {
        var predicate = AdvancedSearchConditions.GetPredicate(new AdvancedSearchConditions.Cache(AccessLevel.GameMaster), typeof(ImpureSubject), "Hue=5@Lazy=1");

        Assert.True(predicate.NeedsLoop);
        Assert.True(predicate.Worker(new ImpureSubject()));
        Assert.False(predicate.Worker(new ImpureSubject { Hue = 6 }));
        Assert.True(predicate.Loop(new ImpureSubject()));
    }

    [Fact]
    public void OrWithImpureGroup_WorkerDoesNotReject()
    {
        var predicate = AdvancedSearchConditions.GetPredicate(new AdvancedSearchConditions.Cache(AccessLevel.GameMaster), typeof(ImpureSubject), "Hue=6|Lazy=1");

        Assert.True(predicate.Worker(new ImpureSubject()));
        Assert.True(predicate.Loop(new ImpureSubject()));
    }

    [Fact]
    public void ComplexValueType_DefersToLoop()
    {
        var predicate = AdvancedSearchConditions.GetPredicate(new AdvancedSearchConditions.Cache(AccessLevel.GameMaster), typeof(Subject), "Legacy=alpha");

        Assert.True(predicate.NeedsLoop);
    }

    [Fact]
    public void PropertyWithoutCommandProperty_IsNoMatch()
    {
        Assert.False(AdvancedSearchConditions.Compile(typeof(PlainSubject), "Plain=1")(new PlainSubject()));
    }

    [Fact]
    public void PropertyAboveSearcherAccess_IsNoMatch()
    {
        Assert.False(AdvancedSearchConditions.Compile(typeof(PlainSubject), "Secret=1", AccessLevel.GameMaster)(new PlainSubject()));
        Assert.True(AdvancedSearchConditions.Compile(typeof(PlainSubject), "Secret=1", AccessLevel.Administrator)(new PlainSubject()));
    }

    [Fact]
    public void DottedChain_EachLinkNeedsAccess()
    {
        Assert.False(AdvancedSearchConditions.Compile(typeof(PlainSubject), "Link.Value=7", AccessLevel.GameMaster)(new PlainSubject()));
        Assert.True(AdvancedSearchConditions.Compile(typeof(PlainSubject), "Link.Value=7", AccessLevel.Administrator)(new PlainSubject()));
    }

    [Fact]
    public void SharedCompiledType_DoesNotLeakPurityAcrossRuntimeTypes()
    {
        var cache = new AdvancedSearchConditions.Cache(AccessLevel.GameMaster);

        Assert.False(AdvancedSearchConditions.GetPredicate(cache, typeof(PureDerived), "Name=x").NeedsLoop);
        Assert.True(AdvancedSearchConditions.GetPredicate(cache, typeof(ImpureDerived), "Name=x").NeedsLoop);
    }
}

[Collection("Sequential UOContent Tests")]
public class AdvancedSearchEntityLeafTests
{
    private sealed class LinkedItem : Item
    {
        public LinkedItem() : base(0x1)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Linked { get; set; }
    }

    [Fact]
    public void EntityLeaf_ComparesBySerial()
    {
        var target = new Item(0x1);
        var linked = new LinkedItem { Linked = target };
        var unlinked = new LinkedItem();

        try
        {
            var predicate = AdvancedSearchConditions.GetPredicate(
                new AdvancedSearchConditions.Cache(AccessLevel.GameMaster),
                typeof(LinkedItem),
                $"Linked=0x{target.Serial.Value:X}"
            );

            Assert.False(predicate.NeedsLoop);
            Assert.True(predicate.Worker(linked));
            Assert.False(predicate.Worker(unlinked));

            var missing = AdvancedSearchConditions.GetPredicate(
                new AdvancedSearchConditions.Cache(AccessLevel.GameMaster),
                typeof(LinkedItem),
                "Linked=0x7FFFFFF0"
            );

            Assert.False(missing.NeedsLoop);
            Assert.False(missing.Worker(linked));
            Assert.False(missing.Worker(unlinked));
        }
        finally
        {
            target.Delete();
            linked.Delete();
            unlinked.Delete();
        }
    }
}
