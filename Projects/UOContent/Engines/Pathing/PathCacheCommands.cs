using System;
using System.Diagnostics;
using System.IO;
using Server.Engines.Pathing.Cache;
using Server.Logging;

namespace Server.Engines.Pathing;

/// <summary>
/// Admin commands for inspecting and operating the pathfinding step cache.
///   [PathCacheStats — resident-chunk count and hit/miss/eviction telemetry.
///   [PathCacheClear — drop all cached chunks, close the .swb readers, zero the counters.
///   [PathBake       — build a map's full static cache and save it.
///   [PathCacheSave  — persist the resident chunks to Data/Pathfinding/&lt;mapId&gt;.swb.
///   [PathCacheLoad  — open those files as backing stores. Also runs at startup.
///   [PathRecord     — toggle capture of pathfind telemetry.
///
/// None of this is required: the cache builds chunks on demand as creatures path, with or without
/// a .swb on disk. Baking one is purely an optimization that trades disk and a few minutes of bake
/// time for the removal of first-pathfind-after-boot latency.
/// </summary>
public static class PathCacheCommands
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PathCacheCommands));

    // When set, Initialize() bakes any missing or stale .swb at startup. ConfigurePrompts() asks
    // for it on first boot.
    private const string PrebakeSetting = "pathfinding.prebakeMaps";

    private static string PathFor(int mapId) =>
        Path.Combine(Core.BaseDirectory, "Data", "Pathfinding", $"{mapId}.swb");

    public static void Configure()
    {
        // Resident-chunk cap, shard-tunable — the default works out to roughly 40 MB. Written back
        // to server.cfg on first boot so it's discoverable.
        StepCache.Instance.MaxResidentChunks = ServerConfiguration.GetOrUpdateSetting(
            "pathfinding.maxResidentChunks",
            8192
        );

        PathfindRecorder.Configure();

        CommandSystem.Register("PathCacheStats", AccessLevel.Administrator, OnPathCacheStats);
        CommandSystem.Register("PathCacheClear", AccessLevel.Administrator, OnPathCacheClear);
        CommandSystem.Register("PathBake",       AccessLevel.Administrator, OnPathBake);
        CommandSystem.Register("PathCacheSave",  AccessLevel.Administrator, OnPathCacheSave);
        CommandSystem.Register("PathCacheLoad",  AccessLevel.Administrator, OnPathCacheLoad);
        CommandSystem.Register("PathRecord",     AccessLevel.Administrator, OnPathRecord);
        AutoLoadAtStartup();
    }

    /// <summary>
    /// Asks, once, whether to pre-bake the .swb cache; <see cref="Initialize"/> does the work later.
    /// The answer persists, so the question is never repeated, and it's skipped entirely when input
    /// is redirected — a headless or CI boot sets <see cref="PrebakeSetting"/> directly instead.
    ///
    /// Runs in the ConfigurePrompts phase because that's the one window where content can prompt:
    /// assemblies are loaded, but Serilog hasn't started, so console output won't interleave with
    /// async log writes.
    /// </summary>
    public static void ConfigurePrompts()
    {
        if (ServerConfiguration.GetSetting(PrebakeSetting, (string)null) != null || Console.IsInputRedirected)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Pre-bake the pathfinding cache for your selected maps now?");
        Console.WriteLine("  Bakes each map's .swb so there is zero first-pathfind-after-boot latency.");
        Console.WriteLine("  Takes several minutes and ~tens of MB of disk per facet. You can also do");
        Console.WriteLine("  this later at runtime with [PathBake.");
        Console.Write("Pre-bake now? [y/N] ");

        var answer = Console.ReadLine()?.Trim();
        var prebake = answer?.StartsWith("y", StringComparison.OrdinalIgnoreCase) == true;

        ServerConfiguration.SetSetting(PrebakeSetting, prebake);
    }

    /// <summary>
    /// Bakes any map whose <c>.swb</c> is missing or stale, when <see cref="PrebakeSetting"/> is
    /// set. Runs in the Initialize phase, once the tile matrix and world are loaded. An up-to-date
    /// cache makes it a no-op, so the cost lands only on a first boot or after a client or map
    /// update moves the fingerprint.
    ///
    /// A map is judged up-to-date by whether it has an open reader. <see cref="AutoLoadAtStartup"/>
    /// already ran in the earlier Configure phase and only opens a reader for a .swb whose
    /// fingerprint validates, so an open reader is proof of a good bake — no need to fingerprint
    /// the map a second time here.
    /// </summary>
    public static void Initialize()
    {
        if (!ServerConfiguration.GetSetting(PrebakeSetting, false))
        {
            return;
        }

        var baked = 0;
        for (var i = 0; i < Map.Maps.Length; i++)
        {
            var map = Map.Maps[i];
            if (map == null || map == Map.Internal)
            {
                continue;
            }

            if (StepCache.Instance.HasLazyReader(map.MapID))
            {
                continue; // already has a fingerprint-valid .swb open
            }

            var path = PathFor(map.MapID);

            logger.Information(
                "PathBake: pre-baking map {MapId} (pathfinding.prebakeMaps) — this can take several minutes...",
                map.MapID
            );
            StepCache.Instance.BakeMap(map.MapID, path);
            StepCache.Instance.ClearResidentChunks();
            baked++;
        }

        if (baked > 0)
        {
            logger.Information("PathBake: pre-bake complete ({Count} map(s) written).", baked);
            AutoLoadAtStartup(); // reopen what we just wrote
        }
    }

    /// <summary>
    /// Opens Data/Pathfinding/&lt;mapId&gt;.swb as a backing store for every map. Only the header and
    /// index are read up front; chunk records are fetched as the cache asks for them, so resident
    /// memory stays bounded by the LRU cap however large the files are.
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
            StepCache.Instance.TryOpenLazyReader(PathFor(map.MapID), map.MapID);
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
        from.SendMessage($"  fallthru(multi)={stats.FallthroughMulti} fallthru(notBuilt)={stats.FallthroughNotBuilt}");
        from.SendMessage($"  multiLocalHits={stats.MultiLocalHits}");
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

    [Usage("PathBake [mapId]")]
    [Description("Walks every chunk of the given map (or all loaded maps) building the full static step cache, then saves it to Data/Pathfinding/<mapId>.swb so a future boot has zero first-pathfind latency. WARNING: blocks the game loop for several seconds and transiently uses hundreds of MB per map — run during maintenance, not peak hours.")]
    private static void OnPathBake(CommandEventArgs e)
    {
        var from = e.Mobile;
        int? only = e.Arguments.Length > 0 && int.TryParse(e.Arguments[0], out var parsed) ? parsed : null;

        from.SendMessage("PathBake: building the static step cache. The server will pause briefly per map...");

        var totalChunks = 0;
        var totalMaps = 0;
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < Map.Maps.Length; i++)
        {
            var map = Map.Maps[i];
            if (map == null || map == Map.Internal || only.HasValue && map.MapID != only.Value)
            {
                continue;
            }

            // BakeMap leaves every chunk it built resident. Drop them between maps so peak memory
            // is one map's worth rather than all of them, and the footprint afterwards is back
            // under the LRU cap.
            var written = StepCache.Instance.BakeMap(map.MapID, PathFor(map.MapID));
            StepCache.Instance.ClearResidentChunks();

            if (written > 0)
            {
                totalChunks += written;
                totalMaps++;
                from.SendMessage($"  map {map.MapID}: {written} chunks → {PathFor(map.MapID)}");
            }
        }

        sw.Stop();

        if (totalMaps == 0)
        {
            from.SendMessage(only.HasValue ? $"PathBake: map {only.Value} not loaded." : "PathBake: no maps to bake.");
            return;
        }

        // Reopen what we just wrote, so the bake is usable immediately without a restart.
        AutoLoadAtStartup();
        from.SendMessage($"PathBake: {totalChunks} chunks across {totalMaps} map(s) in {sw.Elapsed.TotalSeconds:F1}s; lazy readers reopened.");
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
    [Description("Opens Data/Pathfinding/<mapId>.swb as a lazy backing store for every map. Chunks are fetched on demand, so RAM stays bounded by the LRU cap regardless of file size.")]
    private static void OnPathCacheLoad(CommandEventArgs e)
    {
        var openedMaps = 0;
        for (var i = 0; i < Map.Maps.Length; i++)
        {
            var map = Map.Maps[i];
            if (map == null || map == Map.Internal)
            {
                continue;
            }
            if (StepCache.Instance.TryOpenLazyReader(PathFor(map.MapID), map.MapID))
            {
                openedMaps++;
            }
        }
        e.Mobile.SendMessage(
            $"StepCache: opened {openedMaps} map(s) for lazy loading (total readers open: {StepCache.Instance.OpenLazyReaderCount})."
        );
    }

    [Usage("PathRecord [on|off|flush|status]")]
    [Description("Toggles PathfindRecorder. With no arg, reports state. 'on' enables JSONL capture of every Find call; 'off' disables and flushes; 'flush' forces a buffer flush without disabling.")]
    private static void OnPathRecord(CommandEventArgs e)
    {
        var arg = (e.Arguments.Length > 0 ? e.Arguments[0] : "status").ToLowerInvariant();
        var from = e.Mobile;
        switch (arg)
        {
            case "on":
                {
                    PathfindRecorder.SetEnabled(true);
                    from.SendMessage(PathfindRecorder.Enabled
                        ? $"PathRecord: ON, writing to {PathfindRecorder.OutputPath}"
                        : "PathRecord: enable failed (see server log)");
                    break;
                }
            case "off":
                {
                    PathfindRecorder.SetEnabled(false);
                    from.SendMessage("PathRecord: OFF");
                    break;
                }
            case "flush":
                {
                    PathfindRecorder.Flush();
                    from.SendMessage($"PathRecord: flushed ({PathfindRecorder.RecordsWritten} records this session)");
                    break;
                }
            default:
                {
                    from.SendMessage(
                        $"PathRecord: {(PathfindRecorder.Enabled ? "ON" : "OFF")}, "
                        + $"path={PathfindRecorder.OutputPath}, records={PathfindRecorder.RecordsWritten}"
                    );
                    break;
                }
        }
    }
}
