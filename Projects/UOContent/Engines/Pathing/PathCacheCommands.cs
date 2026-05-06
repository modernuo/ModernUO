using Server.Engines.Pathing.Cache;

namespace Server.Engines.Pathing;

/// <summary>
/// Admin commands for inspecting and operating the pathfinding step cache.
///   [PathCacheStats — current resident-chunk count + hit/miss/eviction telemetry.
///   [PathCacheClear — drop all cached chunks and zero counters.
/// </summary>
public static class PathCacheCommands
{
    public static void Configure()
    {
        CommandSystem.Register("PathCacheStats", AccessLevel.Administrator, OnPathCacheStats);
        CommandSystem.Register("PathCacheClear", AccessLevel.Administrator, OnPathCacheClear);
    }

    [Usage("PathCacheStats")]
    [Description("Reports StepCache resident-chunk count and hit/miss/eviction telemetry.")]
    private static void OnPathCacheStats(CommandEventArgs e)
    {
        var stats = StepCache.Instance.GetStats();
        var from = e.Mobile;

        from.SendMessage($"StepCache: {stats.ResidentChunks} chunks resident");
        from.SendMessage($"  builds={stats.BuildsTotal} hits={stats.Hits}");
        from.SendMessage($"  miss(notBuilt)={stats.MissesNotBuilt} miss(dirty)={stats.MissesDirtyRebuild}");
        from.SendMessage($"  fallthru(multiZ)={stats.FallthroughMultiZ} fallthru(offMap)={stats.FallthroughOffMap} fallthru(srcZ)={stats.FallthroughSourceZMismatch}");
        from.SendMessage($"  evictions(lruCap)={stats.EvictionsByLruCap}");
    }

    [Usage("PathCacheClear")]
    [Description("Drops all StepCache resident chunks and zeros the telemetry counters.")]
    private static void OnPathCacheClear(CommandEventArgs e)
    {
        var residentBefore = StepCache.Instance.GetStats().ResidentChunks;
        StepCache.Instance.Clear();
        e.Mobile.SendMessage($"StepCache cleared: {residentBefore} chunks dropped, counters reset.");
    }
}
