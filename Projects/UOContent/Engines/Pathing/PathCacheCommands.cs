using System;
using System.Diagnostics;
using System.IO;
using Server.Engines.Pathing.Cache;
using Server.Logging;

namespace Server.Engines.Pathing;

/// <summary>
/// Admin commands for inspecting and operating the pathfinding step cache.
///   [PathCacheStats — current resident-chunk count + hit/miss/eviction telemetry.
///   [PathCacheClear — drop all cached chunks, close lazy readers, zero counters.
///   [PathBake       — walk a whole map building the full static cache, then save it.
///   [PathCacheSave  — persist resident chunks per map to Data/Pathfinding/&lt;mapId&gt;.swb.
///   [PathCacheLoad  — open those files as lazy backing stores. Also runs at startup.
///   [PathRecord     — toggle JSONL telemetry capture for replay / benchmark corpora.
///
/// The step cache works WITHOUT any .swb file — chunks build on demand as creatures path.
/// A baked .swb is an optional optimization that removes first-pathfind-after-boot latency
/// for shard owners who want it; <see cref="OnPathBake"/> is how you produce one.
/// </summary>
public static class PathCacheCommands
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PathCacheCommands));

    // modernuo.json flag: when true, Initialize() bakes any missing/stale .swb at startup.
    // The first-boot ConfigurePrompts() prompt writes it.
    private const string PrebakeSetting = "pathfinding.prebakeMaps";

    private static string PathFor(int mapId) =>
        Path.Combine(Core.BaseDirectory, "Data", "Pathfinding", $"{mapId}.swb");

    public static void Configure()
    {
        // Resident-chunk cap is shard-tunable. Default 8192 ≈ 40 MB; small shards may
        // want lower, large shards (or full-map bakes) may want higher. Setting is
        // written back to server.cfg on first boot for discoverability.
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
    /// First-boot prompt, auto-invoked by <c>AssemblyHandler.Invoke("ConfigurePrompts")</c> in
    /// the startup sequence — after assemblies load (so content can prompt) but before Serilog
    /// starts, so the console prompt isn't interleaved with async log output. Offers to pre-bake
    /// the pathfinding <c>.swb</c> cache for the selected maps; the answer persists in
    /// modernuo.json (<see cref="PrebakeSetting"/>), so it's asked exactly once. Skipped when the
    /// setting already exists or when input is redirected (headless/CI) — operators can set the
    /// flag directly. The bake itself happens later in <see cref="Initialize"/>.
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
    /// Auto-invoked by <c>AssemblyHandler.Invoke("Initialize")</c> after the tile matrix and
    /// world are loaded. When <see cref="PrebakeSetting"/> is set, bakes any map whose
    /// <c>.swb</c> is missing or stale, so the first pathfind on each region is already warm. A
    /// fresh cache makes this a no-op, so only first boot — or a client/map update that changes
    /// the fingerprint — pays the cost.
    ///
    /// Validity is decided by <see cref="StepCache.HasLazyReader"/>: <see cref="Configure"/> runs
    /// <see cref="AutoLoadAtStartup"/> in the earlier Configure phase, opening (and fingerprint-
    /// validating) a reader for every up-to-date <c>.swb</c>. So a map with an open reader is
    /// already good and we skip it — no need to recompute the fingerprint a second time here.
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
                continue; // AutoLoadAtStartup already opened a fingerprint-valid .swb for this map
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
            AutoLoadAtStartup(); // (re)open the freshly written files as lazy backing stores
        }
    }

    /// <summary>
    /// Open Data/Pathfinding/&lt;mapId&gt;.swb as a lazy backing store for every map.
    /// Reads only the header + chunk-offset index up front (~16 bytes per chunk);
    /// individual chunk records are fetched on demand when the cache asks for them.
    /// RAM stays bounded by MaxResidentChunks regardless of file size.
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

            // BakeMap walks the whole map (building every chunk) and writes the .swb. The
            // chunks are left resident afterward; drop them so peak memory is bounded to one
            // map at a time and the post-command footprint returns to the LRU cap.
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

        // Reopen the freshly written files as lazy backing stores so they're usable now
        // without a restart (resident memory stays bounded by the LRU cap).
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
