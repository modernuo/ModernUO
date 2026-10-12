using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Logging;
using CalcMoves = Server.Movement.Movement;

namespace Server.BotAI.V2;

/// <summary>
/// How far each piece of ground around the population's home lies from it by road, rather than in a straight line.
///
/// <para>
/// <b>A dart knew only the straight line, and the straight line lies about ground behind a river.</b> The hunt throws
/// its darts at ground within five hundred tiles of the asker as the crow flies, and on 15.09.2026 the ground north-west
/// of the banks west of Britain — x below 1140, y between 1050 and 1420 — took one arriving prowl in 44 sessions against
/// some 450 failed ones on that day alone. Asked through the door, the pathfinder found no road into it from the banks at
/// (1161, 1345) and (1242, 1239) at five times its ceiling, and a road of 520 tiles from reachable ground in the
/// south-west at (1006, 1641): the way in runs round by the south, about 1,100 tiles from Britain for a place 440 tiles
/// off. No search the shard pays for walks away from its goal for that long, so every such dart ended at a bank.
/// </para>
///
/// <para>
/// <b>Notes about failed places could not stand in for it.</b> Replayed over 14–15.09, reading the quadrant baulk in the
/// darts would have caught 130 of 1,889 prowl failures and turned away 228 walks that arrived or found a fight; a note of
/// failures without arrivals at 30, 60 or 90 tiles did no better. A sample over an area needs a fact about the area.
/// </para>
///
/// <para>
/// <b>What it is.</b> One flood from home, breadth first, stepping exactly as the planner and the far-side look step — the
/// engine's step masks, both flanks of a diagonal clear, no item standing where the step lands — inside a square of
/// <see cref="Reach"/> tiles each way. Each tile keeps the steps of the shortest road to it from home. Heights are kept
/// apart the way the search keeps them, so a floor above a street is its own place.
/// </para>
///
/// <para>
/// <b>What it can say about a bot standing somewhere else.</b> A walk is not the same both ways — the world allows drops
/// it will not allow back up — so from another start this proves one thing: the road from there to a place is at least
/// the road from home to the place less the road from home to the start. That is the case it was built for. Bots live
/// around home, and the ground that fails them is ground whose road from home runs far past its straight line.
/// </para>
///
/// <para>
/// <b>Paid once per world load, on the game loop, in slices.</b> The map is game state, so no thread may read it; a
/// slice of at most <see cref="SliceMs"/> every <see cref="SliceEveryMs"/> keeps the loop's share small, and until the
/// flood is finished every answer is "unknown" and nothing reading it changes behaviour.
/// </para>
/// </summary>
public static class BotRoads
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRoads));

    /// <summary>Whether the flood is run at all. Off, every answer is "unknown".</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// How far from home the flood goes, each way. The darts land within five hundred tiles of the asker; the rest is
    /// room for a road that bends out past them and back.
    /// </summary>
    public static int Reach { get; set; } = 640;

    /// <summary>The most one slice may spend on the game loop.</summary>
    public static double SliceMs { get; set; } = 8.0;

    /// <summary>How often a slice runs.</summary>
    public static int SliceEveryMs { get; set; } = 100;

    /// <summary>How many expansions pass between two readings of the clock.</summary>
    private const int ClockEvery = 64;

    /// <summary>How far round home the flood looks for somewhere to begin, when home itself will take nobody.</summary>
    private const int StartSweep = 8;

    private static Map _map;

    private static Point3D _home;

    /// <summary>Where the flood started: the population's home as it was when the map was drawn.</summary>
    public static Point3D Home => _home;

    private static int _x0;

    private static int _y0;

    private static int _side;

    /// <summary>Steps from home plus one, per tile of the square; nought for a tile the flood has not reached.</summary>
    private static ushort[] _steps;

    /// <summary>The height each tile was first reached at.</summary>
    private static sbyte[] _heights;

    /// <summary>
    /// Per tile, at the height it was first reached, the steps a body may take from it: the engine's step mask, and a
    /// diagonal only where both its flanks are open. Nought for a tile the flood did not reach. What <see cref="BotChart"/>
    /// is drawn from, so the chart costs no look at the world of its own. Items standing in the way are not in it; the
    /// planner walking each leg steps round them.
    /// </summary>
    private static byte[] _exits;

    /// <summary>A tile reached again at another height band, by cell, with its steps plus one.</summary>
    private static readonly Dictionary<int, ushort> _stacked = [];

    /// <summary>
    /// The ring being expanded and the ring it is making: a tile's index in the square shifted left eight bits, and its
    /// height in the low byte.
    /// </summary>
    private static List<int> _ring = [];

    private static List<int> _next = [];

    private static int _at;

    private static int _depth;

    private static bool _begun;

    private static long _begunTick;

    /// <summary>Whether the flood has finished. Until then every answer is "unknown".</summary>
    public static bool Ready { get; private set; }

    /// <summary>Whether the flood could not begin at all, for want of footing anywhere near home.</summary>
    public static bool Failed { get; private set; }

    /// <summary>Tiles reached, a second height on one tile counted again.</summary>
    public static long Tiles { get; private set; }

    /// <summary>Slices run.</summary>
    public static long Slices { get; private set; }

    /// <summary>The game loop the slices cost, in milliseconds.</summary>
    public static double SpentMs { get; private set; }

    /// <summary>From the first slice to the last, by the wall clock.</summary>
    public static long TookMs { get; private set; }

    /// <summary>Questions asked of the map, and how many it had no answer to.</summary>
    public static long Asked { get; private set; }

    public static long Unknown { get; private set; }

    /// <summary>One slice of the flood. Called on a timer until <see cref="Ready"/> or <see cref="Failed"/>.</summary>
    public static void Slice()
    {
        if (!Running || Ready || Failed)
        {
            return;
        }

        if (!_begun && !Begin())
        {
            return;
        }

        var started = Stopwatch.GetTimestamp();
        var deadline = started + (long)(SliceMs * Stopwatch.Frequency / 1000.0);
        var map = _map;
        var expansions = 0;
        var done = false;

        while (true)
        {
            if (_at >= _ring.Count)
            {
                if (_next.Count == 0)
                {
                    done = true;

                    break;
                }

                (_ring, _next) = (_next, _ring);
                _next.Clear();
                _at = 0;
                _depth++;

                continue;
            }

            if ((++expansions & (ClockEvery - 1)) == 0 && Stopwatch.GetTimestamp() >= deadline)
            {
                break;
            }

            Expand(map, _ring[_at++]);
        }

        Slices++;
        SpentMs += (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

        if (done)
        {
            Finish();
        }
    }

    /// <summary>
    /// Steps of road from home to a tile, or -1 when there is no answer: the flood has not finished, the tile lies outside
    /// its square, or no road inside the square reaches it or the tiles beside it.
    /// </summary>
    /// <summary>
    /// Whether the map has anything to say about this tile: the flood is finished and the tile lies inside its square.
    /// Outside it <see cref="FromHome"/>'s -1 means "not looked at", inside it means "no road" — and a chooser that
    /// refuses ground on -1 alone would refuse the whole island past <see cref="Reach"/>.
    /// </summary>
    public static bool Covers(Map map, int x, int y) =>
        Ready && map != null && map == _map && x >= _x0 && y >= _y0 && x < _x0 + _side && y < _y0 + _side;

    public static int FromHome(Map map, int x, int y)
    {
        Asked++;

        if (!Ready || map == null || map != _map)
        {
            Unknown++;

            return -1;
        }

        var best = int.MaxValue;

        // The tile itself, or the nearest of the tiles beside it and one step more: a dart is settled on a tile, and a
        // tile with a crate on it is still beside the road.
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                var tx = x + dx - _x0;
                var ty = y + dy - _y0;

                if (tx < 0 || ty < 0 || tx >= _side || ty >= _side)
                {
                    continue;
                }

                var steps = _steps[ty * _side + tx];

                if (steps == 0)
                {
                    continue;
                }

                var road = steps - 1 + (dx == 0 && dy == 0 ? 0 : 1);

                if (road < best)
                {
                    best = road;
                }
            }
        }

        if (best == int.MaxValue)
        {
            Unknown++;

            return -1;
        }

        return best;
    }

    /// <summary>
    /// How many steps, at least, the road from <paramref name="from"/> to <paramref name="to"/> runs past the straight line
    /// between them, or -1 when the map cannot say. At least, because from anywhere but home this knows only the road
    /// from home to each end.
    /// </summary>
    public static int Detour(Map map, Point3D from, Point3D to)
    {
        var road = FromHome(map, to.X, to.Y);

        if (road < 0)
        {
            return -1;
        }

        var own = FromHome(map, from.X, from.Y);

        if (own < 0)
        {
            return -1;
        }

        var straight = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));

        return Math.Max(0, road - own - straight);
    }

    /// <summary>
    /// How much further round from home <paramref name="to"/> lies than <paramref name="from"/> — each end's road from home
    /// less its straight line from home, the one taken from the other — or -1 when the map cannot say.
    ///
    /// <para>
    /// <b>The question <see cref="Detour"/> cannot answer from anywhere but home.</b> Detour is a lower bound reckoned
    /// through home, and a bot choosing from beside the river, or from the south, sees no detour to ground beyond it:
    /// on 24.09.2026 (build 209, 11:01–11:37) ten prowls stopped on the bank at (1088–1091, 1617–1644) for destinations
    /// at (928–1025, 1593–1696), whose road from home is 800 to 890 tiles for some 450 in a straight line — ground 350 to
    /// 450 tiles "round", where the bank itself and the town are nought to a few dozen. Two places whose roundness differs
    /// by that much have the river between them whichever side of it the asker stands on; two places on the far side
    /// differ by little, so a bot over there still hunts over there.
    /// </para>
    /// </summary>
    public static int Behind(Map map, Point3D from, Point3D to)
    {
        var road = FromHome(map, to.X, to.Y);
        var own = FromHome(map, from.X, from.Y);

        if (road < 0 || own < 0)
        {
            return -1;
        }

        var toRound = road - Math.Max(Math.Abs(to.X - _home.X), Math.Abs(to.Y - _home.Y));
        var fromRound = own - Math.Max(Math.Abs(from.X - _home.X), Math.Abs(from.Y - _home.Y));

        return Math.Max(0, toRound - fromRound);
    }

    /// <summary>
    /// How much further round by road than each other two places lie — the larger of the two ways <see cref="Behind"/> can
    /// be asked — or nought when the map cannot say. A river between them, whichever side the asker stands on.
    /// </summary>
    public static int Farther(Map map, Point3D a, Point3D b)
    {
        var ra = FromHome(map, a.X, a.Y);
        var rb = FromHome(map, b.X, b.Y);

        if (ra < 0 || rb < 0)
        {
            return 0;
        }

        var roundA = ra - Math.Max(Math.Abs(a.X - _home.X), Math.Abs(a.Y - _home.Y));
        var roundB = rb - Math.Max(Math.Abs(b.X - _home.X), Math.Abs(b.Y - _home.Y));

        return Math.Abs(roundA - roundB);
    }

    public static string Describe()
    {
        if (!Running)
        {
            return "the road map is off";
        }

        if (Failed)
        {
            return $"the road map could not begin: nothing within {StartSweep} tiles of home at {_home} will take a body";
        }

        if (!Ready)
        {
            return _begun
                ? $"the road map is being drawn: {Tiles} tiles reached, {_depth} steps out, {Slices} slices and {SpentMs:F0}ms of the loop so far"
                : "the road map has not begun";
        }

        return $"the road map from {_home}: {Tiles} tiles reached within {(_side - 1) / 2} tiles each way, the farthest {_depth} steps of road, drawn in {Slices} slices and {SpentMs:F0}ms of the loop over {TookMs / 1000.0:F1}s; {Asked} questions asked of it, {Unknown} without an answer";
    }

    /// <summary>Everything back to nothing, for a world that has just been replaced.</summary>
    public static void Forget()
    {
        Ready = false;
        Failed = false;
        _begun = false;
        _map = null;
        _stacked.Clear();
        _ring.Clear();
        _next.Clear();
        _at = 0;
        _depth = 0;
        Tiles = 0;
        Slices = 0;
        SpentMs = 0.0;
        TookMs = 0;
        Asked = 0;
        Unknown = 0;
    }

    private static bool Begin()
    {
        var map = BotPopulation.Home;

        // Not placed yet: asked again at the next slice.
        if (map == null || map == Map.Internal)
        {
            return false;
        }

        _begun = true;
        _begunTick = Core.TickCount;
        _map = map;
        _home = BotPopulation.Where;

        // The navigation graph of the same map, drawn outward from the same home. See NavigationService.
        Server.Engines.Pathing.Tiered.NavigationService.Prepare(map, _home);

        var reach = Math.Clamp(Reach, 16, 2000);

        _side = 2 * reach + 1;
        _x0 = _home.X - reach;
        _y0 = _home.Y - reach;

        var cells = _side * _side;

        if (_steps == null || _steps.Length != cells)
        {
            _steps = new ushort[cells];
            _heights = new sbyte[cells];
            _exits = new byte[cells];
        }
        else
        {
            Array.Clear(_steps);
            Array.Clear(_exits);
        }

        _stacked.Clear();
        _ring.Clear();
        _next.Clear();
        _at = 0;
        _depth = 0;
        Tiles = 0;

        if (!Footing(map, _home, out var x, out var y, out var z))
        {
            Failed = true;

            logger.Warning(
                "Roads: nothing within {Sweep} tiles of home at {Home} on {Map} will take a body, so there is no road map and the darts keep the straight line",
                StartSweep,
                _home,
                map
            );

            return false;
        }

        Mark(x, y, z, 0);
        (_ring, _next) = (_next, _ring);

        return true;
    }

    /// <summary>
    /// Somewhere at or near home a body could stand. Home is a muster point, and a muster point is the one tile on the
    /// island most likely to be refused for being crowded — so it is asked in rings, nearest first, as the far-side look
    /// asks about a destination.
    /// </summary>
    private static bool Footing(Map map, Point3D home, out int x, out int y, out sbyte z)
    {
        var homeZ = (sbyte)Math.Clamp(home.Z, sbyte.MinValue, sbyte.MaxValue);

        for (var r = 0; r <= StartSweep; r++)
        {
            for (var dy = -r; dy <= r; dy++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    x = home.X + dx;
                    y = home.Y + dy;

                    if (!BotStep.OnMap(map, x, y))
                    {
                        continue;
                    }

                    if (BotStep.Mask(map, x, y, homeZ).WalkMask != 0)
                    {
                        z = homeZ;

                        return true;
                    }

                    if (BotStep.Settle(map, x, y, out z) && BotStep.Mask(map, x, y, z).WalkMask != 0)
                    {
                        return true;
                    }
                }
            }
        }

        x = 0;
        y = 0;
        z = 0;

        return false;
    }

    /// <summary>Whether a tile at a height has been reached already, at that height band.</summary>
    private static bool Seen(int index, int x, int y, sbyte z)
    {
        if (_steps[index] == 0)
        {
            return false;
        }

        var cell = BotStep.Cell(x, y, z);

        return cell == BotStep.Cell(x, y, _heights[index]) || _stacked.ContainsKey(cell);
    }

    private static void Mark(int x, int y, sbyte z, int depth)
    {
        var index = (y - _y0) * _side + (x - _x0);
        var steps = (ushort)Math.Min(depth + 1, ushort.MaxValue);

        if (_steps[index] == 0)
        {
            _steps[index] = steps;
            _heights[index] = z;
        }
        else
        {
            _stacked[BotStep.Cell(x, y, z)] = steps;
        }

        Tiles++;
        _next.Add((index << 8) | (z + 128));
    }

    /// <summary>
    /// Every step a body could take from one tile of the ring, by the planner's own rules. See <c>BotPath.Enclose</c>,
    /// whose flood this repeats: a flood stricter than the engine would call open ground far, and one looser would call
    /// the far bank near.
    /// </summary>
    private static void Expand(Map map, int node)
    {
        var index = node >> 8;
        var z = (sbyte)((node & 0xFF) - 128);
        var cx = _x0 + index % _side;
        var cy = _y0 + index / _side;

        var mask = BotStep.Mask(map, cx, cy, z);
        var walk = mask.WalkMask;

        if (walk == 0)
        {
            return;
        }

        // The tile's ways out, for the chart, at the height the tile was first reached: a diagonal only with both flanks.
        if (z == _heights[index])
        {
            var exits = (int)walk;

            for (var d = 1; d < 8; d += 2)
            {
                if ((walk & (1 << ((d + 7) & 7))) == 0 || (walk & (1 << ((d + 1) & 7))) == 0)
                {
                    exits &= ~(1 << d);
                }
            }

            _exits[index] = (byte)exits;
        }

        for (var d = 0; d < 8; d++)
        {
            if ((walk & (1 << d)) == 0)
            {
                continue;
            }

            var nx = cx;
            var ny = cy;

            CalcMoves.Offset((Direction)d, ref nx, ref ny);

            if (nx < _x0 || ny < _y0 || nx >= _x0 + _side || ny >= _y0 + _side || nx < 0 || ny < 0 || nx >= map.Width
                || ny >= map.Height)
            {
                continue;
            }

            var nz = mask.GetWalkZ((Direction)d);

            // Asked before the items, which cost a look at the tile: most steps in a flood land on ground already reached.
            if (Seen((ny - _y0) * _side + (nx - _x0), nx, ny, nz))
            {
                continue;
            }

            if ((d & 1) == 1)
            {
                var left = (d + 7) & 7;
                var right = (d + 1) & 7;

                if ((walk & (1 << left)) == 0 || (walk & (1 << right)) == 0)
                {
                    continue;
                }

                if (BotStep.FlankBlocked(map, cx, cy, left, mask) || BotStep.FlankBlocked(map, cx, cy, right, mask))
                {
                    continue;
                }
            }

            if (BotStep.BlockedByItems(map, nx, ny, nz))
            {
                continue;
            }

            Mark(nx, ny, nz, _depth + 1);
        }
    }

    private static void Finish()
    {
        Ready = true;
        TookMs = Core.TickCount - _begunTick;

        _ring.Clear();
        _next.Clear();

        logger.Information(
            "Roads: {Tiles} tiles reached from home at {Home} within {Reach} tiles each way, the farthest {Deepest} steps of road; {Slices} slices, {Ms:F0}ms of the loop over {Seconds:F1}s",
            Tiles,
            _home,
            (_side - 1) / 2,
            _depth,
            Slices,
            SpentMs,
            TookMs / 1000.0
        );

        // Every guild's seat asked of the finished map. See BotSeat.Audit.
        BotSeat.Audit();

        // And the coarse chart drawn from it, on the same timer. See BotChart.
        BotChart.Begin();
    }

    /// <summary>
    /// The finished flood, lent to <see cref="BotChart"/>: the map, the square's corner and side, each tile's ways out and
    /// the height it was reached at. False until the flood is finished.
    /// </summary>
    public static bool Lend(out Map map, out int x0, out int y0, out int side, out byte[] exits, out sbyte[] heights)
    {
        map = _map;
        x0 = _x0;
        y0 = _y0;
        side = _side;
        exits = _exits;
        heights = _heights;

        return Ready && _exits != null;
    }
}
