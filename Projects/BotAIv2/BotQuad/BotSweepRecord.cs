using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// What sweeps of each square of the island paid lately: how many sweeps walked it, how many fights they found in it, and what
/// the hunts those finds led to came to. The price a prowl is offered at (<see cref="BotProwl.Expects"/>) is read from here.
///
/// <para>
/// <b>A prowl was offered at a flat eight a minute, and it won the auction for being nothing.</b> Patrick, 06.10.2026 ~10:00:
/// 82 % of the 14 000 prowls a day end "nothing here" and count as finished work; "ideally, when they go on a prowl they are
/// given several quadrants to clear; if they walked them and found nobody, that is fine, that is a success." On build 400
/// (06.10, 06:12–09:42) 2709 prowls were taken, 1761 of the 2669 that ended said "nothing here" (a median of 0.6 minutes),
/// and 274 of them were taken over a real offer. The walk itself pays nothing and never will; what it is worth is what the
/// fights it finds pay, and that is a fact about the squares it walks — so it is measured per square, from the sweeps that
/// walked them, and a square that sweeps keep finding empty prices its sweeps down towards nothing.
/// </para>
///
/// <para>
/// Faded by <see cref="HalfLifeMs"/>: a square cleared an hour ago fills again from its spawner, and a square that paid this
/// morning may be empty now. Not saved: a restart is a fresh look, and the first hour refills it.
/// </para>
/// </summary>
public static class BotSweepRecord
{
    /// <summary>How long it takes half of what a square's sweeps came to to fade. An hour, as the island's own board.</summary>
    public static int HalfLifeMs { get; set; } = 3600000;

    /// <summary>
    /// What a sweep of a square nobody has swept lately is reckoned to bring, in coin. One: a prowl of one square is then
    /// offered at one a minute, which is what the corrections made of the old flat eight on build 400 (the median score of the
    /// 2709 prowls taken on 06.10, 06:12–09:42), so untried ground is offered exactly as before.
    /// </summary>
    public static double SquarePrior { get; set; } = 1.0;

    /// <summary>How many sweeps' worth of evidence the prior counts as. Two: a square swept empty five times reads two sevenths of it.</summary>
    public static double PriorWeight { get; set; } = 2.0;

    /// <summary>
    /// How long after a sweep found a fight the hunt that ends is credited to the square it was found in. Three minutes:
    /// a hunt runs a median of 0.4 minutes, and the hunter is offered it at the next review.
    /// </summary>
    public static int CreditMs { get; set; } = 180000;

    /// <summary>How long the squares a sweep had still to walk wait for the bot to come back from the fight it found. Five minutes.</summary>
    public static int ResumeMs { get; set; } = 300000;

    /// <summary>Most squares remembered at once. Beyond this the faintest are forgotten.</summary>
    public static int MostSquares { get; set; } = 8192;

    private sealed class Entry
    {
        public double Walked;

        public double Found;

        public double Gold;

        public long Tick;

        /// <summary>When a sweep's walk to this square's waypoint was last refused or given up; nought never. See <see cref="Wayless"/>.</summary>
        public long WaylessTick;
    }

    /// <summary>
    /// How long a square whose waypoint a sweep could not walk to is left out of every sweep (07.10.2026, work18). An hour: the
    /// quadrant's own rest (BotQuad.BaulkMs) is ten minutes a refusal, and on build 411 "no way through to (x, y)" was 62 of the
    /// first 76 minutes' 1160 prowl endings, the next dart landing on the square the rest had just let go. Nought: off.
    /// </summary>
    public static int WaylessMs { get; set; } = 3600000;

    /// <summary>Squares filed as wayless after a sweep's walk to them was refused or given up.</summary>
    public static long WaylessFiled { get; private set; }

    /// <summary>A sweep could not walk to this square's waypoint (refused or given up): it is left out of sweeps for <see cref="WaylessMs"/>.</summary>
    public static void Wayless(Map map, Point3D where)
    {
        if (WaylessMs <= 0)
        {
            return;
        }

        var entry = Touch(map, where, true);

        if (entry != null)
        {
            entry.WaylessTick = Core.TickCount;
            WaylessFiled++;
        }
    }

    /// <summary>Whether a sweep's walk to this square failed within <see cref="WaylessMs"/>. One dictionary lookup.</summary>
    public static bool NoWay(Map map, Point3D where)
    {
        if (WaylessMs <= 0 || map == null || map == Map.Internal)
        {
            return false;
        }

        return _squares.TryGetValue(BotQuad.Key(map, where), out var entry) && entry.WaylessTick != 0
                                                                            && Core.TickCount - entry.WaylessTick < WaylessMs;
    }

    private static readonly Dictionary<(int Map, int X, int Y), Entry> _squares = [];

    private static readonly Dictionary<Serial, (Map Map, Point3D Where, long Tick)> _finds = [];

    private static readonly Dictionary<Serial, (Map Map, Point3D[] Legs, long Tick)> _left = [];

    /// <summary>Hunts credited to the square a sweep found them in, and the coin and goods they came to.</summary>
    public static long Credited { get; private set; }

    public static long CreditedWorth { get; private set; }

    public static int Squares => _squares.Count;

    private static Entry Touch(Map map, Point3D where, bool make)
    {
        if (map == null || map == Map.Internal)
        {
            return null;
        }

        var key = BotQuad.Key(map, where);
        var now = Core.TickCount;

        if (!_squares.TryGetValue(key, out var entry))
        {
            if (!make)
            {
                return null;
            }

            if (_squares.Count >= MostSquares)
            {
                Trim(now);
            }

            entry = new Entry { Tick = now };
            _squares[key] = entry;

            return entry;
        }

        Fade(entry, now);

        return entry;
    }

    private static void Fade(Entry entry, long now)
    {
        var age = now - entry.Tick;

        if (age <= 0 || HalfLifeMs <= 0)
        {
            return;
        }

        var keep = Math.Pow(0.5, age / (double)HalfLifeMs);

        entry.Walked *= keep;
        entry.Found *= keep;
        entry.Gold *= keep;
        entry.Tick = now;
    }

    /// <summary>The faintest half forgotten, when the table is full. Rare: a square is a thousand tiles.</summary>
    private static void Trim(long now)
    {
        List<(int Map, int X, int Y)> faint = [];

        foreach (var (key, entry) in _squares)
        {
            Fade(entry, now);

            if (entry.Walked + entry.Found < 0.25 && (entry.WaylessTick == 0 || now - entry.WaylessTick >= WaylessMs))
            {
                faint.Add(key);
            }
        }

        for (var i = 0; i < faint.Count; i++)
        {
            _squares.Remove(faint[i]);
        }

        if (_squares.Count >= MostSquares)
        {
            _squares.Clear();
        }
    }

    /// <summary>A sweep reached this square: walked to its waypoint, or found a fight in it.</summary>
    public static void Walked(Map map, Point3D where)
    {
        var entry = Touch(map, where, true);

        if (entry != null)
        {
            entry.Walked += 1.0;
        }
    }

    /// <summary>A sweep found a fight in this square, and this bot is the one that will fight it.</summary>
    public static void Found(Mobile body, Map map, Point3D where)
    {
        var entry = Touch(map, where, true);

        if (entry == null)
        {
            return;
        }

        entry.Found += 1.0;
        entry.Walked += 1.0;

        if (body != null)
        {
            _finds[body.Serial] = (map, where, Core.TickCount);
        }
    }

    /// <summary>
    /// A hunt this bot was on has ended, coming to <paramref name="worth"/> in coin and goods. Credited to the square a sweep
    /// found a fight in, when it found one for this bot within <see cref="CreditMs"/>. See <c>BotSlay.Drop</c>.
    /// </summary>
    public static void Hunted(Mobile body, int worth)
    {
        if (body == null || !_finds.TryGetValue(body.Serial, out var find))
        {
            return;
        }

        _finds.Remove(body.Serial);

        if (Core.TickCount - find.Tick >= CreditMs || worth <= 0)
        {
            return;
        }

        var entry = Touch(find.Map, find.Where, true);

        if (entry == null)
        {
            return;
        }

        entry.Gold += worth;
        Credited++;
        CreditedWorth += worth;
    }

    /// <summary>
    /// What a sweep of this square has brought lately, in coin and goods, blended with <see cref="SquarePrior"/> by
    /// <see cref="PriorWeight"/>: what the hunts found there came to, over how many sweeps walked it.
    /// </summary>
    public static double Worth(Map map, Point3D where)
    {
        var entry = Touch(map, where, false);

        if (entry == null)
        {
            return SquarePrior;
        }

        return (SquarePrior * PriorWeight + entry.Gold) / (PriorWeight + entry.Walked);
    }

    /// <summary>How many sweeps walked this square lately, faded. For the log.</summary>
    public static double Sweeps(Map map, Point3D where) => Touch(map, where, false)?.Walked ?? 0.0;

    /// <summary>The squares a sweep had still to walk when a fight took the bot off it, kept for it to come back to.</summary>
    public static void Leave(Mobile body, Map map, List<Point3D> legs, int from)
    {
        if (body == null || map == null || legs == null || from >= legs.Count)
        {
            return;
        }

        var left = new Point3D[legs.Count - from];

        for (var i = from; i < legs.Count; i++)
        {
            left[i - from] = legs[i];
        }

        _left[body.Serial] = (map, left, Core.TickCount);
    }

    /// <summary>The squares this bot's last sweep still had to walk, if it left them within <see cref="ResumeMs"/> on this map.</summary>
    public static Point3D[] Left(Mobile body, Map map)
    {
        if (body == null || !_left.TryGetValue(body.Serial, out var left))
        {
            return null;
        }

        if (left.Map != map || Core.TickCount - left.Tick >= ResumeMs)
        {
            _left.Remove(body.Serial);

            return null;
        }

        return left.Legs;
    }

    /// <summary>A sweep was taken: whatever an earlier one left is either being walked now or given up.</summary>
    public static void Taken(Mobile body)
    {
        if (body != null)
        {
            _left.Remove(body.Serial);
        }
    }

    public static string Describe() =>
        $"{_squares.Count} squares with a record of sweeps, {Credited} hunts credited to the square a sweep found them in ({CreditedWorth} coin and goods), {WaylessFiled} squares filed as having no way to their waypoint for {WaylessMs / 60000} minutes";

    public static void Forget()
    {
        _squares.Clear();
        _finds.Clear();
        _left.Clear();
        Credited = 0;
        CreditedWorth = 0;
        WaylessFiled = 0;
    }
}
