using System.Collections.Generic;
using Server.Engines.Pathing.Cache;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding;

/// <summary>
/// Parity coverage for "walkable static surface above a land tile" terrain — sewers,
/// dungeon walkways, bridges, raised foundations, and stacked building floors.
///
/// The original parity tests only queried at the LAND-anchored standing Z and skipped
/// multi-Z fallthroughs, so they never noticed that a query at the REAL walk Z — the static
/// surface a creature actually stands on — returns
/// <see cref="CacheHitKind.Fallthrough_SourceZMismatch"/>, because the baker anchored
/// SourceZ at the land average instead of the walkway. In the Britain sewer that's a ~98%
/// cache miss on a known walk-path (confirmed via [PathDiag).
///
/// Method: flood-fill outward from a known-walkable start using
/// <see cref="Movement.Movement.CheckMovement"/> — the slow path the cache mirrors. Each
/// reached (x, y, z) is a genuine standing state at its TRUE Z (CheckMovement returns the
/// destination Z it lands on), exactly the set of states A* would query. For every reached
/// state the cache must serve a Hit and agree with the slow path. This naturally follows
/// ramped stairs (each tread at its own Z) and climbs to upper floors, so one start covers
/// the whole connected structure — no fragile fixed-Z assumption.
///
/// A bare test world has no spawned items/mobiles, so CheckMovement reduces to static
/// walkability (no door/dynamic interference). Parity restricted to cardinal directions:
/// the cache stores raw masks and applies the diagonal corner-cut at query time, so a raw
/// diagonal bit legitimately differs from CheckMovement's diagonal result.
///
/// EXPECTED: RED before the standable-surface bake (reached states fall through at their
/// true Z); GREEN after.
/// </summary>
[Collection("Sequential Pathfinding Tests")]
public class StepCacheStaticSurfaceParityTests
{
    private readonly ITestOutputHelper _output;

    public StepCacheStaticSurfaceParityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    // label, start X, Y, Z (a real in-game walkable tile), max states to explore. Seeds are
    // chosen to span the terrain classes the standable-surface bake must get right; the
    // flood-fill spreads from each across a wide local area, so a handful of seeds exercises
    // thousands of distinct (cell, Z) states without an exhaustive whole-map walk.
    //   sewer   — static walkway @ z=5 over impassable land; covers dungeon walkways + bridges.
    //   inn     — stair foot @ z=10; climbs the stairs onto the 1st & 2nd floors (multi-Z).
    //   plain   — open Britain ground; guards against clearance false-positives on flat land.
    //   town    — Britain cobblestones near the inn; mixed buildings, stairs, raised floors.
    [InlineData("brit_sewer_walkway", 6034, 1476, 5, 2500)]
    [InlineData("brit_inn_stairs_to_floors", 1495, 1628, 10, 2500)]
    [InlineData("trammel_open_plain", 1500, 1600, 10, 2500)]
    [InlineData("brit_town_cobblestones", 1494, 1626, 10, 2500)] // plain ground: guards against clearance false-positives
    public void CacheServesReachableWalkStates(string label, int sx, int sy, int sz, int maxStates)
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1; // eager build — expect the cache to answer every state

        var map = Map.Maps[1];
        Assert.NotNull(map);

        var stub = new ParityStubMobile();
        stub.MoveToWorld(new Point3D(sx, sy, sz), map);

        // Sanity: the start must itself be a walkable standing state via the slow path.
        var startWalkable = false;
        for (var d = 0; d < 8; d++)
        {
            if (Movement.Movement.CheckMovement(stub, map, new Point3D(sx, sy, sz), (Direction)d, out _))
            {
                startWalkable = true;
                break;
            }
        }
        Assert.True(startWalkable, $"[{label}] start ({sx},{sy},{sz}) is not walkable per the slow path — bad waypoint");

        var visited = new HashSet<(int x, int y, int z)>();
        var queue = new Queue<(int x, int y, int z)>();
        visited.Add((sx, sy, sz));
        queue.Enqueue((sx, sy, sz));

        var states = 0;
        var fellThrough = 0;
        var disagreements = 0;
        const int maxLog = 12;

        while (queue.Count > 0)
        {
            var (x, y, z) = queue.Dequeue();
            states++;

            var loc = new Point3D(x, y, z);
            var lookup = cache.TryGetMask(map, x, y, (sbyte)z);

            if (!lookup.IsHit)
            {
                if (fellThrough < maxLog)
                {
                    _output.WriteLine($"FELL THROUGH @ ({x},{y},{z}) hitKind={lookup.HitKind}");
                }
                fellThrough++;
            }

            for (var d = 0; d < 8; d++)
            {
                var dir = (Direction)d;
                var slowOk = Movement.Movement.CheckMovement(stub, map, loc, dir, out var nz);

                // Expand the frontier through every legal move (incl. diagonals).
                if (slowOk)
                {
                    var nx = x;
                    var ny = y;
                    Movement.Movement.Offset(dir, ref nx, ref ny);
                    var next = (nx, ny, (int)nz);
                    if (visited.Count < maxStates && visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }

                // Parity on cardinals only (diagonals carry the query-time corner-cut rule).
                if ((d & 1) == 0 && lookup.IsHit)
                {
                    var cacheOk = lookup.IsWalkable(dir);
                    if (cacheOk != slowOk)
                    {
                        if (disagreements < maxLog)
                        {
                            _output.WriteLine($"WALK DIFF @ ({x},{y},{z}) dir={dir} slow={slowOk} cache={cacheOk}");
                        }
                        disagreements++;
                    }
                    else if (slowOk && nz != lookup.GetWalkZ(dir))
                    {
                        if (disagreements < maxLog)
                        {
                            _output.WriteLine($"Z DIFF @ ({x},{y},{z}) dir={dir} slow={nz} cache={lookup.GetWalkZ(dir)}");
                        }
                        disagreements++;
                    }
                }
            }
        }

        stub.Delete();

        var fallthroughPct = states == 0 ? 0 : 100.0 * fellThrough / states;
        _output.WriteLine($"[{label}] states={states} fellThrough={fellThrough} ({fallthroughPct:F2}%) disagreements={disagreements}");

        Assert.True(states > 50, $"[{label}] only explored {states} states — flood-fill stalled, bad waypoint");

        // Correctness is strict: where the cache DOES answer, it must agree with the slow path.
        Assert.Equal(0, disagreements);

        // Coverage: nearly every reachable state should be cache-served. A small residual is
        // expected and acceptable — a walkable surface sitting directly under a bridge/stair
        // ramp falls through to the slow path (correct, just uncached) because the bake's
        // clearance check is intentionally conservative there. A real anchor regression shows
        // up as a large fraction (the pre-fix sewer was ~98%), which this still catches.
        Assert.True(
            fallthroughPct < 1.0,
            $"[{label}] cache fell through on {fallthroughPct:F2}% ({fellThrough}/{states}) of reachable states — coverage regression"
        );
    }

    /// <summary>
    /// Default static walker: inherits straight from Mobile so MovementImpl sees no
    /// BaseCreature flags (CanSwim/CanFly false, bc==null). Mirrors the existing parity stub.
    /// </summary>
    private class ParityStubMobile : Mobile
    {
        public ParityStubMobile()
        {
            Body = 0xC9;
        }
    }
}
