using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// Where walks have stopped getting any nearer, and which way they were heading, so the hunt stops throwing darts past
/// those places.
///
/// <para>
/// <b>The ground between a bot and its dart is what refuses, and nothing remembered the ground between.</b> A prowl
/// that stops closing baulks its destination square (see <c>BotQuad.Baulk</c>), but the next dart lands on a fresh
/// square beyond the same river, and a note about a place is no answer to a sample over an area. On build 42 "got no
/// nearer" was the largest failure group on the shard, 99 in 45 minutes. Asked through the door, five of the six latest
/// destinations answered Partial with plans ending at the same few places west of Britain — (1161, 1345), (1088, 1628)
/// and (1229, 1247) — and build 43's instrument put the bots' own stopping tiles on exactly those places: three at
/// (1160, 1340), four at (1090, 1640), four more at (1390–1410, 1730–1740) with their destinations to the east.
/// </para>
///
/// <para>
/// So what is kept is the stop and the heading, not the destination. A candidate is passed over when the straight line
/// to it runs close by a place where walks heading roughly the same way have stopped at least <see cref="Enough"/> times
/// lately, and the candidate lies well beyond that place. More than one stop, because one walk can be held up by a
/// fight or a crowd; forgotten after <see cref="KeepMs"/>, because a note nothing can disprove must not last for ever;
/// lost at a restart, like the shard's other notes about ground.
/// </para>
/// </summary>
public static class BotBarrier
{
    /// <summary>
    /// Whether the hunt reads this at all. Off, darts are thrown exactly as they were before it existed.
    ///
    /// <para>
    /// <b>Off, because replayed it caught as many good walks as bad ones.</b> Over build 43's first seventeen minutes,
    /// judging every prowl taken from home against the stops recorded before it, the rule as written would have passed
    /// over 2 of the 23 walks that stopped closing — and 2 of the 189 that finished; wider or looser settings caught 2
    /// to 4 against 3 to 4. A river is a line hundreds of tiles long and these are points on it: a dart that crosses the
    /// same river somewhere else is not near any of them. The stops are still written down, so the line can be drawn
    /// from them later; switch this on through the door to try it.
    /// </para>
    /// </summary>
    public static bool Running { get; set; } = false;

    /// <summary>How close to the line to a candidate a stopping place must lie to count against it, in tiles.</summary>
    public static int Near { get; set; } = 16;

    /// <summary>Stops near one place, heading the same way, before that place counts as a barrier.</summary>
    public static int Enough { get; set; } = 2;

    /// <summary>How long a stop is remembered. Forty minutes.</summary>
    public static int KeepMs { get; set; } = 2400000;

    /// <summary>The most stops kept at once; the oldest goes first.</summary>
    public static int Most { get; set; } = 128;

    /// <summary>
    /// How closely two headings must agree to be the same way: the cosine of the angle between them. About 45 degrees.
    /// </summary>
    public static double SameWay { get; set; } = 0.7;

    /// <summary>Walks written down as having stopped closing.</summary>
    public static long Stops { get; private set; }

    /// <summary>Hunting grounds passed over for lying beyond a place walks that way have stopped at.</summary>
    public static long Behind { get; private set; }

    private readonly record struct Stop(Map Map, int X, int Y, double Dx, double Dy, long Tick);

    private static readonly List<Stop> _stops = [];

    /// <summary>A walk from <paramref name="at"/> towards <paramref name="toward"/> stopped getting any nearer.</summary>
    public static void Stopped(Map map, Point3D at, Point3D toward)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        var dx = toward.X - at.X;
        var dy = toward.Y - at.Y;
        var length = Math.Sqrt((double)dx * dx + (double)dy * dy);

        if (length < 1.0)
        {
            return;
        }

        if (_stops.Count >= Math.Max(1, Most))
        {
            _stops.RemoveAt(0);
        }

        _stops.Add(new Stop(map, at.X, at.Y, dx / length, dy / length, Core.TickCount));
        Stops++;
    }

    /// <summary>
    /// Whether a walk from <paramref name="from"/> to <paramref name="to"/> would run past a place walks heading that way
    /// have stopped at, with <paramref name="to"/> lying beyond it.
    /// </summary>
    public static bool Beyond(Map map, Point3D from, Point3D to)
    {
        if (!Running || map == null || _stops.Count == 0)
        {
            return false;
        }

        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Sqrt((double)dx * dx + (double)dy * dy);

        if (length <= Near)
        {
            return false;
        }

        var ux = dx / length;
        var uy = dy / length;
        var now = Core.TickCount;

        for (var i = 0; i < _stops.Count; i++)
        {
            var stop = _stops[i];

            if (stop.Map != map || now - stop.Tick > KeepMs || stop.Dx * ux + stop.Dy * uy < SameWay)
            {
                continue;
            }

            // Along the line: between the start and the candidate, with the candidate well past it.
            var along = (stop.X - from.X) * ux + (stop.Y - from.Y) * uy;

            if (along <= 0.0 || along >= length - Near)
            {
                continue;
            }

            // Across the line: close enough to be on the way.
            var across = Math.Abs((stop.X - from.X) * uy - (stop.Y - from.Y) * ux);

            if (across > Near || Neighbours(stop, now) < Enough)
            {
                continue;
            }

            Behind++;

            return true;
        }

        return false;
    }

    /// <summary>Stops within <see cref="Near"/> of this one, heading the same way, itself included.</summary>
    private static int Neighbours(Stop stop, long now)
    {
        var found = 0;

        for (var i = 0; i < _stops.Count; i++)
        {
            var other = _stops[i];

            if (other.Map != stop.Map || now - other.Tick > KeepMs || other.Dx * stop.Dx + other.Dy * stop.Dy < SameWay)
            {
                continue;
            }

            if (Math.Abs(other.X - stop.X) <= Near && Math.Abs(other.Y - stop.Y) <= Near)
            {
                found++;
            }
        }

        return found;
    }

    /// <summary>Everything back to nothing, for a world that has just been replaced.</summary>
    public static void Forget()
    {
        _stops.Clear();
        Stops = 0;
        Behind = 0;
    }
}
