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

    private static AdvancedSearchResult ResultFor(Item item) =>
        new(item.Name ?? item.ItemData.Name, item.GetType(), item.Location, item.Map, item.RootParent)
        {
            Entity = item
        };

    private static AdvancedSearchLoopPhase NewPhase(
        Mobile from,
        AdvancedSearchGump gump,
        AdvancedSearchFilter filter,
        ConcurrentQueue<AdvancedSearchResult> results,
        ConcurrentQueue<IEntity> deferred,
        ConcurrentQueue<IEntity> ignoreSources
    ) => new(from, gump, filter, Origin, NewCache(), results, deferred, ignoreSources, AutoSave.SavesEnabled);

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
    public void LoopPhase_FinishesDeferredAndPublishes()
    {
        var from = new Mobile();
        var gump = new AdvancedSearchGump();
        var a = new Item(0x1) { Hue = 5 };
        var b = new Item(0x1) { Hue = 5 };
        var entities = new List<IEntity> { from, a, b };

        var filter = new AdvancedSearchFilter { FilterPropertyTest = true, PropertyTest = "Hue=5" };
        var deferred = new ConcurrentQueue<IEntity>();
        deferred.Enqueue(b);
        deferred.Enqueue(a);

        try
        {
            var phase = NewPhase(from, gump, filter, new ConcurrentQueue<AdvancedSearchResult>(), deferred, new ConcurrentQueue<IEntity>());

            Assert.True(phase.RunSlice(long.MaxValue));

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

        var results = new ConcurrentQueue<AdvancedSearchResult>();
        results.Enqueue(ResultFor(kept));
        results.Enqueue(ResultFor(deletedResult));
        var deferred = new ConcurrentQueue<IEntity>();
        deferred.Enqueue(deletedDeferred);

        deletedResult.Delete();
        deletedDeferred.Delete();

        try
        {
            var phase = NewPhase(from, gump, new AdvancedSearchFilter(), results, deferred, new ConcurrentQueue<IEntity>());

            Assert.True(phase.RunSlice(long.MaxValue));

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
        var deferred = new ConcurrentQueue<IEntity>();

        for (var i = 0; i < 500; i++)
        {
            var item = new Item(0x1);
            entities.Add(item);
            deferred.Enqueue(item);
        }

        try
        {
            var phase = NewPhase(from, gump, new AdvancedSearchFilter(), new ConcurrentQueue<AdvancedSearchResult>(), deferred, new ConcurrentQueue<IEntity>());

            var slices = 1;
            while (!phase.RunSlice(0))
            {
                slices++;
                Assert.True(slices < 1000, "loop phase never finished");
            }

            Assert.True(slices > 1);
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
        var results = new ConcurrentQueue<AdvancedSearchResult>();
        results.Enqueue(ResultFor(key));
        results.Enqueue(ResultFor(other));
        var ignoreSources = new ConcurrentQueue<IEntity>();
        ignoreSources.Enqueue(keyRing);

        try
        {
            var phase = NewPhase(from, gump, filter, results, new ConcurrentQueue<IEntity>(), ignoreSources);

            Assert.True(phase.RunSlice(long.MaxValue));

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
        var deferred = new ConcurrentQueue<IEntity>();
        deferred.Enqueue(item);

        Assert.True(AdvancedSearchGump.TryBeginSearch());

        try
        {
            var phase = NewPhase(from, gump, new AdvancedSearchFilter(), new ConcurrentQueue<AdvancedSearchResult>(), deferred, new ConcurrentQueue<IEntity>());

            Assert.True(phase.RunSlice(long.MaxValue));

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
    public void LoopPhase_Throws_RestoresAutoSaveAndReleasesGuard()
    {
        var original = AutoSave.SavesEnabled;
        var from = new Mobile();
        var item = new Item(0x1);
        var entities = new List<IEntity> { from, item };
        var deferred = new ConcurrentQueue<IEntity>();
        deferred.Enqueue(item);

        Assert.True(AdvancedSearchGump.TryBeginSearch());
        AutoSave.SavesEnabled = false;

        try
        {
            // A null gump makes publishing throw: the phase-level failure path, not a per-entity one.
            var phase = new AdvancedSearchLoopPhase(
                from, null, new AdvancedSearchFilter(), Origin, NewCache(),
                new ConcurrentQueue<AdvancedSearchResult>(), deferred, new ConcurrentQueue<IEntity>(), true
            );

            Assert.True(phase.RunSlice(long.MaxValue));

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
