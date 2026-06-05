using System;
using System.Diagnostics;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms.BitmapAStar;
using Server.Targeting;

namespace Server.Engines.Pathing;

/// <summary>
/// Developer diagnostic for the bitmap A* step cache. Stand where a creature would start,
/// run <c>[PathDiag</c>, and target the goal. Reports, for that route:
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
/// baked at the right Z. Output goes to both the invoking client and the server console.
///
/// The promotion gate is forced to eager (threshold 1) for the duration so the cache builds
/// on first touch and the numbers reflect its best case; the previous threshold is restored
/// afterward.
/// </summary>
public static class PathDiag
{
    private const int TimingIterations = 200;

    public static void Configure()
    {
        CommandSystem.Register("PathDiag", AccessLevel.Administrator, OnPathDiag);
    }

    [Usage("PathDiag")]
    [Description("Diagnoses the step cache for a route: target a tile to see start/goal tile makeup, the per-Find cache hit/fallthrough breakdown, and warm timing.")]
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

        Out(from, $"PathDiag: ({start.X},{start.Y},{start.Z}) -> ({goal.X},{goal.Y},{goal.Z}) on {map}");

        if (!Utility.InRange(start, goal, 38))
        {
            Out(from, "PathDiag: goal is outside the A* search window (38 tiles); aborting.");
            return;
        }

        var cache = StepCache.Instance;
        var previousThreshold = cache.MissPromotionThreshold;
        cache.MissPromotionThreshold = 1; // eager build — measure the cache's best case

        try
        {
            DumpCell(from, map, start.X, start.Y, start.Z, "start");
            DumpCell(from, map, goal.X, goal.Y, goal.Z, "goal");
            RunInstrumentedFind(from, map, start, goal);
            TimeWarm(from, map, start, goal);
        }
        finally
        {
            cache.MissPromotionThreshold = previousThreshold;
        }
    }

    /// <summary>
    /// Prints the raw tile makeup of one cell plus the surfaces the baker anchors to. A large
    /// gap between the query Z and the standable surfaces is the signature of a route the
    /// cache can't serve (the creature stands on a static surface far from the land average).
    /// </summary>
    private static void DumpCell(Mobile from, Map map, int x, int y, int queryZ, string label)
    {
        map.GetAverageZ(x, y, out var landZ, out var avgZ, out var landTop);
        var landTile = map.Tiles.GetLandTile(x, y);
        var landFlags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;
        var landImpassable = (landFlags & TileFlag.Impassable) != 0;
        var landWet = (landFlags & TileFlag.Wet) != 0;

        Span<sbyte> surfaces = stackalloc sbyte[16];
        var surfaceCount = StepProbe.ComputeStandableSurfaceZs(map, x, y, surfaces);

        Out(from, $"PathDiag {label} cell ({x},{y}) queryZ={queryZ}:");
        Out(from, $"  land: avgZ={avgZ} landZ={landZ} landTop={landTop} impassable={landImpassable} wet={landWet} ignored={landTile.Ignored}");

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < surfaceCount; i++)
        {
            sb.Append(i == 0 ? "" : ",").Append(surfaces[i]);
        }
        Out(from, $"  standable surfaces={surfaceCount} [{sb}] -> {(surfaceCount >= 2 ? "multi-Z (strata)" : "single-Z anchor")}");

        Out(from, "  static/multi surfaces:");
        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            Out(from, $"    id=0x{tile.ID:X4} z={tile.Z} top={tile.Z + data.CalcHeight} h={data.Height} surface={data.Surface} impass={data.Impassable} bridge={data.Bridge} wet={data.Wet}");
        }
    }

    /// <summary>
    /// Runs one warm Find and reports the StepCache counter delta for it — the per-pathfind
    /// cache hit/fallthrough mix and the fallthrough fraction.
    /// </summary>
    private static void RunInstrumentedFind(Mobile from, Map map, Point3D start, Point3D goal)
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

        Out(from, $"PathDiag warm Find: {(path == null ? "NO PATH" : $"{path.Length} steps")}");
        Out(from, $"  cache-served={served}  fallthrough={fallthrough} ({pct:F1}% of {total} probes)");
        Out(from, $"    fallthrough breakdown: multiZ={after.FallthroughMultiZ - before.FallthroughMultiZ} " +
                  $"srcZ={after.FallthroughSourceZMismatch - before.FallthroughSourceZMismatch} " +
                  $"offMap={after.FallthroughOffMap - before.FallthroughOffMap} " +
                  $"notBuilt={after.FallthroughNotBuilt - before.FallthroughNotBuilt}");
        if (path == null)
        {
            Out(from, "  NO PATH: goal unreachable within the 38-tile window (or not standable). This is an A* scope limit, independent of the cache.");
        }
    }

    private static void TimeWarm(Mobile from, Map map, Point3D start, Point3D goal)
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
        Out(from, $"PathDiag timing over {TimingIterations} warm Finds: min={minTicks * usPerTick:F1}us avg={totalTicks * usPerTick / TimingIterations:F1}us");
    }

    private static void Out(Mobile from, string message)
    {
        from.SendMessage(message);
        Console.WriteLine(message);
    }
}
