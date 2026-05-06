using System.IO;
using Server.Engines.Pathing.Cache;

namespace Server.Engines.Pathing;

/// <summary>
/// Admin commands for inspecting and operating the pathfinding step cache.
///   [PathCacheStats — current resident-chunk count + hit/miss/eviction telemetry.
///   [PathCacheClear — drop all cached chunks and zero counters.
///   [PathCacheSave  — persist resident chunks per map to Data/Pathfinding/&lt;mapId&gt;.swb.
///   [PathCacheLoad  — read those files back. Also runs automatically at startup.
/// </summary>
public static class PathCacheCommands
{
    private static string PathFor(int mapId) =>
        Path.Combine(Core.BaseDirectory, "Data", "Pathfinding", $"{mapId}.swb");

    public static void Configure()
    {
        CommandSystem.Register("PathCacheStats", AccessLevel.Administrator, OnPathCacheStats);
        CommandSystem.Register("PathCacheClear", AccessLevel.Administrator, OnPathCacheClear);
        CommandSystem.Register("PathCacheSave",  AccessLevel.Administrator, OnPathCacheSave);
        CommandSystem.Register("PathCacheLoad",  AccessLevel.Administrator, OnPathCacheLoad);
        AutoLoadAtStartup();
    }

    /// <summary>
    /// Scan Data/Pathfinding/&lt;mapId&gt;.swb for every map and merge the chunks into
    /// the resident set. Runs once at server boot via <see cref="Configure"/>. Files
    /// whose TileData hash doesn't match the running server are rejected — see
    /// <see cref="StepCache.TryLoadFromFile"/>.
    /// </summary>
    private static void AutoLoadAtStartup()
    {
        for (var i = 0; i < Map.Maps.Length; i++)
        {
            var map = Map.Maps[i];
            if (map == null || map == Map.Internal)
            {
                continue;
            }
            StepCache.Instance.TryLoadFromFile(PathFor(map.MapID));
        }
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

    [Usage("PathCacheSave")]
    [Description("Persists resident StepCache chunks for every loaded map to Data/Pathfinding/<mapId>.swb.")]
    private static void OnPathCacheSave(CommandEventArgs e)
    {
        var totalChunks = 0;
        var totalMaps = 0;
        for (var i = 0; i < Map.Maps.Length; i++)
        {
            var map = Map.Maps[i];
            if (map == null || map == Map.Internal)
            {
                continue;
            }
            var path = PathFor(map.MapID);
            var written = StepCache.Instance.SaveToFile(path, map.MapID);
            if (written > 0)
            {
                totalChunks += written;
                totalMaps++;
                e.Mobile.SendMessage($"  map {map.MapID}: {written} chunks → {path}");
            }
        }
        e.Mobile.SendMessage($"StepCache saved: {totalChunks} chunks across {totalMaps} map(s).");
    }

    [Usage("PathCacheLoad")]
    [Description("Reads Data/Pathfinding/<mapId>.swb for every map and merges the chunks into the resident set.")]
    private static void OnPathCacheLoad(CommandEventArgs e)
    {
        var beforeResident = StepCache.Instance.GetStats().ResidentChunks;
        var loadedMaps = 0;
        for (var i = 0; i < Map.Maps.Length; i++)
        {
            var map = Map.Maps[i];
            if (map == null || map == Map.Internal)
            {
                continue;
            }
            if (StepCache.Instance.TryLoadFromFile(PathFor(map.MapID)))
            {
                loadedMaps++;
            }
        }
        var afterResident = StepCache.Instance.GetStats().ResidentChunks;
        e.Mobile.SendMessage(
            $"StepCache loaded: +{afterResident - beforeResident} chunks across {loadedMaps} map(s) (total resident: {afterResident})."
        );
    }
}
