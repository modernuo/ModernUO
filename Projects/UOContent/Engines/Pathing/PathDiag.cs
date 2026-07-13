using System;
using System.Diagnostics;
using System.IO;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms;
using Server.Targeting;

namespace Server.Engines.Pathing;

/// <summary>
/// Diagnoses why the step cache does or doesn't serve a given route. Stand where the creature
/// would start, run <c>[PathDiag</c>, target the goal; the full report lands in
/// <c>Logs/pathdiag.log</c> and a summary goes to the client. It reports the tile makeup of the
/// start and goal cells alongside the standable surfaces the baker anchors to, the cache
/// hit/fallthrough breakdown for one warm Find, and warm timings.
///
/// The fallthrough fraction is the number to read: a high one means the cache is paying for a
/// lookup on every cell and then taking the slow path anyway. That usually points at
/// static-over-land geometry — dungeon walkways, bridges, stairs, stacked floors — baking at the
/// wrong Z, which is why this is most useful when bringing up a custom map or facet.
///
/// The promotion gate is forced eager for the duration, so the numbers reflect the cache's best
/// case rather than an artifact of chunks not having been built yet.
/// </summary>
public static class PathDiag
{
    private const int TimingIterations = 200;

    private static string LogPath => Path.Combine(Core.BaseDirectory, "Logs", "pathdiag.log");

    public static void Configure()
    {
        CommandSystem.Register("PathDiag", AccessLevel.Administrator, OnPathDiag);
    }

    [Usage("PathDiag")]
    [Description("Diagnoses the step cache for a route (results appended to Logs/pathdiag.log): target a tile to record start/goal tile makeup, the per-Find cache hit/fallthrough breakdown, and warm timing.")]
    private static void OnPathDiag(CommandEventArgs e)
    {
        var start = e.Mobile.Location;
        e.Mobile.SendMessage("PathDiag: target the goal tile.");
        e.Mobile.BeginTarget(-1, true, TargetFlags.None, (from, targeted) => OnTarget(from, start, targeted));
    }

    private static void OnTarget(Mobile from, Point3D start, object targeted)
    {
        if (targeted is not IPoint3D p)
        {
            return;
        }

        var map = from.Map;
        var goal = new Point3D(p.X, p.Y, p.Z);

        if (!Utility.InRange(start, goal, 38))
        {
            from.SendMessage("PathDiag: goal is outside the A* search window (38 tiles); aborting.");
            return;
        }

        var cache = StepCache.Instance;
        var previousThreshold = cache.MissPromotionThreshold;
        cache.MissPromotionThreshold = 1; // build eagerly, so we measure the cache's best case

        StreamWriter log = null;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            log = new StreamWriter(new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.Read));

            log.WriteLine($"===== [{Core.Now:yyyy-MM-dd HH:mm:ss}] PathDiag ({start.X},{start.Y},{start.Z}) -> ({goal.X},{goal.Y},{goal.Z}) on {map} =====");

            DumpCell(log, map, start.X, start.Y, start.Z, "start");
            DumpCell(log, map, goal.X, goal.Y, goal.Z, "goal");
            var find = RunInstrumentedFind(log, from, map, start, goal);
            var (minUs, avgUs) = TimeWarm(log, from, map, start, goal);
            log.WriteLine();

            from.SendMessage($"PathDiag ({start.X},{start.Y},{start.Z})->({goal.X},{goal.Y},{goal.Z}): {find.result}");
            from.SendMessage($"  cache fallthrough {find.fallthroughPct:F1}% of {find.total}; warm min={minUs:F1}us avg={avgUs:F1}us");
            from.SendMessage($"  full report appended to Logs/pathdiag.log");
        }
        catch (IOException ex)
        {
            from.SendMessage($"PathDiag: failed to write {LogPath}: {ex.Message}");
        }
        finally
        {
            log?.Dispose();
            cache.MissPromotionThreshold = previousThreshold;
        }
    }

    /// <summary>
    /// Dumps one cell's tiles and the surfaces the baker anchors to. A wide gap between the query Z
    /// and every standable surface is the signature of a cell the cache can't serve.
    /// </summary>
    private static void DumpCell(TextWriter log, Map map, int x, int y, int queryZ, string label)
    {
        map.GetAverageZ(x, y, out var landZ, out var avgZ, out var landTop);
        var landTile = map.Tiles.GetLandTile(x, y);
        var landFlags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;
        var landImpassable = (landFlags & TileFlag.Impassable) != 0;
        var landWet = (landFlags & TileFlag.Wet) != 0;

        Span<sbyte> surfaces = stackalloc sbyte[16];
        var surfaceCount = StepProbe.ComputeStandableSurfaceZs(map, x, y, surfaces);

        log.WriteLine($"{label} cell ({x},{y}) queryZ={queryZ}:");
        log.WriteLine($"  land: avgZ={avgZ} landZ={landZ} landTop={landTop} impassable={landImpassable} wet={landWet} ignored={landTile.Ignored}");

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < surfaceCount; i++)
        {
            sb.Append(i == 0 ? "" : ",").Append(surfaces[i]);
        }
        log.WriteLine($"  standable surfaces={surfaceCount} [{sb}] -> {(surfaceCount >= 2 ? "multi-Z (strata)" : "single-Z anchor")}");

        log.WriteLine("  static/multi surfaces:");
        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            log.WriteLine($"    id=0x{tile.ID:X4} z={tile.Z} top={tile.Z + data.CalcHeight} h={data.Height} surface={data.Surface} impass={data.Impassable} bridge={data.Bridge} wet={data.Wet}");
        }
    }

    /// <summary>
    /// Runs one Find against a warm cache and reports the counter delta it produced — the
    /// hit/fallthrough mix for that single pathfind.
    /// </summary>
    private static (string result, double fallthroughPct, long total) RunInstrumentedFind(
        TextWriter log, Mobile from, Map map, Point3D start, Point3D goal
    )
    {
        var cache = StepCache.Instance;

        // Build every chunk the route touches first, so the measured Find below reports steady-state
        // behaviour rather than first-touch misses.
        for (var i = 0; i < 3; i++)
        {
            BitmapAStarAlgorithm.Instance.Find(from, map, start, goal);
        }

        var before = cache.GetStats();
        var path = BitmapAStarAlgorithm.Instance.Find(from, map, start, goal);
        var after = cache.GetStats();

        var served = after.Hits - before.Hits
                     + (after.MissesNotBuilt - before.MissesNotBuilt)
                     + (after.MissesDirtyRebuild - before.MissesDirtyRebuild);
        var fallthrough = after.FallthroughMultiZ - before.FallthroughMultiZ
                          + (after.FallthroughSourceZMismatch - before.FallthroughSourceZMismatch)
                          + (after.FallthroughOffMap - before.FallthroughOffMap)
                          + (after.FallthroughNotBuilt - before.FallthroughNotBuilt);
        var total = served + fallthrough;
        var pct = total == 0 ? 0 : 100.0 * fallthrough / total;
        var result = path == null ? "NO PATH" : $"{path.Length} steps";

        log.WriteLine($"warm Find: {result}");
        log.WriteLine($"  cache-served={served}  fallthrough={fallthrough} ({pct:F1}% of {total} probes)");
        log.WriteLine($"    fallthrough breakdown: multiZ={after.FallthroughMultiZ - before.FallthroughMultiZ} " +
                      $"srcZ={after.FallthroughSourceZMismatch - before.FallthroughSourceZMismatch} " +
                      $"offMap={after.FallthroughOffMap - before.FallthroughOffMap} " +
                      $"notBuilt={after.FallthroughNotBuilt - before.FallthroughNotBuilt}");
        if (path == null)
        {
            log.WriteLine("  NO PATH: goal unreachable within the 38-tile window (or not standable). This is an A* scope limit, independent of the cache.");
        }

        return (result, pct, total);
    }

    private static (double minUs, double avgUs) TimeWarm(TextWriter log, Mobile from, Map map, Point3D start, Point3D goal)
    {
        var sw = new Stopwatch();
        var minTicks = long.MaxValue;
        long totalTicks = 0;

        for (var i = 0; i < TimingIterations; i++)
        {
            sw.Restart();
            BitmapAStarAlgorithm.Instance.Find(from, map, start, goal);
            sw.Stop();
            totalTicks += sw.ElapsedTicks;
            if (sw.ElapsedTicks < minTicks)
            {
                minTicks = sw.ElapsedTicks;
            }
        }

        var usPerTick = 1_000_000.0 / Stopwatch.Frequency;
        var minUs = minTicks * usPerTick;
        var avgUs = totalTicks * usPerTick / TimingIterations;
        log.WriteLine($"timing over {TimingIterations} warm Finds: min={minUs:F1}us avg={avgUs:F1}us");
        return (minUs, avgUs);
    }
}
