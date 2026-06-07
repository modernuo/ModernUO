using System;
using System.Diagnostics;
using System.IO;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms;
using Server.Targeting;

namespace Server.Engines.Pathing;

/// <summary>
/// Developer diagnostic for the bitmap A* step cache. Stand where a creature would start,
/// run <c>[PathDiag</c>, and target the goal. The detailed report is appended to
/// <c>Logs/pathdiag.log</c>; a short summary is sent to the invoking client. For the route
/// it records:
///   1. the raw tile makeup of the start and goal cells (land + statics) and the
///      clearance-aware standable surfaces the baker anchors to — the ground truth for
///      "why does the cache (not) serve this cell";
///   2. one warm <see cref="StepCache.TryGetMask"/>-served Find with the per-pathfind cache
///      hit/fallthrough breakdown and fallthrough fraction — a high fallthrough fraction
///      means the cache isn't helping the route (it pays the lookup then uses the slow path);
///   3. warm timing over many iterations.
///
/// Primarily useful when bringing up custom maps / facets: it shows whether static-over-land
/// geometry (dungeon walkways, bridges, stairs, raised foundations, stacked floors) is being
/// baked at the right Z.
///
/// Output goes to a log file rather than the console because the live server uses Serilog and
/// raw Console writes interleave badly with it. The promotion gate is forced to eager
/// (threshold 1) for the duration so the cache builds on first touch and the numbers reflect
/// its best case; the previous threshold is restored afterward.
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
        cache.MissPromotionThreshold = 1; // eager build — measure the cache's best case

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
    /// Writes the raw tile makeup of one cell plus the surfaces the baker anchors to. A large
    /// gap between the query Z and the standable surfaces is the signature of a route the
    /// cache can't serve (the creature stands on a static surface far from the land average).
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
    /// Runs one warm Find and records the StepCache counter delta for it — the per-pathfind
    /// cache hit/fallthrough mix and the fallthrough fraction. Returns a summary for the
    /// caller to relay to the player.
    /// </summary>
    private static (string result, double fallthroughPct, long total) RunInstrumentedFind(
        TextWriter log, Mobile from, Map map, Point3D start, Point3D goal
    )
    {
        var cache = StepCache.Instance;

        // Warm every chunk the route touches before measuring.
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
