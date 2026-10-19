using System;
using Server.Engines.Pathing.Cache;
using Server.Tests.Terrain;
using Xunit;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class TerrainScopeCacheTests : IDisposable
{
    // Default ground, clear of every prefab.
    private const int X = 1300;
    private const int Y = 1300;

    private readonly StepCacheStateScope _cacheState = new();

    public void Dispose() => _cacheState.Dispose();

    [Fact]
    public void TerrainStampedInAScope_IsNotServedStaleFromTheStepCache()
    {
        var cache = StepCache.Instance;
        cache.MissPromotionThreshold = 1;
        var map = Map.Trammel;

        var open = cache.TryGetMask(map, X, Y, sourceZ: 0);
        Assert.True(open.IsHit);
        Assert.True(open.IsWalkable(Direction.North));

        using (TestTerrain.Scope())
        {
            TestTerrain.AddStatic(X, Y - 1, TestTiles.Wall, 0);

            Assert.False(cache.TryGetMask(map, X, Y, sourceZ: 0).IsWalkable(Direction.North));
        }

        Assert.True(cache.TryGetMask(map, X, Y, sourceZ: 0).IsWalkable(Direction.North));
    }

    [Fact]
    public void TerrainStampedAfterAQueryInsideAScope_IsNotServedStale()
    {
        var cache = StepCache.Instance;
        cache.MissPromotionThreshold = 1;
        var map = Map.Trammel;

        using var scope = TestTerrain.Scope();
        Assert.True(cache.TryGetMask(map, X, Y, sourceZ: 0).IsWalkable(Direction.North));

        TestTerrain.AddStatic(X, Y - 1, TestTiles.Wall, 0);

        Assert.False(cache.TryGetMask(map, X, Y, sourceZ: 0).IsWalkable(Direction.North));
    }
}
