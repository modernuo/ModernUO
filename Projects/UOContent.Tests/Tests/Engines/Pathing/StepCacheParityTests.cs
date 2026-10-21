using System;
using System.Collections.Generic;
using Server.Engines.Pathing.Cache;
using Server.Tests.Terrain;
using Xunit;
using Xunit.Abstractions;
using static Server.Tests.Pathfinding.PathingTestSupport;

namespace Server.Tests.Pathfinding;

/// <summary>
/// The cache is only worth having if it answers exactly as MovementImpl would. These tests pin
/// that down at each layer, so a failure says which one broke:
///
///   <see cref="ProbeMatchesSlowPath"/>          StepProbe vs MovementImpl — does the bake compute the right answer?
///   <see cref="CacheMatchesProbe"/>             StepCache vs StepProbe    — does the chunk store and return it intact?
///   <see cref="CacheServesReachableWalkStates"/> StepCache vs MovementImpl — end to end, over the states A* actually visits.
///
/// The end-to-end test is the one that matters, but it can only tell you something is wrong; the
/// two layer tests tell you where. It also measures coverage, not just correctness — a cache that
/// falls through on everything agrees with the slow path perfectly and is worthless.
/// </summary>
[Collection("Sequential Pathfinding Tests")]
public class StepCacheParityTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly StepCacheStateScope _cacheState = new();

    public StepCacheParityTests(ITestOutputHelper output) => _output = output;

    public void Dispose() => _cacheState.Dispose();

    // "Plain" is the synthetic map's default ground (flat, no statics); every other name is a prefab.
    private static Rectangle2D Region(string scene)
    {
        if (scene == "Plain")
        {
            return new Rectangle2D(PlainX, PlainY, 32, 32);
        }

        var prefab = Prefabs.Named(scene);
        prefab.AssertPresent();
        return prefab.Bounds;
    }

    private static Point3D Seed(string scene) =>
        scene == "Plain" ? new Point3D(PlainX, PlainY, PlainZ) : Prefabs.Named(scene).Anchor;

    // ---- layer 1: the bake agrees with MovementImpl ----

    /// <summary>
    /// Sweeps a region and compares StepProbe's mask against MovementImpl for all 8 directions.
    /// The probe stores raw masks and leaves the diagonal corner-cut to the caller, so the rule has
    /// to be applied here before the two are comparable.
    /// </summary>
    [Theory]
    [InlineData("Town")]
    [InlineData("Plain")]
    public void ProbeMatchesSlowPath(string label)
    {
        var region = Region(label);
        int xStart = region.X, yStart = region.Y, size = region.Width;

        var map = TestMap;
        Assert.NotNull(map);

        var walker = new WalkerStub();
        walker.MoveToWorld(new Point3D(xStart, yStart, 0), map);

        var disagreements = 0;
        var samples = 0;
        var walkable = 0;

        for (var x = xStart; x < xStart + size; x++)
        {
            for (var y = yStart; y < yStart + size; y++)
            {
                map.GetAverageZ(x, y, out _, out var avgZ, out _);
                var sourceZ = (sbyte)avgZ;
                var loc = new Point3D(x, y, sourceZ);

                var probe = StepProbe.ComputeMaskAt(map, x, y, sourceZ);

                for (var d = 0; d < 8; d++)
                {
                    var dir = (Direction)d;
                    samples++;

                    var slowOk = Movement.Movement.CheckMovement(walker, map, loc, dir, out var slowZ);

                    // Creature corner-cut: a diagonal needs at least one flanking cardinal.
                    var probeOk = probe.IsWalkable(dir);
                    if (probeOk && (d & 1) == 1)
                    {
                        probeOk = probe.IsWalkable((Direction)((d - 1) & 7)) || probe.IsWalkable((Direction)((d + 1) & 7));
                    }

                    if (slowOk)
                    {
                        walkable++;
                    }

                    if (slowOk != probeOk)
                    {
                        disagreements++;
                        _output.WriteLine($"WALKABLE DIFF @ ({x},{y},{sourceZ}) dir={dir} slow={slowOk} probe={probeOk}");
                    }
                    else if (slowOk && slowZ != probe.GetWalkZ(dir))
                    {
                        disagreements++;
                        _output.WriteLine($"Z DIFF @ ({x},{y},{sourceZ}) dir={dir} slow={slowZ} probe={probe.GetWalkZ(dir)}");
                    }
                }
            }
        }

        walker.Delete();
        _output.WriteLine($"[{label}] samples={samples} walkable={walkable} disagreements={disagreements}");

        // The dense region must contain a mix. All-walkable or all-blocked would mean the sweep
        // agreed about nothing interesting.
        if (label == "Town")
        {
            Assert.NotEqual(0, walkable);
            Assert.NotEqual(samples, walkable);
        }

        Assert.Equal(0, disagreements);
    }

    /// <summary>
    /// The swim bake must actually produce swim output. A probe that silently returned an empty
    /// WetMask everywhere would pass every parity test above — walkers would still agree — while
    /// leaving every swimming creature unable to move.
    /// </summary>
    [Fact]
    public void ProbeBakesWetCells_OnACoastline()
    {
        var map = TestMap;
        Assert.NotNull(map);

        var coast = Prefabs.Coastline;
        coast.AssertPresent();
        int xStart = coast.Bounds.X, yStart = coast.Bounds.Y, size = coast.Bounds.Width;

        var wetCells = 0;
        for (var x = xStart; x < xStart + size; x++)
        {
            for (var y = yStart; y < yStart + size; y++)
            {
                map.GetAverageZ(x, y, out _, out var avgZ, out _);
                var sourceZ = (sbyte)StepProbe.ComputeStandingZ(map, x, y, avgZ);
                if (StepProbe.ComputeMaskAt(map, x, y, sourceZ).WetMask != 0)
                {
                    wetCells++;
                }
            }
        }

        _output.WriteLine($"coastline: wetCells={wetCells} of {size * size}");
        Assert.True(wetCells > 0, $"swim bake produced zero wet cells across a {size}x{size} coastal region");
    }

    // ---- layer 2: the chunk returns what was baked ----

    /// <summary>
    /// Sweeps a region and compares what the cache serves against what StepProbe computes for the
    /// same cell. The chunk is built from the probe, so any disagreement is a storage fault — a
    /// bad cell index, a Z array crossed with another, a guard firing when it shouldn't.
    ///
    /// Queries run at the cell's standable surface Z, which is where the cache anchors. Querying at
    /// the land average instead would trip the source-Z guard on raised terrain (a causeway, a
    /// walkway) and report a fallthrough that is correct behaviour rather than a fault.
    /// </summary>
    [Theory]
    [InlineData("Town")]
    [InlineData("Plain")]
    [InlineData("Causeway")]
    public void CacheMatchesProbe(string label)
    {
        // A storage fault (crossed cell index, swapped Z array) only shows against varied terrain;
        // on flat ground every cell bakes the same answer, so the plain alone proves nothing.
        var region = Region(label);
        int xStart = region.X, yStart = region.Y, size = region.Width;

        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1; // build on first touch: every cell should get a real answer

        var map = TestMap;
        Assert.NotNull(map);

        var disagreements = 0;
        var samples = 0;
        var multiZ = 0;

        Span<sbyte> surfaces = stackalloc sbyte[16];

        for (var x = xStart; x < xStart + size; x++)
        {
            for (var y = yStart; y < yStart + size; y++)
            {
                if (StepProbe.ComputeStandableSurfaceZs(map, x, y, surfaces) == 0)
                {
                    continue; // nothing for a walker to stand on here
                }

                var sourceZ = surfaces[0];
                var probe = StepProbe.ComputeMaskAt(map, x, y, sourceZ);
                var cached = cache.TryGetMask(map, x, y, sourceZ);
                samples++;

                if (cached.HitKind == CacheHitKind.Fallthrough_MultiZ)
                {
                    multiZ++;
                    continue;
                }

                Assert.True(cached.IsHit, $"cache returned {cached.HitKind} at ({x},{y})");

                if (cached.WalkMask != probe.WalkMask)
                {
                    disagreements++;
                    _output.WriteLine($"WALK MASK DIFF @ ({x},{y}) cache=0x{cached.WalkMask:X2} probe=0x{probe.WalkMask:X2}");
                    continue;
                }

                if (cached.WetMask != probe.WetMask)
                {
                    disagreements++;
                    _output.WriteLine($"WET MASK DIFF @ ({x},{y}) cache=0x{cached.WetMask:X2} probe=0x{probe.WetMask:X2}");
                    continue;
                }

                for (var d = 0; d < 8; d++)
                {
                    var dir = (Direction)d;
                    if (cached.GetWalkZ(dir) != probe.GetWalkZ(dir))
                    {
                        disagreements++;
                        _output.WriteLine(
                            $"Z DIFF @ ({x},{y}) dir={dir} cache={cached.GetWalkZ(dir)} probe={probe.GetWalkZ(dir)}"
                        );
                        break;
                    }
                }
            }
        }

        _output.WriteLine($"[{label}] samples={samples} disagreements={disagreements} multiZ={multiZ}");

        // Guard the sweep itself: if every cell fell through as multi-Z, the comparison above never
        // actually ran and a zero disagreement count would mean nothing.
        if (label == "Town")
        {
            Assert.True(samples - multiZ > 0, "no cell produced a real cache answer — the sweep proved nothing");
        }

        Assert.Equal(0, disagreements);
    }

    // ---- layer 3: end to end, over the states A* actually visits ----

    /// <summary>
    /// Flood-fills outward from a known-walkable tile using MovementImpl itself, and demands the
    /// cache serve — and agree on — every state it reaches.
    ///
    /// The fill is what makes this meaningful. MovementImpl returns the Z a step lands on, so each
    /// reached (x, y, z) is a genuine standing state at its true Z: exactly the set A* would query,
    /// discovered rather than assumed. It follows stair treads up at their own Zs and climbs onto
    /// upper floors, so a single seed covers a whole connected structure with no fixed-Z guess to
    /// get wrong. That matters because the failure this test exists to catch — anchoring a cell at
    /// the land beneath a walkway instead of the walkway itself — is invisible to any test that
    /// queries at the land Z, and turned the Britain sewer into a ~98% cache miss.
    ///
    /// Cardinals only: the cache stores raw masks and applies the corner-cut at query time, so a
    /// raw diagonal bit legitimately differs from MovementImpl's diagonal answer.
    /// </summary>
    [Theory]
    // Scenes chosen for the terrain classes the standable-surface bake has to get right. Each seed
    // floods across a wide local area, so a handful covers thousands of states without a map walk.
    [InlineData("SewerWalkway", 2500)] // static walkway over impassable land
    [InlineData("Town", 2500)]         // buildings, and a ramp up to a deck over open ground (two Zs per cell)
    [InlineData("Causeway", 2500)]     // bridge over impassable land
    [InlineData("Plain", 2500)]        // flat ground: catches clearance false-positives
    public void CacheServesReachableWalkStates(string label, int maxStates)
    {
        Region(label);
        var seed = Seed(label);
        int sx = seed.X, sy = seed.Y, sz = seed.Z;

        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1; // build on first touch: every reached state should be answered

        var map = TestMap;
        Assert.NotNull(map);

        var walker = new WalkerStub();
        walker.MoveToWorld(new Point3D(sx, sy, sz), map);

        var startIsWalkable = false;
        for (var d = 0; d < 8 && !startIsWalkable; d++)
        {
            startIsWalkable = Movement.Movement.CheckMovement(walker, map, new Point3D(sx, sy, sz), (Direction)d, out _);
        }

        Assert.True(startIsWalkable, $"[{label}] seed ({sx},{sy},{sz}) is not walkable — bad waypoint");

        var visited = new HashSet<(int x, int y, int z)> { (sx, sy, sz) };
        var frontier = new Queue<(int x, int y, int z)>();
        frontier.Enqueue((sx, sy, sz));

        var states = 0;
        var fellThrough = 0;
        var disagreements = 0;
        const int maxLog = 12;

        while (frontier.Count > 0)
        {
            var (x, y, z) = frontier.Dequeue();
            var loc = new Point3D(x, y, z);
            var cached = cache.TryGetMask(map, x, y, (sbyte)z);
            states++;

            if (!cached.IsHit)
            {
                if (fellThrough < maxLog)
                {
                    _output.WriteLine($"FELL THROUGH @ ({x},{y},{z}) hitKind={cached.HitKind}");
                }

                fellThrough++;
            }

            for (var d = 0; d < 8; d++)
            {
                var dir = (Direction)d;
                var slowOk = Movement.Movement.CheckMovement(walker, map, loc, dir, out var slowZ);

                if (slowOk)
                {
                    var nx = x;
                    var ny = y;
                    Movement.Movement.Offset(dir, ref nx, ref ny);

                    if (visited.Count < maxStates && visited.Add((nx, ny, slowZ)))
                    {
                        frontier.Enqueue((nx, ny, slowZ));
                    }
                }

                if ((d & 1) != 0 || !cached.IsHit)
                {
                    continue;
                }

                if (cached.IsWalkable(dir) != slowOk)
                {
                    if (disagreements < maxLog)
                    {
                        _output.WriteLine($"WALK DIFF @ ({x},{y},{z}) dir={dir} slow={slowOk} cache={cached.IsWalkable(dir)}");
                    }

                    disagreements++;
                }
                else if (slowOk && slowZ != cached.GetWalkZ(dir))
                {
                    if (disagreements < maxLog)
                    {
                        _output.WriteLine($"Z DIFF @ ({x},{y},{z}) dir={dir} slow={slowZ} cache={cached.GetWalkZ(dir)}");
                    }

                    disagreements++;
                }
            }
        }

        walker.Delete();

        var fallthroughPct = states == 0 ? 0 : 100.0 * fellThrough / states;
        _output.WriteLine($"[{label}] states={states} fellThrough={fellThrough} ({fallthroughPct:F2}%) disagreements={disagreements}");

        Assert.True(states > 50, $"[{label}] flood-fill stalled at {states} states — bad waypoint");

        // Where the cache answers at all, it must be right.
        Assert.Equal(0, disagreements);

        // And it must answer nearly everywhere. A small residual is legitimate: a walkable surface
        // directly beneath a bridge or stair ramp falls through because the bake's clearance check
        // is deliberately conservative there. An anchor regression is not small — the pre-fix sewer
        // fell through on ~98% — so a 1% ceiling separates the two comfortably.
        Assert.True(
            fallthroughPct < 1.0,
            $"[{label}] cache fell through on {fallthroughPct:F2}% ({fellThrough}/{states}) of reachable states"
        );
    }
}
