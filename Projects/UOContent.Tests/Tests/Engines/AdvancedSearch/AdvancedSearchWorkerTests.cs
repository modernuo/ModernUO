using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Server;
using Server.Engines.AdvancedSearch;
using Server.Items;
using Server.Saves;
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
        [CommandProperty(AccessLevel.GameMaster)] public string Boom => throw new InvalidOperationException("boom");
    }

    // A getter that writes on first read, so the classifier rejects it and the test needs the loop.
    private sealed class LazyItem : Item
    {
        private object _cache;

        public LazyItem() : base(0x1) { }
        public LazyItem(Serial s) : base(s) { }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Lazy
        {
            get
            {
                _cache ??= new object();
                return 1;
            }
        }
    }

    private static void RunWorker(
        AdvancedSearchFilter filter,
        ConcurrentQueue<AdvancedSearchResult> results,
        ConcurrentQueue<IEntity> deferred,
        ConcurrentQueue<IEntity> ignoreSources,
        params IEntity[] entities
    )
    {
        var worker = new AdvancedSearchThreadWorker();

        try
        {
            worker.Wake(
                new WorldLocation(Point3D.Zero, Map.Felucca),
                filter,
                results,
                deferred,
                ignoreSources,
                new AdvancedSearchConditions.Cache(AccessLevel.GameMaster)
            );

            for (var i = 0; i < entities.Length; i++)
            {
                worker.Push(entities[i]);
            }

            worker.Sleep();
        }
        finally
        {
            worker.Exit();
        }
    }

    private static HashSet<IEntity> EntitiesOf(ConcurrentQueue<AdvancedSearchResult> results)
    {
        var set = new HashSet<IEntity>();
        foreach (var r in results)
        {
            set.Add(r.Entity);
        }

        return set;
    }

    [Fact]
    public void Worker_RegionFilter_DefersInsteadOfFindingRegion()
    {
        var item = new Item(0x1);
        item.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);

        var results = new ConcurrentQueue<AdvancedSearchResult>();
        var deferred = new ConcurrentQueue<IEntity>();
        var filter = new AdvancedSearchFilter { FilterRegion = true, RegionName = "Britain" };

        try
        {
            RunWorker(filter, results, deferred, new ConcurrentQueue<IEntity>(), item);

            Assert.Empty(results);
            Assert.Equal([item], deferred.ToArray());
        }
        finally
        {
            item.Delete();
        }
    }

    [Fact]
    public void Worker_ImpurePropertyTest_Defers()
    {
        var item = new LazyItem();

        var results = new ConcurrentQueue<AdvancedSearchResult>();
        var deferred = new ConcurrentQueue<IEntity>();
        var filter = new AdvancedSearchFilter { FilterPropertyTest = true, PropertyTest = "Lazy=1" };

        try
        {
            RunWorker(filter, results, deferred, new ConcurrentQueue<IEntity>(), item);

            Assert.Empty(results);
            Assert.Equal([item], deferred.ToArray());
        }
        finally
        {
            item.Delete();
        }
    }

    [Fact]
    public void Worker_PurePropertyTest_MatchesDirectly()
    {
        var item = new Item(0x1) { Hue = 5 };

        var results = new ConcurrentQueue<AdvancedSearchResult>();
        var deferred = new ConcurrentQueue<IEntity>();
        var filter = new AdvancedSearchFilter { FilterPropertyTest = true, PropertyTest = "Hue=5" };

        try
        {
            RunWorker(filter, results, deferred, new ConcurrentQueue<IEntity>(), item);

            Assert.Empty(deferred);
            Assert.Equal([item], EntitiesOf(results));
        }
        finally
        {
            item.Delete();
        }
    }

    // A KeyRing stands in for any ignore source: what it hides is collected on the loop, but the source
    // itself is still a search candidate.
    [Fact]
    public void Worker_IgnoreSourceIsQueuedAndStillEvaluated()
    {
        var keyRing = new KeyRing();
        keyRing.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);

        var results = new ConcurrentQueue<AdvancedSearchResult>();
        var deferred = new ConcurrentQueue<IEntity>();
        var ignoreSources = new ConcurrentQueue<IEntity>();
        var filter = new AdvancedSearchFilter { HideValidInternalMap = true };

        try
        {
            RunWorker(filter, results, deferred, ignoreSources, keyRing);

            Assert.Equal([keyRing], ignoreSources.ToArray());
            Assert.True(EntitiesOf(results).Contains(keyRing) || Array.IndexOf(deferred.ToArray(), keyRing) >= 0);
        }
        finally
        {
            keyRing.Delete();
        }
    }

    // Ignore sources come only from entities that pass the type filter, so a KeyRing outside the searched
    // type hides nothing. The name-only search is the control: there the KeyRing is searched and hides its key.
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void DoSearch_IgnoreSourcesComeOnlyFromTypeFilteredEntities(bool filterByKeyType, bool keyExpected)
    {
        const string keyName = "advanced-search-type-filter-key";

        var from = new Mobile { AccessLevel = AccessLevel.GameMaster };
        var keyRing = new KeyRing();
        keyRing.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        var pack = new Backpack();
        pack.MoveToWorld(new Point3D(1001, 1000, 0), Map.Felucca);
        var key = new Key { Name = keyName };
        keyRing.Add(key);
        pack.DropItem(key); // in a container on Felucca, so only the KeyRing can hide it

        var gump = new AdvancedSearchGump
        {
            Filter = new AdvancedSearchFilter
            {
                FilterType = filterByKeyType,
                Type = filterByKeyType ? typeof(Key) : null,
                FilterName = true,
                Name = keyName,
                HideValidInternalMap = true
            }
        };

        var autoSave = AutoSave.SavesEnabled;
        var originalContext = Core.LoopContext;

        try
        {
            // Disabled outside the scoped wheel, so the search's own restore never starts the autosave timer on it.
            AutoSave.SavesEnabled = false;

            using (new TimerWheelScope())
            {
                // The context pins itself to the thread that built it; own one for the test's thread.
                var owned = new EventLoopContext();
                Core.LoopContext = owned;

                gump.DoSearch(from);

                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (AdvancedSearchGump.IsSearchInProgress && DateTime.UtcNow < deadline)
                {
                    owned.ExecuteTasks();
                    TimerWheelScope.RunFor(TimerWheelScope.Step);
                    Thread.Sleep(1);
                }

                Assert.False(AdvancedSearchGump.IsSearchInProgress, "search never finished");
                Assert.False(AutoSave.SavesEnabled);
            }

            Assert.NotNull(gump.SearchResults);
            Assert.Equal(keyExpected, Array.Exists(gump.SearchResults, r => r.Entity == key));
        }
        finally
        {
            Core.LoopContext = originalContext;
            AutoSave.SavesEnabled = autoSave;
            AdvancedSearchGump.EndSearch();
            key.Delete();
            pack.Delete();
            keyRing.Delete();
            from.Delete();
        }
    }

    [Fact]
    public void Worker_FilterThrows_DoesNotEscape_ReturnsNoMatch()
    {
        var worker = new AdvancedSearchThreadWorker();
        var results = new ConcurrentQueue<AdvancedSearchResult>();
        var deferred = new ConcurrentQueue<IEntity>();
        var ignore = new ConcurrentQueue<IEntity>();
        var filter = new AdvancedSearchFilter
        {
            FilterPropertyTest = true,
            PropertyTest = "Boom=1", // reflection GetValue -> throws
        };

        var item = new ThrowingItem();

        try
        {
            worker.Wake(new WorldLocation(Point3D.Zero, Map.Felucca), filter, results, deferred, ignore, new AdvancedSearchConditions.Cache(AccessLevel.GameMaster));
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
        var deferred = new ConcurrentQueue<IEntity>();
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
            worker.Wake(new WorldLocation(Point3D.Zero, Map.Felucca), filter, results, deferred, ignore, new AdvancedSearchConditions.Cache(AccessLevel.GameMaster));
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
        var deferred = new ConcurrentQueue<IEntity>();
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
            worker.Wake(new WorldLocation(Point3D.Zero, Map.Felucca), filter, results, deferred, ignore, new AdvancedSearchConditions.Cache(AccessLevel.GameMaster));
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
        var deferred = new ConcurrentQueue<IEntity>();
        var ignore = new ConcurrentQueue<IEntity>();
        var filter = new AdvancedSearchFilter(); // no filters -> everything matches

        var item = new Item(0x1);
        item.Delete();

        try
        {
            worker.Wake(new WorldLocation(Point3D.Zero, Map.Felucca), filter, results, deferred, ignore, new AdvancedSearchConditions.Cache(AccessLevel.GameMaster));
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
