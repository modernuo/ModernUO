using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Commands.Generic;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands;

// `sort by` and `distinct` compile the same property chain the conditionals do, and had the same
// crash on a null link in the middle. Ordering cannot answer "no match" the way a condition can,
// so an unreadable binding reads as the default value instead -- which is what a null link means.
[Collection("Sequential UOContent Tests")]
public class ChainedBindingSortTests : IDisposable
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

    private static Property Bind(string binding)
    {
        var prop = new Property(binding);
        prop.BindTo(typeof(SkillTeleporter), PropertyAccess.Read);
        return prop;
    }

    private static IComparer<object> Sorter(string binding) =>
        SortCompiler.Compile<object>(
            typeof(SkillTeleporter),
            [new OrderInfo(Bind(binding), true)]
        );

    private static IComparer<object> Distincter(string binding) =>
        DistinctCompiler.Compile<object>(
            typeof(SkillTeleporter),
            [Bind(binding)]
        );

    [Fact]
    public void SortingOnAChainSurvivesANullIntermediate()
    {
        var named = Teleporter(TextDefinition.Of("Alpha"));
        var blank = Teleporter(null);

        var comparer = Sorter("Message.String");

        // A consistent total order is the contract; which end the blanks land on is not asserted.
        var forward = comparer.Compare(named, blank);

        Assert.NotEqual(0, forward);
        Assert.Equal(-Math.Sign(forward), Math.Sign(comparer.Compare(blank, named)));
        Assert.Equal(0, comparer.Compare(blank, blank));
        Assert.Equal(0, comparer.Compare(named, named));
    }

    [Fact]
    public void SortingOnAChainStillOrdersReadableValues()
    {
        var alpha = Teleporter(TextDefinition.Of("Alpha"));
        var beta = Teleporter(TextDefinition.Of("Beta"));

        var comparer = Sorter("Message.String");

        Assert.True(comparer.Compare(alpha, beta) < 0);
        Assert.True(comparer.Compare(beta, alpha) > 0);
    }

    [Fact]
    public void DistinctOnAChainSurvivesANullIntermediate()
    {
        var named = Teleporter(TextDefinition.Of("Alpha"));
        var blank = Teleporter(null);
        var alsoBlank = Teleporter(null);

        var comparer = Distincter("Message.String");

        Assert.NotEqual(0, comparer.Compare(named, blank));
        Assert.Equal(0, comparer.Compare(blank, alsoBlank));
    }

    // An unchained binding never had the problem and must keep working untouched.
    [Fact]
    public void SortingOnAPlainBindingIsUnchanged()
    {
        var low = Teleporter(null);
        low.Hue = 1;

        var high = Teleporter(null);
        high.Hue = 2;

        var comparer = Sorter("Hue");

        Assert.True(comparer.Compare(low, high) < 0);
        Assert.True(comparer.Compare(high, low) > 0);
        Assert.Equal(0, comparer.Compare(low, low));
    }
}
