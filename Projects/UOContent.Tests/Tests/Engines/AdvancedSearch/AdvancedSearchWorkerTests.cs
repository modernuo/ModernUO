using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Server;
using Server.Engines.AdvancedSearch;
using Server.Items;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class AdvancedSearchWorkerTests
{
    // An item whose property test path will throw when evaluated.
    private sealed class ThrowingItem : Item
    {
        public ThrowingItem() : base(0x1) { }
        public ThrowingItem(Serial s) : base(s) { }
        public string Boom => throw new InvalidOperationException("boom");
    }

    [Fact]
    public void Worker_FilterThrows_DoesNotEscape_ReturnsNoMatch()
    {
        var worker = new AdvancedSearchThreadWorker();
        var results = new ConcurrentQueue<AdvancedSearchResult>();
        var ignore = new ConcurrentQueue<IEntity>();
        var filter = new AdvancedSearchFilter
        {
            FilterPropertyTest = true,
            PropertyTest = "Boom=1", // reflection GetValue -> throws
        };

        var item = new ThrowingItem();

        try
        {
            worker.Wake(new WorldLocation(Point3D.Zero, Map.Felucca), filter, results, ignore);
            worker.Push(item);
            worker.Sleep(); // drains; must not crash the test process

            Assert.Empty(results);
        }
        finally
        {
            item.Delete();
            worker.Exit();
        }
    }

    // End to end through Wake/Push/Sleep on real entities: the property test is compiled on the
    // worker, memoized per type, and applied to items and mobiles alike.
    [Fact]
    public void Worker_PropertyTest_FiltersItemsAndMobiles()
    {
        var worker = new AdvancedSearchThreadWorker();
        var results = new ConcurrentQueue<AdvancedSearchResult>();
        var ignore = new ConcurrentQueue<IEntity>();
        var filter = new AdvancedSearchFilter
        {
            FilterPropertyTest = true,
            PropertyTest = "Hue > 0",
        };

        var plain = new Item(0x1);
        var hued = new Item(0x1) { Hue = 42 };
        var huedToo = new Gold(1) { Hue = 7 };
        var mobile = new Mobile { Hue = 1002 };

        try
        {
            worker.Wake(new WorldLocation(Point3D.Zero, Map.Felucca), filter, results, ignore);
            worker.Push(plain);
            worker.Push(hued);
            worker.Push(huedToo);
            worker.Push(mobile);
            worker.Sleep();

            var matched = new HashSet<IEntity>();
            foreach (var r in results)
            {
                matched.Add(r.Entity);
            }

            Assert.Equal(3, matched.Count);
            Assert.Contains(hued, matched);
            Assert.Contains(huedToo, matched);
            Assert.Contains(mobile, matched);
            Assert.DoesNotContain(plain, matched);
        }
        finally
        {
            plain.Delete();
            hued.Delete();
            huedToo.Delete();
            mobile.Delete();
            worker.Exit();
        }
    }

    // The map boxes are independent checks. Ticking several used to reject everything, because
    // each ticked map was applied as "must be on this map"; an entity on any ticked map passes.
    [Fact]
    public void Worker_SeveralMapsTicked_MatchesAnyOfThem()
    {
        var worker = new AdvancedSearchThreadWorker();
        var results = new ConcurrentQueue<AdvancedSearchResult>();
        var ignore = new ConcurrentQueue<IEntity>();
        var filter = new AdvancedSearchFilter
        {
            FilterFelucca = true,
            FilterTrammel = true,
            FilterInternalMap = true,
            HideValidInternalMap = false,
        };

        var fel = new Item(0x1);
        fel.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        var tram = new Item(0x1);
        tram.MoveToWorld(new Point3D(1000, 1000, 0), Map.Trammel);
        var internalItem = new Item(0x1); // starts on Map.Internal
        var malas = new Item(0x1);
        malas.MoveToWorld(new Point3D(1000, 1000, 0), Map.Malas);

        try
        {
            worker.Wake(new WorldLocation(Point3D.Zero, Map.Felucca), filter, results, ignore);
            worker.Push(fel);
            worker.Push(tram);
            worker.Push(internalItem);
            worker.Push(malas);
            worker.Sleep();

            var matched = new HashSet<IEntity>();
            foreach (var r in results)
            {
                matched.Add(r.Entity);
            }

            Assert.Contains(fel, matched);
            Assert.Contains(tram, matched);
            Assert.Contains(internalItem, matched);
            Assert.DoesNotContain(malas, matched);
        }
        finally
        {
            fel.Delete();
            tram.Delete();
            internalItem.Delete();
            malas.Delete();
            worker.Exit();
        }
    }

    [Fact]
    public void Worker_DeletedEntity_IsSkipped()
    {
        var worker = new AdvancedSearchThreadWorker();
        var results = new ConcurrentQueue<AdvancedSearchResult>();
        var ignore = new ConcurrentQueue<IEntity>();
        var filter = new AdvancedSearchFilter(); // no filters -> everything matches

        var item = new Item(0x1);
        item.Delete();

        try
        {
            worker.Wake(new WorldLocation(Point3D.Zero, Map.Felucca), filter, results, ignore);
            worker.Push(item);
            worker.Sleep();

            Assert.Empty(results);
        }
        finally
        {
            worker.Exit();
        }
    }

    [Fact]
    public void DoSearch_IsGuarded_AgainstReentry()
    {
        // White-box: flip the guard, assert a second entry is rejected, then clear.
        // _searchInProgress is process-global static state; release it in finally so a
        // failed assert here can't leak the guard into other tests.
        Assert.False(AdvancedSearchGump.IsSearchInProgress);
        Assert.True(AdvancedSearchGump.TryBeginSearch()); // acquires
        try
        {
            Assert.False(AdvancedSearchGump.TryBeginSearch()); // rejected
        }
        finally
        {
            AdvancedSearchGump.EndSearch(); // releases
        }

        Assert.False(AdvancedSearchGump.IsSearchInProgress);
    }
}
