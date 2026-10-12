using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Places the population could not get to, remembered for everybody and forgotten again when somebody
/// proves otherwise.
///
/// <para>
/// <b>Why this exists, in the numbers that produced it.</b> Over the night of 07–08.09.2026 the shard spent
/// two thirds of every piece of work it started on errands that ended in "no way through": 20968 failures
/// out of 29666 undertakings in one session, 5103 of 5568 failures in another. Five destinations alone
/// accounted for 232 of them; herb gathering failed 2940 times out of 2990; a single scout errand failed 374
/// times out of 383. Each of those failures paid for a full path search first — 43139 searches, 1.6 billion
/// tiles, 588 seconds of the game loop in under two hours.
/// </para>
///
/// <para>
/// <b>And none of it was written down anywhere a chooser could read.</b> The shard already had three records
/// about ground and not one of them fit: <see cref="BotReach"/> learns pockets, but only ones small enough
/// to walk the edges of — a patch of grass across water is part of the mainland, the probe returns TooBig,
/// and nothing is filed (five pockets learned in four hours). <see cref="BotQuad"/>'s baulks are shaped like
/// quadrants, thirty tiles across, and until this same night only the hunt read them. And
/// <see cref="BotLedger"/>'s caution is per-bot, so forty-nine bots each discover the same wall separately.
/// </para>
///
/// <para>
/// <b>What this is instead: the destination itself, shared, and disprovable.</b> One entry per
/// <see cref="Grain"/>-sized square of ground, holding how many times somebody gave up trying to reach it
/// and when. The rest doubles with each refusal up to a ceiling, so a place that turned one bot away on a
/// bad afternoon is tried again in minutes, while a rooftop that has turned away nine is left alone for
/// hours. <b>And arriving anywhere clears it</b> — which is the property the baulk map lacked, and the
/// reason its marks grew to cover a quarter of the island in ninety minutes with nothing able to take one
/// back.
/// </para>
///
/// <para>
/// It says nothing about what work is worth. That is the ledger's business, and the night's other repair
/// was to stop a refused road from speaking on that subject at all.
/// </para>
/// </summary>
public static class BotRefused
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRefused));

    /// <summary>
    /// How wide a square one entry covers.
    ///
    /// Eight tiles: wide enough that a bot which failed to reach a doorway does not have to fail again two
    /// tiles along, narrow enough that refusing one does not refuse a district. The ledger bands ground at
    /// 64 and the quadrant map at 30; both proved too coarse to point at a place, which is what this is for.
    /// </summary>
    public static int Grain { get; set; } = 8;

    /// <summary>How long one refusal rests a square. Doubles per refusal, up to <see cref="MostRestMs"/>.</summary>
    public static int RestMs { get; set; } = 120000;

    /// <summary>The ceiling on that doubling — two hours, which is longer than any errand's patience.</summary>
    public static int MostRestMs { get; set; } = 7200000;

    /// <summary>Most squares remembered at once. Oldest untouched go first when it is full.</summary>
    public static int MostPlaces { get; set; } = 4096;

    /// <summary>Refusals written.</summary>
    public static long Marked { get; private set; }

    /// <summary>Times a chooser was told to leave a square alone.</summary>
    public static long Skipped { get; private set; }

    /// <summary>Squares cleared because somebody arrived in them after all.</summary>
    public static long Disproved { get; private set; }

    /// <summary>Squares dropped to keep the record inside <see cref="MostPlaces"/>.</summary>
    public static long Forgotten { get; private set; }

    private sealed class Row
    {
        public int Refusals;

        public long MarkedTick;

        public long TouchedTick;
    }

    private static readonly Dictionary<(int Map, int X, int Y), Row> _rows = [];

    private static (int Map, int X, int Y) Key(Map map, Point3D where) =>
        (map?.MapID ?? -1, where.X / Grain, where.Y / Grain);

    /// <summary>
    /// Somebody gave up trying to reach here. Written by whoever gives up — the walker when a road refuses,
    /// the stall watch when it takes an errand away — and by nobody else.
    /// </summary>
    public static void Refuse(Map map, Point3D where)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        var now = Core.TickCount;
        var key = Key(map, where);

        if (!_rows.TryGetValue(key, out var row))
        {
            Sweep(now);

            row = new Row();
            _rows[key] = row;
        }

        if (row.Refusals < 30)
        {
            row.Refusals++;
        }

        row.MarkedTick = now;
        row.TouchedTick = now;

        Marked++;
    }

    /// <summary>
    /// Whether this ground is resting after refusing somebody. Asked by every sampler that picks a place to
    /// walk to, before it picks.
    /// </summary>
    public static bool Refusing(Map map, Point3D where)
    {
        // Ground barred outright comes first, and it is deliberately not counted as a refusal: a bar is a
        // statement, a refusal is a measurement, and adding the two together would put a number nobody
        // measured into Skipped. See BotBarred, which keeps its own.
        if (BotBarred.Barred(map, where))
        {
            return true;
        }

        if (map == null || _rows.Count == 0)
        {
            return false;
        }

        if (!_rows.TryGetValue(Key(map, where), out var row) || row.Refusals <= 0)
        {
            return false;
        }

        var rest = Math.Min((long)RestMs << Math.Min(row.Refusals - 1, 20), MostRestMs);

        if (Core.TickCount - row.MarkedTick >= rest)
        {
            return false;
        }

        row.TouchedTick = Core.TickCount;
        Skipped++;

        return true;
    }

    /// <summary>
    /// Somebody arrived here, so whatever this square refused before, it does not refuse now.
    ///
    /// <b>This is the half the quadrant baulks never had.</b> A record that only ever accumulates will, given
    /// hours, refuse the whole map — the baulk map reached a quarter of the island in ninety minutes on the
    /// night this was written, and nothing existed that could take one back.
    /// </summary>
    public static void Arrived(Map map, Point3D where)
    {
        if (map == null || _rows.Count == 0)
        {
            return;
        }

        if (_rows.Remove(Key(map, where)))
        {
            Disproved++;
        }
    }

    /// <summary>Drops the least recently touched squares when the record is full.</summary>
    private static void Sweep(long now)
    {
        if (_rows.Count < MostPlaces)
        {
            return;
        }

        var oldestKey = default((int Map, int X, int Y));
        var oldest = long.MaxValue;

        foreach (var (key, row) in _rows)
        {
            if (row.TouchedTick < oldest)
            {
                oldest = row.TouchedTick;
                oldestKey = key;
            }
        }

        if (_rows.Remove(oldestKey))
        {
            Forgotten++;
        }
    }

    /// <summary>One line for the shard's own summary.</summary>
    public static string Describe() =>
        Marked == 0
            ? $"nowhere has refused anybody yet; {BotBarred.Describe()}"
            : $"{_rows.Count} squares of {Grain} tiles are resting after refusing somebody: {Marked} refusals written, {Skipped} choices steered away from them, {Disproved} cleared by somebody arriving after all, {Forgotten} dropped to stay inside {MostPlaces}; {BotBarred.Describe()}";

    /// <summary>A world reload is a different world.</summary>
    public static void Forget()
    {
        _rows.Clear();
        Marked = 0;
        Skipped = 0;
        Disproved = 0;
        Forgotten = 0;

        // Its counters, not its list: see BotBarred.Forget.
        BotBarred.Forget();
    }
}
