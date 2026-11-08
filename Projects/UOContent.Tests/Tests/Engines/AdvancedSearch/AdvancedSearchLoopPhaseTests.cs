using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Server;
using Server.Engines.AdvancedSearch;
using Server.Items;
using Server.Saves;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class AdvancedSearchLoopPhaseTests
{
    private static readonly WorldLocation Origin = new(Point3D.Zero, Map.Felucca);

    private static AdvancedSearchConditions.Cache NewCache() => new(AccessLevel.GameMaster);

    private sealed class NotWorkerSafeItem : Item
    {
        public NotWorkerSafeItem() : base(0x1) { }
        public NotWorkerSafeItem(Serial s) : base(s) { }
        public override string DefaultName => new string('n', 1);
    }

    private static AdvancedSearchResult ResultFor(Item item) =>
        new(item.Name ?? item.ItemData.Name, item.GetType(), item.Location, item.Map, item.RootParent)
        {
            Entity = item
        };

    private static AdvancedSearchLoopPhase NewPhase(
        Mobile from,
        AdvancedSearchGump gump,
        AdvancedSearchFilter filter,
        AdvancedSearchResult[] workerResults,
        ConcurrentQueue<IEntity> deferred,
        ConcurrentQueue<IEntity> ignoreSources
    )
    {
        // The caller sorts the worker results off the loop.
        Array.Sort(workerResults, AdvancedSearchResultSerialComparer.Instance);
        return new AdvancedSearchLoopPhase(from, gump, filter, Origin, NewCache(), workerResults, deferred, ignoreSources, false);
    }

    private static void RunToCompletion(AdvancedSearchLoopPhase phase) => RunSlices(phase, long.MaxValue);

    // The test thread acts as the game loop: runs slices and the sort's posted completion. Returns slices taken.
    private static int RunSlices(AdvancedSearchLoopPhase phase, long budget)
    {
        var original = Core.LoopContext;

        // The context pins itself to the thread that built it; own one for the test's thread.
        var loop = new EventLoopContext();
        Core.LoopContext = loop;

        try
        {
            var slices = 1;
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!phase.RunSlice(budget))
            {
                Assert.True(DateTime.UtcNow < deadline, "loop phase never finished");
                loop.ExecuteTasks();
                slices++;
            }

            return slices;
        }
        finally
        {
            Core.LoopContext = original;
        }
    }

    private static void DeleteAll(List<IEntity> entities)
    {
        for (var i = 0; i < entities.Count; i++)
        {
            entities[i].Delete();
        }
    }

    [Fact]
    public void Matcher_OnLoop_AppliesRegionFilter()
    {
        var item = new Item(0x1);
        item.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);

        var filter = new AdvancedSearchFilter { FilterRegion = true, RegionName = "Nowhere" };

        try
        {
            Assert.Equal(
                MatchOutcome.Reject,
                AdvancedSearchMatcher.Evaluate(item, filter, Origin, NewCache(), true, out var onLoopResult)
            );
            Assert.Null(onLoopResult);

            Assert.Equal(
                MatchOutcome.Defer,
                AdvancedSearchMatcher.Evaluate(item, filter, Origin, NewCache(), false, out var workerResult)
            );
            Assert.Null(workerResult);
        }
        finally
        {
            item.Delete();
        }
    }

    [Fact]
    public void Matcher_Worker_CompilesPredicateBeforeDeferringUnsafeType()
    {
        var item = new NotWorkerSafeItem();
        var cache = NewCache();
        var filter = new AdvancedSearchFilter { FilterPropertyTest = true, PropertyTest = "Hue=5" };

        try
        {
            Assert.False(AdvancedSearchWorkerReads.IsWorkerSafe(typeof(NotWorkerSafeItem)));
            Assert.Equal(MatchOutcome.Defer, AdvancedSearchMatcher.Evaluate(item, filter, Origin, cache, false, out _));

            // The loop's drain then finds the predicate already compiled.
            Assert.True(cache.ByRuntimeType.ContainsKey(typeof(NotWorkerSafeItem)));
        }
        finally
        {
            item.Delete();
        }
    }

    [Fact]
    public void LoopPhase_FinishesDeferredAndPublishes()
    {
        var from = new Mobile();
        var gump = new AdvancedSearchGump();
        var a = new Item(0x1) { Hue = 5 };
        var b = new Item(0x1) { Hue = 5 };
        var entities = new List<IEntity> { from, a, b };

        var filter = new AdvancedSearchFilter { FilterPropertyTest = true, PropertyTest = "Hue=5" };
        ConcurrentQueue<IEntity> deferred = [];
        deferred.Enqueue(b);
        deferred.Enqueue(a);

        try
        {
            var phase = NewPhase(from, gump, filter, [], deferred, []);

            RunToCompletion(phase);

            Assert.NotNull(gump.SearchResults);
            Assert.Equal(2, gump.SearchResults.Length);
            Assert.Same(a, gump.SearchResults[0].Entity);
            Assert.Same(b, gump.SearchResults[1].Entity);
        }
        finally
        {
            DeleteAll(entities);
            AdvancedSearchGump.EndSearch();
        }
    }

    [Fact]
    public void LoopPhase_EntityDeletedBeforeSlice_IsExcluded()
    {
        var from = new Mobile();
        var gump = new AdvancedSearchGump();
        var kept = new Item(0x1);
        var deletedResult = new Item(0x1);
        var deletedDeferred = new Item(0x1);
        var entities = new List<IEntity> { from, kept };

        AdvancedSearchResult[] results = [ResultFor(kept), ResultFor(deletedResult)];
        ConcurrentQueue<IEntity> deferred = [];
        deferred.Enqueue(deletedDeferred);

        deletedResult.Delete();
        deletedDeferred.Delete();

        try
        {
            var phase = NewPhase(from, gump, new AdvancedSearchFilter(), results, deferred, []);

            RunToCompletion(phase);

            Assert.NotNull(gump.SearchResults);
            Assert.Single(gump.SearchResults);
            Assert.Same(kept, gump.SearchResults[0].Entity);
        }
        finally
        {
            DeleteAll(entities);
            AdvancedSearchGump.EndSearch();
        }
    }

    [Fact]
    public void LoopPhase_SmallBudget_TakesSeveralSlices()
    {
        var from = new Mobile();
        var gump = new AdvancedSearchGump();
        var entities = new List<IEntity> { from };
        ConcurrentQueue<IEntity> deferred = [];

        for (var i = 0; i < 500; i++)
        {
            var item = new Item(0x1);
            entities.Add(item);
            deferred.Enqueue(item);
        }

        try
        {
            var phase = NewPhase(from, gump, new AdvancedSearchFilter(), [], deferred, []);

            Assert.True(RunSlices(phase, 0) > 1);
            Assert.NotNull(gump.SearchResults);
            Assert.Equal(500, gump.SearchResults.Length);
        }
        finally
        {
            DeleteAll(entities);
            AdvancedSearchGump.EndSearch();
        }
    }

    [Fact]
    public void LoopPhase_IgnoreSourcesExcludeTheirChildren()
    {
        var from = new Mobile();
        var gump = new AdvancedSearchGump();
        var keyRing = new KeyRing();
        var key = new Key();
        var other = new Item(0x1);
        keyRing.Add(key);
        var entities = new List<IEntity> { from, keyRing, key, other };

        var filter = new AdvancedSearchFilter { HideValidInternalMap = true };
        AdvancedSearchResult[] results = [ResultFor(key), ResultFor(other)];
        ConcurrentQueue<IEntity> ignoreSources = [];
        ignoreSources.Enqueue(keyRing);

        try
        {
            var phase = NewPhase(from, gump, filter, results, [], ignoreSources);

            RunToCompletion(phase);

            Assert.NotNull(gump.SearchResults);
            Assert.Single(gump.SearchResults);
            Assert.Same(other, gump.SearchResults[0].Entity);
        }
        finally
        {
            DeleteAll(entities);
            AdvancedSearchGump.EndSearch();
        }
    }

    [Fact]
    public void LoopPhase_DisconnectedSearcher_FinishesQuietly()
    {
        var from = new Mobile(); // no NetState
        var gump = new AdvancedSearchGump();
        var item = new Item(0x1);
        var entities = new List<IEntity> { from, item };
        ConcurrentQueue<IEntity> deferred = [];
        deferred.Enqueue(item);

        Assert.True(AdvancedSearchGump.TryBeginSearch());

        try
        {
            var phase = NewPhase(from, gump, new AdvancedSearchFilter(), [], deferred, []);

            RunToCompletion(phase);

            Assert.NotNull(gump.SearchResults);
            Assert.Single(gump.SearchResults);
            Assert.False(AdvancedSearchGump.IsSearchInProgress);
        }
        finally
        {
            DeleteAll(entities);
            AdvancedSearchGump.EndSearch();
        }
    }

    [Fact]
    public void LoopPhase_LargeResultSet_PublishesAcrossSeveralSlices()
    {
        var from = new Mobile();
        var gump = new AdvancedSearchGump();
        var keyRing = new KeyRing();
        var entities = new List<IEntity> { from, keyRing };
        List<IEntity> expected = [];
        List<AdvancedSearchResult> workerResults = [];

        for (var i = 0; i < 2000; i++)
        {
            var item = new Item(0x1);
            entities.Add(item);
            workerResults.Add(ResultFor(item));

            if (i % 100 == 0)
            {
                item.Delete();
            }
            else if (i % 100 == 1)
            {
                var key = new Key();
                entities.Add(key);
                keyRing.Add(key);
                workerResults[^1] = ResultFor(key);
            }
            else
            {
                expected.Add(item);
            }
        }

        // Fewer than one budget interval, so the drains end inside the first slice.
        ConcurrentQueue<IEntity> deferred = [];
        for (var i = 0; i < 10; i++)
        {
            // On a real map, or HideValidInternalMap rejects it as a stray internal item.
            var item = new Item(0x1);
            item.MoveToWorld(new Point3D(1000 + i, 1000, 0), Map.Felucca);
            entities.Add(item);
            expected.Add(item);
            deferred.Enqueue(item);
        }

        ConcurrentQueue<IEntity> ignoreSources = [];
        ignoreSources.Enqueue(keyRing);

        try
        {
            var phase = NewPhase(from, gump, new AdvancedSearchFilter { HideValidInternalMap = true }, workerResults.ToArray(), deferred, ignoreSources);

            // Slice 1 drains, and publishing then takes ~2000/64 slices of its own.
            var slices = RunSlices(phase, 0);
            Assert.True(slices > 3, $"published in {slices} slices");

            expected.Sort((a, b) => a.Serial.CompareTo(b.Serial));
            Assert.NotNull(gump.SearchResults);
            Assert.Equal(expected.Count, gump.SearchResults.Length);
            for (var i = 0; i < expected.Count; i++)
            {
                Assert.Same(expected[i], gump.SearchResults[i].Entity);
            }
        }
        finally
        {
            DeleteAll(entities);
            AdvancedSearchGump.EndSearch();
        }
    }

    [Fact]
    public void LoopPhase_MergesWorkerAndLoopResultsInSerialOrder()
    {
        var from = new Mobile();
        var gump = new AdvancedSearchGump();
        var entities = new List<IEntity> { from };
        List<AdvancedSearchResult> workerResults = [];
        ConcurrentQueue<IEntity> deferred = [];
        List<IEntity> all = [];

        // Interleaved serials: even-indexed items come from the workers, odd-indexed from the loop.
        for (var i = 0; i < 20; i++)
        {
            var item = new Item(0x1);
            entities.Add(item);
            all.Add(item);

            if (i % 2 == 0)
            {
                workerResults.Add(ResultFor(item));
            }
            else
            {
                deferred.Enqueue(item);
            }
        }

        try
        {
            var phase = NewPhase(from, gump, new AdvancedSearchFilter(), workerResults.ToArray(), deferred, []);

            RunToCompletion(phase);

            all.Sort((a, b) => a.Serial.CompareTo(b.Serial));
            Assert.NotNull(gump.SearchResults);
            Assert.Equal(all.Count, gump.SearchResults.Length);
            for (var i = 0; i < all.Count; i++)
            {
                Assert.Same(all[i], gump.SearchResults[i].Entity);
            }
        }
        finally
        {
            DeleteAll(entities);
            AdvancedSearchGump.EndSearch();
        }
    }

    [Fact]
    public void LoopPhase_LargeDeferredSet_SortsOffTheLoopAndPublishesInSerialOrder()
    {
        var from = new Mobile();
        var gump = new AdvancedSearchGump();
        var entities = new List<IEntity> { from };
        ConcurrentQueue<IEntity> deferred = [];
        List<Item> items = [];

        for (var i = 0; i < 3000; i++)
        {
            var item = new Item(0x1);
            entities.Add(item);
            items.Add(item);
        }

        // Highest serial first, so the drain collects the loop results in reverse order.
        for (var i = items.Count - 1; i >= 0; i--)
        {
            deferred.Enqueue(items[i]);
        }

        var original = Core.LoopContext;
        var loop = new EventLoopContext();
        Core.LoopContext = loop;

        try
        {
            var phase = NewPhase(from, gump, new AdvancedSearchFilter(), [], deferred, []);

            // The drain hands the sort to the thread pool; its completion waits in the loop's queue.
            Assert.False(phase.RunSlice(long.MaxValue));
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (loop.IsEmpty)
            {
                Assert.True(DateTime.UtcNow < deadline, "the sort never posted back to the loop");
                System.Threading.Thread.Yield();
            }

            // Until the loop runs that completion, slices do nothing.
            Assert.False(phase.RunSlice(long.MaxValue));
            Assert.False(phase.RunSlice(long.MaxValue));
            Assert.Null(gump.SearchResults);

            loop.ExecuteTasks();

            var slices = 0;
            while (!phase.RunSlice(long.MaxValue))
            {
                Assert.True(++slices <= 4, "allocate, merge, publish should follow the sort");
            }

            Assert.NotNull(gump.SearchResults);
            Assert.Equal(items.Count, gump.SearchResults.Length);
            for (var i = 0; i < items.Count; i++)
            {
                Assert.Same(items[i], gump.SearchResults[i].Entity);
            }
        }
        finally
        {
            Core.LoopContext = original;
            DeleteAll(entities);
            AdvancedSearchGump.EndSearch();
        }
    }

    [Fact]
    public void LoopPhase_Throws_RestoresAutoSaveAndReleasesGuard()
    {
        var original = AutoSave.SavesEnabled;
        var from = new Mobile();
        var item = new Item(0x1);
        var entities = new List<IEntity> { from, item };
        ConcurrentQueue<IEntity> deferred = [];
        deferred.Enqueue(item);

        Assert.True(AdvancedSearchGump.TryBeginSearch());
        AutoSave.SavesEnabled = false;

        try
        {
            // A null gump makes publishing throw, exercising the phase-level failure path.
            var phase = new AdvancedSearchLoopPhase(
                from, null, new AdvancedSearchFilter(), Origin, NewCache(),
                [], deferred, [], true
            );

            RunToCompletion(phase);

            Assert.True(AutoSave.SavesEnabled);
            Assert.False(AdvancedSearchGump.IsSearchInProgress);
        }
        finally
        {
            AutoSave.SavesEnabled = original;
            DeleteAll(entities);
            AdvancedSearchGump.EndSearch();
        }
    }
}
