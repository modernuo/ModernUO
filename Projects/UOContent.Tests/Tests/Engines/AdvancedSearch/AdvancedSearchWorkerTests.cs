using System;
using System.Collections.Concurrent;
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
