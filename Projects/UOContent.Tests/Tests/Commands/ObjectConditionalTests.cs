using System;
using System.Collections.Generic;
using Server;
using Server.Commands.Generic;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands;

// `where` compiles conditions to IL. Two shapes were broken there: a property type with value
// equality but no IComparable fell through to a raw Ceq (reference equality, so never true), and
// a chained binding dereferenced every link with no null guard (so the first object with a null
// intermediate took the whole sweep down with an NRE).
[Collection("Sequential UOContent Tests")]
public class ObjectConditionalTests : IDisposable
{
    private readonly List<Item> _items = [];
    private readonly Mobile _from = new() { AccessLevel = AccessLevel.Developer };

    public void Dispose()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Delete();
        }

        _items.Clear();
        _from.Delete();
    }

    private SkillTeleporter Teleporter(TextDefinition message)
    {
        var tp = new SkillTeleporter { Message = message };
        _items.Add(tp);
        return tp;
    }

    private bool Check(object target, params string[] condition)
    {
        var args = new string[condition.Length + 1];
        args[0] = nameof(SkillTeleporter);
        Array.Copy(condition, 0, args, 1, condition.Length);

        return ObjectConditional.ParseDirect(_from, args, 0, args.Length).CheckCondition(target);
    }

    [Fact]
    public void EqualityUsesValueSemanticsNotReferenceIdentity()
    {
        var tp = Teleporter(TextDefinition.Of(1060847));

        Assert.True(Check(tp, "Message", "=", "1060847"));
    }

    [Fact]
    public void InequalityUsesValueSemanticsNotReferenceIdentity()
    {
        var tp = Teleporter(TextDefinition.Of(1060847));

        Assert.False(Check(tp, "Message", "!=", "1060847"));
        Assert.True(Check(tp, "Message", "!=", "1234567"));
    }

    [Fact]
    public void StringValuedDefinitionsCompareByValue()
    {
        var tp = Teleporter(TextDefinition.Of("Hail, traveller."));

        Assert.True(Check(tp, "Message", "=", "Hail, traveller."));
        Assert.False(Check(tp, "Message", "=", "Farewell."));
    }

    [Fact]
    public void NullComparisonStillWorks()
    {
        Assert.True(Check(Teleporter(null), "Message", "=", "null"));
        Assert.False(Check(Teleporter(TextDefinition.Of(1060847)), "Message", "=", "null"));
    }

    // The sweep case: [global where SkillTeleporter Message.Number = X hits teleporters whose
    // Message was never set long before it hits one that matches.
    [Fact]
    public void ChainedBindingOnANullIntermediateIsFalseNotAnException()
    {
        var blank = Teleporter(null);

        Assert.False(Check(blank, "Message.Number", "=", "1060847"));
    }

    [Fact]
    public void ChainedBindingStillMatchesWhenIntermediateIsPresent()
    {
        var tp = Teleporter(TextDefinition.Of(1060847));

        Assert.True(Check(tp, "Message.Number", "=", "1060847"));
        Assert.False(Check(tp, "Message.Number", "=", "1234567"));
    }

    [Fact]
    public void ChainedBindingOnANullIntermediateIsFalseUnderNegationToo()
    {
        var blank = Teleporter(null);

        Assert.False(Check(blank, "not", "Message.Number", "=", "1060847"));
    }

    // The string operators (contains / starts / ends / =~) compile through StringCondition, which
    // chains the same way ComparisonCondition does and so had the same null-intermediate crash.
    [Theory]
    [InlineData("contains")]
    [InlineData("contains~")]
    [InlineData("starts")]
    [InlineData("ends")]
    [InlineData("=~")]
    [InlineData("!=~")]
    public void StringOperatorsOnANullIntermediateAreFalseNotAnException(string oper)
    {
        var blank = Teleporter(null);

        Assert.False(Check(blank, "Message.String", oper, "gate"));
    }

    [Fact]
    public void StringOperatorsOnANullIntermediateAreFalseUnderNegationToo()
    {
        var blank = Teleporter(null);

        Assert.False(Check(blank, "not", "Message.String", "contains", "gate"));
    }

    [Fact]
    public void StringOperatorsStillMatchThroughAChain()
    {
        var tp = Teleporter(TextDefinition.Of("Moongate"));

        Assert.True(Check(tp, "Message.String", "contains", "gate"));
        Assert.True(Check(tp, "Message.String", "starts", "Moon"));
        Assert.True(Check(tp, "Message.String", "ends", "gate"));
        Assert.True(Check(tp, "Message.String", "=~", "moongate"));
        Assert.False(Check(tp, "Message.String", "contains", "portal"));
    }

    // The chain resolves here -- Message is set -- but its String is null because the definition
    // holds a cliloc. That is the final value, which StringCondition already guarded.
    [Fact]
    public void StringOperatorsHandleANullFinalValue()
    {
        var tp = Teleporter(TextDefinition.Of(1060847));

        Assert.False(Check(tp, "Message.String", "contains", "gate"));
        Assert.False(Check(tp, "Message.String", "=~", "gate"));
    }

    [Fact]
    public void UnchainedStringOperatorsStillWork()
    {
        var tp = Teleporter(null);
        tp.Name = "Moongate";

        Assert.True(Check(tp, "Name", "contains", "gate"));
        Assert.True(Check(tp, "Name", "=~", "moongate"));
        Assert.False(Check(tp, "Name", "contains", "portal"));
    }

    // Guards for the comparison paths the equality change must not disturb. Ints, strings and
    // enums are IComparable, so they route through CompareTo and never reach CompareEquality --
    // these prove that routing is intact.
    [Fact]
    public void NumericComparisonsStillWork()
    {
        var tp = Teleporter(null);
        tp.Hue = 42;

        Assert.True(Check(tp, "Hue", "=", "42"));
        Assert.False(Check(tp, "Hue", "=", "43"));
        Assert.True(Check(tp, "Hue", ">", "41"));
        Assert.True(Check(tp, "Hue", "<", "43"));
        Assert.True(Check(tp, "Hue", "!=", "43"));
    }

    [Fact]
    public void StringComparisonsStillWork()
    {
        var tp = Teleporter(null);
        tp.Name = "gate";

        Assert.True(Check(tp, "Name", "=", "gate"));
        Assert.False(Check(tp, "Name", "=", "portal"));
    }

    [Fact]
    public void EnumComparisonsStillWork()
    {
        var tp = Teleporter(null);
        tp.Skill = SkillName.Magery;

        Assert.True(Check(tp, "Skill", "=", "Magery"));
        Assert.False(Check(tp, "Skill", "=", "Anatomy"));
    }
}
