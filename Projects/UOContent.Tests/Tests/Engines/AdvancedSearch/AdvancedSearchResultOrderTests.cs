using System;
using System.Collections.Generic;
using Server;
using Server.Engines.AdvancedSearch;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

// Every sort must give the same sequence from any arrival order: key first, then serial.
[Collection("Sequential UOContent Tests")]
public class AdvancedSearchResultOrderTests : IDisposable
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

    private AdvancedSearchResult Result(Item item, string name = "same", Map map = null)
    {
        _items.Add(item);
        return new AdvancedSearchResult(name, item.GetType(), item.Location, map ?? Map.Felucca, null) { Entity = item };
    }

    private AdvancedSearchResult[] EqualKeyed()
    {
        var results = new AdvancedSearchResult[6];

        for (var i = 0; i < results.Length; i++)
        {
            results[i] = Result(new Item(0x1));
        }

        return results;
    }

    private static AdvancedSearchResult[] Shuffled(AdvancedSearchResult[] inOrder, params int[] permutation)
    {
        var shuffled = new AdvancedSearchResult[inOrder.Length];

        for (var i = 0; i < permutation.Length; i++)
        {
            shuffled[i] = inOrder[permutation[i]];
        }

        return shuffled;
    }

    private static void AssertSameSequenceFromAnyArrival(
        AdvancedSearchResult[] inOrder,
        IComparer<AdvancedSearchResult> comparer
    )
    {
        var first = Shuffled(inOrder, 3, 0, 5, 1, 4, 2);
        var second = Shuffled(inOrder, 5, 4, 3, 2, 1, 0);

        Array.Sort(first, comparer);
        Array.Sort(second, comparer);

        Assert.Equal(first, second);
    }

    public static IEnumerable<object[]> Comparers()
    {
        yield return [AdvancedSearchResultSerialComparer.Instance];
        yield return [AdvancedSearchResultTypeComparer.Instance];
        yield return [AdvancedSearchResultTypeComparer.InstanceReverse];
        yield return [AdvancedSearchResultNameComparer.Instance];
        yield return [AdvancedSearchResultNameComparer.InstanceReverse];
        yield return [AdvancedSearchResultMapComparer.Instance];
        yield return [AdvancedSearchResultMapComparer.InstanceReverse];
        yield return [AdvancedSearchResultSelectedComparer.Instance];
        yield return [AdvancedSearchResultSelectedComparer.InstanceReverse];
    }

    [Theory]
    [MemberData(nameof(Comparers))]
    public void EqualKeysOrderBySerialFromAnyArrivalOrder(IComparer<AdvancedSearchResult> comparer)
    {
        var inOrder = EqualKeyed();

        AssertSameSequenceFromAnyArrival(inOrder, comparer);

        var sorted = Shuffled(inOrder, 3, 0, 5, 1, 4, 2);
        Array.Sort(sorted, comparer);

        Assert.Equal(inOrder, sorted);
    }

    // Off-map results compare equal on range.
    [Fact]
    public void RangeComparerOrdersOffMapResultsBySerial()
    {
        var from = new Mobile();
        from.MoveToWorld(new Point3D(1000, 1000, 0), Map.Trammel);

        try
        {
            var inOrder = EqualKeyed();

            AssertSameSequenceFromAnyArrival(inOrder, new AdvancedSearchRangeComparer(from));
            AssertSameSequenceFromAnyArrival(inOrder, new AdvancedSearchRangeComparer(from, true));
        }
        finally
        {
            from.Delete();
        }
    }

    [Fact]
    public void ReverseFlipsTheKeyNotTheTieBreak()
    {
        var a1 = Result(new Item(0x1), "alpha");
        var a2 = Result(new Item(0x1), "alpha");
        var b1 = Result(new Item(0x1), "beta");

        var results = new[] { b1, a2, a1 };

        Array.Sort(results, AdvancedSearchResultNameComparer.Instance);
        Assert.Equal([a1, a2, b1], results);

        Array.Sort(results, AdvancedSearchResultNameComparer.InstanceReverse);
        Assert.Equal([b1, a1, a2], results);
    }
}
