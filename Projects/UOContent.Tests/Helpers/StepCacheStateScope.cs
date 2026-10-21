using System;
using Server.Engines.Pathing.Cache;
using Server.Systems.FeatureFlags;

namespace Server.Tests;

/// <summary>
/// Snapshots the pathfinding cache's tuning knobs and feature flag, and restores them on dispose
/// along with empty caches. StepCache and MultiMaskCache are process-wide singletons and
/// <see cref="StepCache.Clear"/> leaves the knobs alone, so a test that tunes one would otherwise
/// leak it into every test that runs after it.
/// </summary>
internal sealed class StepCacheStateScope : IDisposable
{
    private readonly int _missPromotionThreshold;
    private readonly uint _missPromotionWindowMs;
    private readonly int _maxResidentChunks;
    private readonly bool _preloadOnLazyOpen;
    private readonly bool _cacheEnabled;

    public StepCacheStateScope()
    {
        var cache = StepCache.Instance;
        _missPromotionThreshold = cache.MissPromotionThreshold;
        _missPromotionWindowMs = cache.MissPromotionWindowMs;
        _maxResidentChunks = cache.MaxResidentChunks;
        _preloadOnLazyOpen = cache.PreloadOnLazyOpen;
        _cacheEnabled = ContentFeatureFlags.BitmapPathfindingCache;
    }

    public void Dispose()
    {
        var cache = StepCache.Instance;

        // Clear first: it closes any lazy reader a test opened under the knobs it set.
        cache.Clear();
        MultiMaskCache.Instance.Clear();

        cache.MissPromotionThreshold = _missPromotionThreshold;
        cache.MissPromotionWindowMs = _missPromotionWindowMs;
        cache.MaxResidentChunks = _maxResidentChunks;
        cache.PreloadOnLazyOpen = _preloadOnLazyOpen;
        ContentFeatureFlags.BitmapPathfindingCache = _cacheEnabled;
    }
}
