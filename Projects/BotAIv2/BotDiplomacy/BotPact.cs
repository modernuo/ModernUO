using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What two guilds have agreed before a witness, and the three places on the shard that each agreement changes.
///
/// <para>
/// <b>Patrick's order of 29.09.2026, point 2: guilds agree "an alliance, orders, trade and safety for each other" the same
/// way they declare war — at a meeting.</b> Each agreement here is kept small and made to change one real thing rather than
/// named and left to be decorative:
/// </para>
///
/// <list type="bullet">
/// <item><b>Peace</b> (safety): neither may declare war on the other while it stands. Enforced by the war ledger's own truce
/// (<c>BotWar.RestoreTruce</c>), which <c>BotWar.MayDeclare</c> already refuses on and counts — one rule, one place.</item>
/// <item><b>Trade</b>: a member of one buys at the other's shops by the bank at <see cref="TradeDiscount"/> below the asking
/// price, and sees that price when it weighs the shop (<c>BotShopkeep.PriceFor</c>, the one place a shop's price is read). The
/// keeper is paid less by exactly what the buyer saves, so no coin is created: the terms move trade between the two, they do not
/// mint it.</item>
/// <item><b>Orders</b>: each side's carriers answer the other's wants before anybody else's of the same kind
/// (<c>BotMarket.Demand</c>, <c>BotCourier</c>) — the price is the buyer's as ever; only who is served first changes.</item>
/// </list>
///
/// <para>
/// An alliance is the engine's own (<c>Guild.AddAlly</c>) and is kept by the engine; its consequences — allies join each
/// other's wars and read as allies to notoriety — are <c>BotWar.Declare</c>'s and the engine's, and are not repeated here.
/// </para>
///
/// <para>
/// <b>Clocks are durations of the shard running</b>, saved as time left like the war ledger's truces
/// (<see cref="BotDiplomacyStore"/>): an agreement of twelve hours is twelve hours of the shard up, not of the wall.
/// </para>
/// </summary>
public static class BotPact
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPact));

    /// <summary>The kinds of agreement a meeting can end in besides war and nothing.</summary>
    public enum Kind
    {
        Peace,
        Trade,
        Orders
    }

    /// <summary>How long an agreement stands, in hours, once sworn. Twelve: an evening and a night of the shard.</summary>
    public static double Hours { get; set; } = 12.0;

    /// <summary>
    /// What trade terms take off a stall's asking price between the two guilds. A tenth: enough to turn a buyer from an
    /// equal stall to a partner's, and to matter against a counter; small enough that a partner's stall at twice the market
    /// is still passed over.
    /// </summary>
    public static double TradeDiscount { get; set; } = 0.10;

    /// <summary>Agreements sworn, by kind.</summary>
    public static long Peaces { get; private set; }

    public static long Trades { get; private set; }

    public static long Orders { get; private set; }

    /// <summary>Purchases off a partner's stall at the terms, and the gold the buyers kept by them.</summary>
    public static long Discounted { get; private set; }

    public static long Saved { get; private set; }

    /// <summary>Orders filled for a guild under standing orders with the filler's: served first by <c>BotMarket.Demand</c>.</summary>
    public static long Favoured { get; private set; }

    /// <summary>
    /// One pair's agreements: when each kind runs out, and whether it was ever sworn. A flag rather than a tick of nought for
    /// "never", because a tick can be anything on a host whose counter never resets (CLAUDE.md, rule 20).
    /// </summary>
    private sealed class Terms
    {
        public readonly long[] Ends = new long[3];

        public readonly bool[] Sworn = new bool[3];

        public bool Stands(int kind, long now) => Sworn[kind] && now - Ends[kind] < 0;
    }

    private static readonly Dictionary<(string A, string B), Terms> _pacts = [];

    private static (string A, string B) Pair(string one, string other) =>
        string.CompareOrdinal(one, other) <= 0 ? (one, other) : (other, one);

    /// <summary>
    /// Swears an agreement between two guilds for <paramref name="ms"/>. Peace also stands up the war ledger's truce, which
    /// is what actually refuses a declaration.
    /// </summary>
    public static void Swear(string one, string other, Kind kind, long ms)
    {
        if (one == null || other == null || one == other || ms <= 0)
        {
            return;
        }

        var pair = Pair(one, other);

        if (!_pacts.TryGetValue(pair, out var terms))
        {
            _pacts[pair] = terms = new Terms();
        }

        var now = Core.TickCount;
        var ends = now + ms;

        // Never shortened by a second oath: an agreement sworn again runs from now or from when it would have run out,
        // whichever is later.
        if (!terms.Stands((int)kind, now) || ends - terms.Ends[(int)kind] > 0)
        {
            terms.Ends[(int)kind] = ends;
            terms.Sworn[(int)kind] = true;
        }

        switch (kind)
        {
            case Kind.Peace:
                {
                    Peaces++;
                    BotWar.RestoreTruce(one, other, terms.Ends[(int)Kind.Peace] - now);

                    break;
                }
            case Kind.Trade:
                {
                    Trades++;

                    break;
                }
            default:
                {
                    Orders++;

                    break;
                }
        }

        logger.Information("{One} and {Other} have sworn {Kind} for {Hours:0.#} hours", one, other, Word(kind), ms / 3600000.0);
    }

    /// <summary>Whether these two stand under this kind of agreement now.</summary>
    public static bool Holds(string one, string other, Kind kind)
    {
        if (_pacts.Count == 0 || one == null || other == null || one == other)
        {
            return false;
        }

        return _pacts.TryGetValue(Pair(one, other), out var terms) && terms.Stands((int)kind, Core.TickCount);
    }

    /// <summary>Agreements broken by a seizure (<c>BotSeizure</c>): one count per pair, whatever kinds it held.</summary>
    public static long Broken { get; private set; }

    /// <summary>
    /// Every agreement between these two let go at once, because one of them laid claim to the other's ground (Patrick,
    /// 06.10.2026: war without negotiations). Returns what stood, as words ("trade terms and peace"), or null when nothing did.
    /// The truce a peace stood up in the war ledger is the caller's to break (<c>BotWar.Untruce</c>).
    /// </summary>
    public static string Break(string one, string other)
    {
        if (_pacts.Count == 0 || one == null || other == null || one == other)
        {
            return null;
        }

        var pair = Pair(one, other);

        if (!_pacts.TryGetValue(pair, out var terms))
        {
            return null;
        }

        var now = Core.TickCount;
        string said = null;

        for (var k = 0; k < terms.Ends.Length; k++)
        {
            if (terms.Stands(k, now))
            {
                said = said == null ? Word((Kind)k) : $"{said} and {Word((Kind)k)}";
            }
        }

        _pacts.Remove(pair);

        if (said != null)
        {
            Broken++;
        }

        return said;
    }

    /// <summary>Minutes an agreement has left, or nought.</summary>
    public static int MinutesLeft(string one, string other, Kind kind)
    {
        if (!Holds(one, other, kind))
        {
            return 0;
        }

        return (int)((_pacts[Pair(one, other)].Ends[(int)kind] - Core.TickCount) / 60000);
    }

    /// <summary>Whether these two stand under any agreement.</summary>
    public static bool Any(string one, string other) =>
        Holds(one, other, Kind.Peace) || Holds(one, other, Kind.Trade) || Holds(one, other, Kind.Orders);

    private static string GuildOf(Mobile m) => (m?.Guild as Guild)?.Name;

    /// <summary>
    /// What one of a shop's units costs this buyer: the asking price, or less by the trade terms when the buyer's guild and the
    /// keeper's have them. Never below one. Asked twice by the shops — choosing and paying — so the price a buyer chose by is the
    /// price it pays.
    /// </summary>
    public static int Price(Mobile buyer, Mobile seller, int asking)
    {
        if (_pacts.Count == 0 || asking <= 1 || TradeDiscount <= 0.0)
        {
            return asking;
        }

        var ours = GuildOf(buyer);
        var theirs = GuildOf(seller);

        if (ours == null || theirs == null || ours == theirs || !Holds(ours, theirs, Kind.Trade))
        {
            return asking;
        }

        return Math.Max(1, (int)Math.Round(asking * (1.0 - Math.Clamp(TradeDiscount, 0.0, 0.5))));
    }

    /// <summary>A purchase was made at the terms: counted, and what the buyer kept by them.</summary>
    public static void Bought(int asking, int paid, int units)
    {
        if (units <= 0 || paid >= asking)
        {
            return;
        }

        Discounted++;
        Saved += (long)(asking - paid) * units;
    }

    /// <summary>
    /// Whether this supplier's guild has standing orders with this buyer's: its orders are then filled first. Asked by
    /// <c>BotMarket.Demand</c> of each want of the kind; answers false at once when no agreement stands anywhere.
    /// </summary>
    public static bool Serves(Mobile supplier, Mobile buyer)
    {
        if (_pacts.Count == 0)
        {
            return false;
        }

        var ours = GuildOf(supplier);
        var theirs = GuildOf(buyer);

        return ours != null && theirs != null && ours != theirs && Holds(ours, theirs, Kind.Orders);
    }

    /// <summary>A want was answered; counted when the carrier's guild and the buyer's stand under standing orders. From <c>BotDeliver</c>.</summary>
    public static void Filled(Mobile supplier, Mobile buyer)
    {
        if (Serves(supplier, buyer))
        {
            Favoured++;
        }
    }

    public static string Word(Kind kind) =>
        kind switch
        {
            Kind.Peace => "peace",
            Kind.Trade => "trade terms",
            _ => "standing orders"
        };

    /// <summary>The agreements standing, one clause each.</summary>
    public static string Tell()
    {
        using var say = Server.Text.ValueStringBuilder.Create(128);

        var now = Core.TickCount;

        foreach (var (pair, terms) in _pacts)
        {
            for (var k = 0; k < terms.Ends.Length; k++)
            {
                if (!terms.Stands(k, now))
                {
                    continue;
                }

                var ends = terms.Ends[k];

                if (say.Length > 0)
                {
                    say.Append(", ");
                }

                say.Append($"{pair.A} and {pair.B} under {Word((Kind)k)} for {(ends - now) / 60000} min more");
            }
        }

        return say.Length == 0 ? "no agreements stand" : say.ToString();
    }

    /// <summary>Every agreement, for the store and the page: the pair, the kind and the milliseconds left.</summary>
    public static IEnumerable<(string A, string B, Kind Kind, long LeftMs)> Standing()
    {
        var now = Core.TickCount;

        foreach (var (pair, terms) in _pacts)
        {
            for (var k = 0; k < terms.Ends.Length; k++)
            {
                if (terms.Stands(k, now))
                {
                    yield return (pair.A, pair.B, (Kind)k, terms.Ends[k] - now);
                }
            }
        }
    }

    /// <summary>An agreement read back off the save, with <paramref name="leftMs"/> still to run. Nothing is counted or said.</summary>
    public static void Restore(string a, string b, Kind kind, long leftMs)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b || leftMs <= 0)
        {
            return;
        }

        var pair = Pair(a, b);

        if (!_pacts.TryGetValue(pair, out var terms))
        {
            _pacts[pair] = terms = new Terms();
        }

        terms.Ends[(int)kind] = Core.TickCount + leftMs;
        terms.Sworn[(int)kind] = true;
    }

    /// <summary>Agreements whose time is up are let go, so the table stays the size of what stands. Once a look.</summary>
    public static void Sweep()
    {
        if (_pacts.Count == 0)
        {
            return;
        }

        List<(string, string)> gone = null;
        var now = Core.TickCount;

        foreach (var (pair, terms) in _pacts)
        {
            var alive = false;

            for (var k = 0; k < terms.Ends.Length; k++)
            {
                alive |= terms.Stands(k, now);
            }

            if (!alive)
            {
                (gone ??= []).Add(pair);
            }
        }

        if (gone == null)
        {
            return;
        }

        for (var i = 0; i < gone.Count; i++)
        {
            _pacts.Remove(gone[i]);
        }
    }

    public static string Describe() =>
        $"{Tell()}; {Peaces} peaces, {Trades} trade terms and {Orders} standing orders sworn for {Hours:0.#}h each; {Discounted} purchases off a partner's stall at {TradeDiscount:P0} off, {Saved}gp kept by the buyers; {Favoured} orders filled for a partner under standing orders; {Broken} pairs' agreements broken by a seizure";

    public static void Forget()
    {
        Broken = 0;
        Peaces = 0;
        Trades = 0;
        Orders = 0;
        Discounted = 0;
        Saved = 0;
        Favoured = 0;
    }

    /// <summary>Every agreement let go, for Patrick's reset.</summary>
    public static int Wipe()
    {
        var gone = _pacts.Count;

        _pacts.Clear();
        Forget();

        return gone;
    }
}
