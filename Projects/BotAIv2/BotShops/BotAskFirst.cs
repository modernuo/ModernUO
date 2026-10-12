using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A restock of a supply some bot near holds over its own keep, asked of the island at the counter's price before the walk.
///
/// <para>
/// <b>Patrick, 03.10.2026, night: "the focus is 99 % fulfilment of Needs; we have to bring the economy to life."</b> Measured by
/// the analyst on build 395 (<c>flow9.md</c>, 210 bots): five consumable families were 64 % of the gold bots paid to shopkeepers
/// and 9 % of the gold on the bots' board; the shopper named 5,155 restock errands in 40 minutes, 90.9 % to a counter, 7.4 % to
/// a bot's shop and 1.7 % to a want. On the same log (03:08–04:19) the restocks bought 1,580 cloth (3,703gp), 1,406 arrows
/// (3,438gp), 404 bandages (2,333gp), 70 pickaxes (1,588gp) and 856 reagents (3,288gp) over a counter, while herb pickers came
/// back with their herbs "kept to sell", fletchers kept arrows and tinkers kept tools. The two roads that already asked the
/// island first did not reach them: the far ask (<c>BotShopper.AskFarMinutes</c>) needs a four-minute walk and a thing the
/// smith, tailor, alchemist, fletcher or scribe makes, so no cloth, bandage, reagent or tool; the pile ask
/// (<c>BotShopper.PileMs</c>) needs an island-wide glut and leaves out arrows and bolts. Lines for goods a bot already held
/// were met 97.5 % of the time; carriers took a want for leather or feathers — goods hunters carry — at a median of half a
/// minute, 81 to 100 % of them inside three.
/// </para>
///
/// <para>
/// <b>What this does.</b> Before the walk to a counter for cloth or bandages, a reagent, arrows or bolts, or a tool, the
/// shopper asks here whether a bot within a carrier's reach (<see cref="BotCourier.Reach"/>, on the same land) holds that
/// kind over what it keeps for itself (<see cref="BotUnload.Keeps"/>) — read off the population every <see cref="HoldersMs"/>,
/// the way the carrier itself reads a pack, less what other buyers near already ask for. Only then is a want said
/// (<see cref="BotMarket.Ask"/>) at the price the counter would charge this bot (<c>BotBurgh.Dear</c> included) for no more
/// than the largest single holder holds, so one carrier can meet it; and the bot goes on with its work and its other supplies
/// (<see cref="Parked"/>: the shopper passes the waiting supply over and looks at the next). A carrier who takes it walks it
/// over (<see cref="BotCourier"/>, <see cref="BotDeliver"/>) and is paid at the meeting; nothing here pays anybody. If nobody
/// has taken it at <see cref="WaitMs"/>, the want is taken back by the existing road (<see cref="BotMarket.Withdrawn"/>,
/// <see cref="BotWithdraw.Walked"/>) and the bot walks to the counter as it would have. A carrier already bringing it is always
/// waited for: taking the want back would fail the carrier's walk, and the bot's other supplies are not held back meanwhile.
/// </para>
///
/// <para>
/// <b>The Needs share.</b> Nothing about what counts changes. A line taken back inside its first ten minutes is kept out by the
/// rule build 391 wrote (<see cref="BotLines.GraceMs"/>), and every take-back here is that road, swept on a clock
/// (<see cref="SweepMs"/>) rather than at the buyer's next look, so a buyer who died, rested or joined a company is not left
/// holding a want past its tenth minute. A take-back past it — a carrier who took the walk and gave up late — is a failed line,
/// and is counted here as well (<c>past their line's first 10 min</c>).
/// </para>
///
/// <para>
/// <b>Never for an urgent need</b> (<see cref="Urgent"/>): a bot under <see cref="HurtShare"/> of its health, one in a fight
/// (struck lately, or with a foe set), one out of any supply it fights or mends with — bandages, a bottle its class carries, its
/// own arrows, a reagent its key spell burns: the "no bottle at death's door" of <c>BotMobile.Gasp</c> — a medic below the
/// threshold of bandages the doors ask (<see cref="BotProvision.Floor"/>), and a bot below its entry threshold
/// (<see cref="BotProvision.Short"/>: "below the entry threshold, supplies are the errand that wins", so companies and delves do
/// not wait on it). Those walk as before. Nor while the bot already has a restock in hand or put down (it is on the walk), for
/// the band's order, the bot's own weapon, a potion (alchemy's own question), or a bot whose purse cannot carry the compulsory
/// part of the restock (<c>BotShopper.ByPurse</c>): that bot walks, and its guild pays below the threshold as it always has. The
/// want's money is reserved like any other (build 391), so who can afford what does not change.
/// </para>
/// </summary>
public static class BotAskFirst
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotAskFirst));

    /// <summary>Whether a restock asks the island first at all. Off, nothing new is asked; asks standing are still seen through.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How long a bot waits for a carrier before it takes the want back and walks. Three minutes: flow9's proposal.</summary>
    public static int WaitMs { get; set; } = 180000;

    /// <summary>How long after an ask the same bot does not ask the island again for the same supply; it walks instead.</summary>
    public static int AgainMs { get; set; } = 540000;

    /// <summary>How long one reading of who holds what is kept before the population is read again.</summary>
    public static int HoldersMs { get; set; } = 15000;

    /// <summary>How often the asks standing are looked over for a wait that is over, whether or not their buyer looks again.</summary>
    public static int SweepMs { get; set; } = 5000;

    /// <summary>The share of its health under which a bot is hurt, and walks for its supplies as before.</summary>
    public static double HurtShare { get; set; } = 0.5;

    // ---- The families ---------------------------------------------------------------------------------------------------

    private const int Families = 4;

    private static readonly string[] _familyNames = ["cloth and bandages", "reagents", "arrows and bolts", "tools"];

    /// <summary>The family a supply belongs to here, or -1: cloth and bandages, reagents, arrows and bolts, the tools a class carries.</summary>
    private static int Family(Type kind)
    {
        if (kind == null)
        {
            return -1;
        }

        if (kind == typeof(Bandage) || kind == typeof(Cloth))
        {
            return 0;
        }

        if (typeof(BaseReagent).IsAssignableFrom(kind))
        {
            return 1;
        }

        if (kind == typeof(Arrow) || kind == typeof(Bolt))
        {
            return 2;
        }

        return ToolKinds().Contains(kind) ? 3 : -1;
    }

    private static HashSet<Type> _tools;

    /// <summary>Every tool any class carries (<see cref="BotOutfit.ToolsFor"/>), read once the classes exist.</summary>
    private static HashSet<Type> ToolKinds()
    {
        if (_tools != null)
        {
            return _tools;
        }

        var classes = BotClasses.All;
        HashSet<Type> tools = [];

        for (var i = 0; i < classes.Count; i++)
        {
            var list = BotOutfit.ToolsFor(classes[i]);

            for (var t = 0; t < list.Count; t++)
            {
                tools.Add(list[t]);
            }
        }

        // Kept only once the classes are read: an empty set taken before them would be remembered for the life of the shard.
        if (classes.Count > 0)
        {
            _tools = tools;
        }

        return tools;
    }

    /// <summary>Whether the index reads this kind at all: any kind of the four families.</summary>
    private static bool Indexed(Type kind) => Family(kind) >= 0;

    // ---- Counted, since the shard started; every gate apart ----------------------------------------------------------------

    private static readonly long[] _asked = new long[Families];

    private static readonly long[] _askedUnits = new long[Families];

    /// <summary>Of the asks, those made where no counter in the bot's reach sold the thing at all.</summary>
    private static readonly long[] _askedNoCounter = new long[Families];

    /// <summary>Asks met to the last unit, by the route the goods came: a carrier's walk, a trader's haul, the buyer at a bot's shop.</summary>
    private static readonly long[] _metCarrier = new long[Families];

    private static readonly long[] _metTrader = new long[Families];

    private static readonly long[] _metShop = new long[Families];

    private static readonly long[] _metUnits = new long[Families];

    private static readonly long[] _metGold = new long[Families];

    private static readonly long[] _walked = new long[Families];

    private static readonly long[] _partMet = new long[Families];

    private static readonly long[] _late = new long[Families];

    private static readonly long[] _had = new long[Families];

    private static readonly long[] _gone = new long[Families];

    private static readonly long[] _ended = new long[Families];

    private static readonly long[] _urgent = new long[Families];

    private static readonly long[] _noHolder = new long[Families];

    private static readonly long[] _spokenFor = new long[Families];

    private static readonly long[] _fewHeld = new long[Families];

    private const int UrgentKinds = 5;

    private static readonly string[] _urgentNames =
    [
        "hurt", "in a fight", "out of a supply it fights or mends with", "a medic below the bandages the doors ask",
        "below its entry threshold"
    ];

    private static readonly long[] _urgentBy = new long[UrgentKinds];

    /// <summary>Asks taken back mid-wait because the bot's need turned urgent with nobody bringing it.</summary>
    public static long UrgentMidWait { get; private set; }

    /// <summary>Looks at which the need was urgent and a carrier was already bringing it: waited for, not taken back (re-review R1).</summary>
    public static long UrgentCarrier { get; private set; }

    /// <summary>Restocks that walked because the purse could not carry the compulsory part, or the ask was under a carrier's least.</summary>
    public static long Poor { get; private set; }

    /// <summary>Restocks that walked because a want for the supply already stood, said some other way.</summary>
    public static long Standing { get; private set; }

    /// <summary>Restocks that walked because this bot asked the island for the same supply inside <see cref="AgainMs"/>.</summary>
    public static long Again { get; private set; }

    /// <summary>Looks at a bot already walking a restock (in hand or put down): no ask while it is on the walk.</summary>
    public static long Walking { get; private set; }

    /// <summary>Asks the market refused (the board full, or the money not reservable when said).</summary>
    public static long Refused { get; private set; }

    /// <summary>Looks that passed a waiting supply over for the next; looks at which a carrier was on the way to an ask of this bot.</summary>
    public static long PassedOver { get; private set; }

    public static long CarrierLooks { get; private set; }

    /// <summary>Looks with nothing short but supplies being waited for (counted here, not as "short of nothing").</summary>
    public static long OnlyWaiting { get; private set; }

    /// <summary>Wants read back from the save after a restart, for one of the four families, taken into the wait here.</summary>
    public static long Adopted { get; private set; }

    /// <summary>Bandages cut out of cloth a carrier handed over against a bandage restock.</summary>
    public static long Cut { get; private set; }

    // ---- The asks standing ---------------------------------------------------------------------------------------------

    private sealed class Ask
    {
        public BotWant Want;

        public BotLine Line;

        public Type Need;

        public int Family;

        public long At;

        public int Units;

        public int HeldAt;

        public int FilledAt;

        public int PaidAt;

        public bool Cuts;
    }

    private static readonly Dictionary<(Serial, Type), Ask> _asks = [];

    private static readonly Dictionary<(Serial, Type), long> _lately = [];

    /// <summary>Bots already looked over for wants read back from the save, once a world.</summary>
    private static readonly HashSet<Serial> _scanned = [];

    private static long _sweptTick;

    private static bool _swept;

    /// <summary>Asks standing now.</summary>
    public static int Open => _asks.Count;

    /// <summary>Whether this bot asked the island for this supply here inside <see cref="AgainMs"/>: the pile ask then does not ask again.</summary>
    internal static bool Lately(Serial who, Type need) =>
        _lately.TryGetValue((who, need), out var at) && Core.TickCount - at < AgainMs;

    /// <summary>
    /// Whether this bot is waiting for the island to bring this supply: the shopper passes it over (<c>BotShopper.Wanting</c>) and
    /// looks at the bot's next shortage, so the wait for one supply holds back no other.
    /// </summary>
    internal static bool Parked(Serial who, Type kind)
    {
        if (_asks.Count == 0 || kind == null || !_asks.ContainsKey((who, kind)))
        {
            return false;
        }

        PassedOver++;
        _parkedThisLook = true;

        return true;
    }

    private static bool _parkedThisLook;

    /// <summary>Whether the shopper's last look passed a waiting supply over. Read once after <c>Wanting</c>; reset by <see cref="Review"/>.</summary>
    internal static bool ParkedThisLook => _parkedThisLook;

    /// <summary>A look with nothing short but what is being waited for: counted here rather than as "short of nothing".</summary>
    internal static void OnlyWaited() => OnlyWaiting++;

    /// <summary>
    /// The shopper's first question, asked at every look before it reads what the bot is short of: this bot's asks looked over.
    /// One gone is concluded; one whose wait is over with nobody bringing it is taken back; one whose need has turned urgent with
    /// nobody bringing it is taken back at once and the bot walks. One a carrier is bringing is waited for even then: taking it back
    /// fails the carrier's walk, and the carrier usually arrives sooner than the walk to a counter would (re-review R1). After a restart, the bot's
    /// wants read back from the save are taken into the wait once (<see cref="AdoptRestored"/>).
    /// </summary>
    internal static void Review(IBotWilful bot, Mobile body, BotClass klass, Container pack)
    {
        _parkedThisLook = false;

        if (body == null || klass == null || pack == null)
        {
            return;
        }

        var now = Core.TickCount;

        if (_scanned.Count < 65536 && _scanned.Add(body.Serial))
        {
            AdoptRestored(bot, body, pack, now);
        }

        if (_asks.Count == 0)
        {
            return;
        }

        Sweep(now);

        List<(Serial, Type)> mine = null;

        foreach (var (key, _) in _asks)
        {
            if (key.Item1 == body.Serial)
            {
                (mine ??= []).Add(key);
            }
        }

        if (mine == null)
        {
            return;
        }

        int? urgent = null;
        bool? below = null;

        for (var i = 0; i < mine.Count; i++)
        {
            if (!_asks.TryGetValue(mine[i], out var ask))
            {
                continue;
            }

            if (!BotMarket.Stands(ask.Want))
            {
                _asks.Remove(mine[i]);
                Conclude(ask, now);

                continue;
            }

            urgent ??= Urgent(bot, body, klass, pack);
            var why = urgent.Value;

            if (why < 0 && BotProvision.Consumable(ask.Need))
            {
                below ??= BotProvision.Short(body);

                if (below.Value)
                {
                    why = 4;
                }
            }

            var coming = ask.Want.Coming > 0;

            if (why >= 0 && !coming)
            {
                _asks.Remove(mine[i]);
                UrgentMidWait++;
                Withdraw(ask, now, $"{_urgentNames[why]} now");

                continue;
            }

            if (why >= 0)
            {
                UrgentCarrier++;
            }

            if (coming)
            {
                CarrierLooks++;

                continue;
            }

            if (now - ask.At >= WaitMs)
            {
                _asks.Remove(mine[i]);
                Withdraw(ask, now, null);
            }
        }
    }

    /// <summary>Whether this bot has a restock in hand or put down: it is walking to a counter or a bot's shop already.</summary>
    private static bool OnTheWalk(IBotWilful bot) => bot.Resolve?.Deed is BotRestock || bot.Resolve?.Paused?.Deed is BotRestock;

    /// <summary>
    /// The shopper's second question, asked where it would otherwise walk to a counter: ask the island first? True when a want was
    /// said and the bot is to wait; false when it walks as before. <paramref name="need"/> is what the bot is short of (bandages,
    /// when it would buy cloth to cut), <paramref name="buying"/> what it would buy, <paramref name="unit"/> what the counter would
    /// charge it for one of that, <paramref name="needy"/> whether it is below its entry threshold (the shopper's own reading),
    /// <paramref name="owned"/> whether the pile's or the far ask's clock already holds this need, <paramref name="noCounter"/>
    /// whether no counter in the bot's reach sells it at all (the price is then <see cref="Price"/>, and after the wait the shopper
    /// goes to the board as before).
    /// </summary>
    internal static bool Asks(
        IBotWilful bot, Mobile body, BotClass klass, Container pack, Type need, Type buying, int amount, int unit, bool band,
        bool cutting, bool needy, bool owned, bool noCounter
    )
    {
        if (!Running || bot == null || body == null || klass == null || pack == null || band || amount <= 0 || unit <= 0
            || !BotMarket.Running || owned)
        {
            return false;
        }

        var family = Family(need);

        // The bot's own weapon is its weapon even when it is a pickaxe: a fight wants it now (BotShopper.Patient).
        if (family < 0 || bot.Bond?.Weapon?.Weapon == need)
        {
            return false;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        // Already on the walk: the restock reviewed every fifteen seconds asks the shopper again, and a want said now would hold the
        // money the counter is about to take, or bring the same thing twice (review of build next, F1).
        if (OnTheWalk(bot))
        {
            Walking++;

            return false;
        }

        var now = Core.TickCount;
        var key = (body.Serial, need);

        if (_lately.TryGetValue(key, out var last) && now - last < AgainMs)
        {
            Again++;

            return false;
        }

        // A want for it already stands, said some other way: left to whoever said it, and the bot walks as before.
        if (BotMarket.Wanted(bot, need) != null || buying != need && BotMarket.Wanted(bot, buying) != null)
        {
            Standing++;

            return false;
        }

        var urgent = needy ? 4 : Urgent(bot, body, klass, pack);

        if (urgent >= 0)
        {
            _urgent[family]++;
            _urgentBy[urgent]++;

            return false;
        }

        // Who holds what it would buy, at what it would pay: a bot that cuts cloth into bandages asks for cloth at cloth's price, never
        // for bandages at a bandage's — twice its own cheapest road (re-review R2).
        var kind = cutting && buying == typeof(Cloth) ? buying : need;
        var price = unit;
        var held = Held(body, kind, price, now, out var largest, out var holders, out var spoken);

        if (held <= 0)
        {
            if (spoken > 0)
            {
                _spokenFor[family]++;
            }
            else
            {
                _noHolder[family]++;
            }

            return false;
        }

        var sized = Sized(body, need, kind, amount, price, pack);

        if (sized <= 0)
        {
            Poor++;

            return false;
        }

        // One carrier is to be able to meet it: no more than the largest single holder holds (F4). Holders with less than half of it
        // would leave the bot short and walking anyway, three minutes later.
        var one = Math.Min(largest, held);

        if (one * 2 < sized)
        {
            _fewHeld[family]++;

            return false;
        }

        var units = Math.Min(sized, one);

        if ((long)units * price < BotCourier.LeastGold)
        {
            Poor++;

            return false;
        }

        var heldAt = pack.GetAmount(need);
        var want = BotMarket.Ask(bot, kind, units, price);

        if (want == null)
        {
            Refused++;

            return false;
        }

        Stamp(key, now);
        _asks[key] = new Ask
        {
            Want = want,
            Line = want.Newest,
            Need = need,
            Family = family,
            At = now,
            Units = units,
            HeldAt = heldAt,
            FilledAt = want.Filled,
            PaidAt = want.Paid,
            Cuts = cutting && kind == typeof(Cloth)
        };

        _asked[family]++;
        _askedUnits[family] += units;

        if (noCounter)
        {
            _askedNoCounter[family]++;
        }

        logger.Information(
            "{Name} asked the island for {Units} {Item} at {Offer}gp before walking to a counter: {Held} held over their keep by {Holders} bot(s) within {Reach} tiles, {Largest} by one (line #{Line})",
            body.Name,
            units,
            kind.Name,
            price,
            held,
            holders,
            BotCourier.Reach,
            largest,
            want.Newest?.Name ?? "?"
        );

        return true;
    }

    /// <summary>The clock against asking again, pruned of stamps older than <see cref="AgainMs"/> rather than cleared whole (F9).</summary>
    private static void Stamp((Serial, Type) key, long now)
    {
        if (_lately.Count > 4096)
        {
            List<(Serial, Type)> old = null;

            foreach (var (k, at) in _lately)
            {
                if (now - at >= AgainMs)
                {
                    (old ??= []).Add(k);
                }
            }

            for (var i = 0; old != null && i < old.Count; i++)
            {
                _lately.Remove(old[i]);
            }
        }

        _lately[key] = now;
    }

    /// <summary>
    /// After a restart, once per bot: its wants read back from the save for one of the four families — a bot that is no trader (a
    /// trader's want is a gathering) and is not on the walk already — whose oldest open line is still well inside its first ten
    /// minutes are taken into the wait from when they were said, so they do not stand to their hour while the bot also walks (F8).
    /// </summary>
    private static void AdoptRestored(IBotWilful bot, Mobile body, Container pack, long now)
    {
        if (BotMarket.RestoredWants <= 0 || BotTrading.Trader(body) || OnTheWalk(bot))
        {
            return;
        }

        var wants = BotMarket.Wants;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (!ReferenceEquals(want.Buyer, bot) || want.Newest is not { Restored: true } line)
            {
                continue;
            }

            var need = want.Kind == typeof(Cloth) ? typeof(Bandage) : want.Kind;
            var family = Family(need);

            if (family < 0 || _asks.ContainsKey((body.Serial, need)) || OldestOpen(want, now) >= BotLines.GraceMs - 2L * SweepMs)
            {
                continue;
            }

            Stamp((body.Serial, need), now);
            _asks[(body.Serial, need)] = new Ask
            {
                Want = want,
                Line = line,
                Need = need,
                Family = family,
                At = line.OpenedTick,
                Units = want.Amount,
                HeldAt = pack.GetAmount(need),
                FilledAt = want.Filled,
                PaidAt = want.Paid,
                Cuts = want.Kind == typeof(Cloth) && BotOutfit.Oldest<Scissors>(pack) != null
            };

            Adopted++;
        }
    }

    /// <summary>
    /// What one is asked at where no counter in the bot's reach sells it: the island's lowest counter price for it, else what it
    /// last changed hands for between two bots; nought (and no ask) when neither is known.
    /// </summary>
    internal static int Price(Type kind)
    {
        var lowest = BotShops.Lowest(kind);

        return lowest > 0 ? lowest : BotMarket.Worth(kind, 0);
    }

    /// <summary>
    /// How many of a consumable the purse lets this bot ask for: the part up to the doors' threshold out of whatever it has, the rest
    /// only above the keep — <c>BotShopper.ByPurse</c>'s sum, without the guild's money and without its counters. Nought when the purse
    /// cannot carry the compulsory part: that bot walks, and the guild pays below the threshold as before. A tool is one.
    /// </summary>
    private static int Sized(Mobile body, Type need, Type buying, int amount, int unit, Container pack)
    {
        if (!BotShopper.PurseSized || !BotProvision.Consumable(need) || buying != need && buying != typeof(Cloth))
        {
            return amount;
        }

        var held = pack.GetAmount(need);
        var must = Math.Clamp(BotProvision.Floor(body, need) - held, 0, amount);
        var free = Math.Max(0, BotYield.Spendable(body));

        if (must > 0 && free / unit < must)
        {
            return 0;
        }

        var left = Math.Max(0, free - must * unit);
        var extra = Math.Min(amount - must, Math.Max(0, left - BotShopper.Reserve) / unit);

        return must + Math.Max(0, extra);
    }

    /// <summary>
    /// Why this bot's restock may not wait, or -1: hurt; in a fight; out of a supply it fights or mends with; a medic below the
    /// bandages the doors ask. Out of: the bandages its kit carries, a bottle its class carries, the arrows its bow shoots, a reagent
    /// its key spell burns — the restock it is on stands in front of the one for that, so it is not parked behind a wait. (Below its
    /// entry threshold, the fifth cause, is the shopper's own <c>needy</c> and is read by the callers.)
    /// </summary>
    internal static int Urgent(IBotWilful bot, Mobile body, BotClass klass, Container pack)
    {
        if (body.HitsMax > 0 && body.Hits < body.HitsMax * HurtShare)
        {
            return 0;
        }

        if (BotLadder.Hunted(bot.Resolve) || body.Combatant != null)
        {
            return 1;
        }

        var kit = klass.Kit;

        if (kit.Bandages > 0 && pack.GetAmount(typeof(Bandage)) <= 0)
        {
            return 2;
        }

        var bottles = BotOutfit.PotionsFor(klass);

        for (var i = 0; i < bottles.Count; i++)
        {
            if (bottles[i].Count > 0 && pack.GetAmount(bottles[i].Kind) <= 0)
            {
                return 2;
            }
        }

        if (bot.Bond is { } bond && bond.Weapon is { Ammunition: { } quiver } && BotBinding.BoundCount(quiver, bond) > 0
            && pack.GetAmount(quiver) <= 0)
        {
            return 2;
        }

        if (kit.Reagents > 0)
        {
            for (var i = 0; i < _reagents.Length; i++)
            {
                if (pack.GetAmount(_reagents[i]) <= 0 && BotProvision.Floor(body, _reagents[i]) > 0)
                {
                    return 2;
                }
            }
        }

        if (klass.Role == BotRole.Medic && pack.GetAmount(typeof(Bandage)) < BotProvision.Floor(body, typeof(Bandage)))
        {
            return 3;
        }

        return -1;
    }

    private static readonly Type[] _reagents =
    [
        typeof(SulfurousAsh), typeof(BlackPearl), typeof(Garlic), typeof(Ginseng),
        typeof(SpidersSilk), typeof(Nightshade), typeof(Bloodmoss), typeof(MandrakeRoot)
    ];

    // ---- Ending an ask ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// The asks standing looked over: one whose want is gone is concluded (met, or ended some other way); one whose wait is over
    /// with nobody bringing it is taken back. Every <see cref="SweepMs"/>, from any shopper's look and from the summary.
    /// </summary>
    private static void Sweep(long now)
    {
        if (_swept && now - _sweptTick < SweepMs)
        {
            return;
        }

        _swept = true;
        _sweptTick = now;

        if (_asks.Count == 0)
        {
            return;
        }

        List<(Serial, Type)> done = null;

        foreach (var (key, ask) in _asks)
        {
            if (!BotMarket.Stands(ask.Want))
            {
                (done ??= []).Add(key);

                continue;
            }

            if (ask.Want.Coming <= 0 && now - ask.At >= WaitMs)
            {
                (done ??= []).Add(key);
            }
        }

        if (done == null)
        {
            return;
        }

        for (var i = 0; i < done.Count; i++)
        {
            if (!_asks.Remove(done[i], out var ask))
            {
                continue;
            }

            if (BotMarket.Stands(ask.Want))
            {
                Withdraw(ask, now, null);
            }
            else
            {
                Conclude(ask, now);
            }
        }
    }

    /// <summary>
    /// Taken back by the existing road (<see cref="BotMarket.Withdrawn"/>): "nobody brought it, walked to a counter"; "had it another
    /// way" when the pack already holds what was asked; "other" when the buyer is dead or out of the world at the end of the wait
    /// (F7). Inside the line's first ten minutes that line is kept out; past them it is failed, by the rule build 391 wrote, and
    /// counted here too.
    /// </summary>
    private static void Withdraw(Ask ask, long now, string because)
    {
        var want = ask.Want;
        var buyer = want.Buyer;
        var body = buyer?.Self;
        var filled = Math.Max(0, want.Filled - ask.FilledAt);
        var age = OldestOpen(want, now);
        var gone = body is not { Deleted: false, Alive: true } || body.Map == null || body.Map == Map.Internal;
        var have = !gone && (body.Backpack?.GetAmount(ask.Need) ?? 0) >= ask.HeldAt + ask.Units;

        BotMarket.Withdrawn(buyer, want.Kind, gone ? BotWithdraw.Other : have ? BotWithdraw.Have : BotWithdraw.Walked);

        if (gone)
        {
            _gone[ask.Family]++;
        }
        else if (have)
        {
            _had[ask.Family]++;
        }
        else
        {
            _walked[ask.Family]++;
        }

        if (filled > 0)
        {
            _partMet[ask.Family]++;
            _metUnits[ask.Family] += filled;
            _metGold[ask.Family] += Math.Max(0, want.Paid - ask.PaidAt);
        }

        if (age >= BotLines.GraceMs)
        {
            _late[ask.Family]++;
        }

        Cutting(ask, body, filled);

        logger.Information(
            "{Name} took back its ask for {Item} after {Minutes:F1} min{Because}: {Filled} of {Units} handed{Late}; {Then}",
            body?.Name ?? "a bot",
            want.Kind?.Name,
            (now - ask.At) / 60000.0,
            because == null ? "" : $" ({because})",
            filled,
            ask.Units,
            age >= BotLines.GraceMs ? $", its line {age / 60000.0:F1} min old" : "",
            gone ? "its buyer is dead or out of the world" : have ? "it has what it asked for" : "it walks to a counter"
        );
    }

    /// <summary>
    /// The want left the board some other way: met to the last unit — by a carrier's walk, a trader's haul (counter goods with
    /// carriage) or the buyer at a bot's shop, by the routes its line recorded (F7) — or stood down, given up or orphaned.
    /// </summary>
    private static void Conclude(Ask ask, long now)
    {
        var want = ask.Want;
        var body = want.Buyer?.Self;
        var filled = Math.Max(0, want.Filled - ask.FilledAt);

        if (want.Amount <= 0)
        {
            var routes = Routes(ask);
            string by;

            if ((routes & BotRoute.Trader) != 0)
            {
                _metTrader[ask.Family]++;
                by = "a trader's haul";
            }
            else if ((routes & BotRoute.Shop) != 0)
            {
                _metShop[ask.Family]++;
                by = "the buyer at a bot's shop";
            }
            else
            {
                _metCarrier[ask.Family]++;
                by = "a carrier";
            }

            _metUnits[ask.Family] += filled;
            _metGold[ask.Family] += Math.Max(0, want.Paid - ask.PaidAt);

            logger.Information(
                "{Name}'s ask for {Item} was met by {By} in {Minutes:F1} min: {Filled} for {Gold}gp, no walk to a counter",
                body?.Name ?? "a bot",
                want.Kind?.Name,
                by,
                (now - ask.At) / 60000.0,
                filled,
                Math.Max(0, want.Paid - ask.PaidAt)
            );
        }
        else
        {
            _ended[ask.Family]++;
        }

        Cutting(ask, body, filled);
    }

    /// <summary>The routes the ask's line was filled by; the want's every line when the ask's own is not known.</summary>
    private static BotRoute Routes(Ask ask)
    {
        if (ask.Line != null)
        {
            return ask.Line.Routes;
        }

        var routes = BotRoute.None;
        var lines = ask.Want.Lines;

        for (var i = 0; i < lines.Count; i++)
        {
            routes |= lines[i].Routes;
        }

        return routes;
    }

    /// <summary>Cloth handed over against a bandage restock is cut into bandages, as cloth bought for them at a counter is.</summary>
    private static void Cutting(Ask ask, Mobile body, int filled)
    {
        if (!ask.Cuts || filled <= 0 || body is not { Deleted: false, Alive: true })
        {
            return;
        }

        Cut += BotWeave.Bandages(body, filled);
    }

    /// <summary>How long the oldest open line of a want has stood; nought when none is open.</summary>
    private static long OldestOpen(BotWant want, long now)
    {
        var lines = want.Lines;
        var oldest = 0L;

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].State == BotLineState.Open)
            {
                oldest = Math.Max(oldest, now - lines[i].OpenedTick);
            }
        }

        return oldest;
    }

    // ---- Who holds what ---------------------------------------------------------------------------------------------------

    private static readonly Dictionary<Type, List<(BotMobile Bot, int Units)>> _held = [];

    private static readonly Dictionary<Type, int> _tally = [];

    private static long _readTick;

    private static bool _read;

    /// <summary>Holders and units at the last reading, for the summary.</summary>
    private static int _holdersRead;

    private static long _unitsRead;

    /// <summary>
    /// Units of <paramref name="kind"/> held over their keep by bots a carrier would send: within a carrier's reach of
    /// <paramref name="body"/>, on its land, not wanting the kind themselves, not already walking a delivery or a haul, and holding
    /// at least a carrier's least at <paramref name="unit"/> (F5) — less what other buyers within that reach already ask for and nobody
    /// is bringing (<paramref name="spoken"/>). <paramref name="largest"/> is the most one of them holds. The carrier's own reading of
    /// a pack (<see cref="BotCourier"/>), made ahead of it.
    /// </summary>
    private static int Held(Mobile body, Type kind, int unit, long now, out int largest, out int holders, out int spoken)
    {
        largest = 0;
        holders = 0;
        spoken = 0;

        Read(now);

        if (!_held.TryGetValue(kind, out var list) || list.Count == 0)
        {
            return 0;
        }

        var map = body.Map;
        var where = body.Location;
        var reach = BotCourier.Reach;
        var units = 0;

        for (var i = 0; i < list.Count; i++)
        {
            var (holder, has) = list[i];

            if (ReferenceEquals(holder, body) || holder is not { Deleted: false, Alive: true } || holder.Map != map
                || holder.Squad != null || (long)has * unit < BotCourier.LeastGold
                || holder.Resolve?.Deed is IBotWantCarrier || holder.Resolve?.Paused?.Deed is IBotWantCarrier
                || !Utility.InRange(holder.Location, where, reach) || !BotGates.Joined(map, holder.Location, where)
                || BotMarket.Wanted(holder, kind) != null)
            {
                continue;
            }

            units += has;
            holders++;
            largest = Math.Max(largest, has);
        }

        if (units <= 0)
        {
            return 0;
        }

        var asked = 0;
        var wants = BotMarket.Wants;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (want.Kind == kind && want.IsOpen && want.Buyer?.Self is { Deleted: false } buyer && !ReferenceEquals(buyer, body)
                && buyer.Map == map && Utility.InRange(buyer.Location, where, reach))
            {
                asked += want.Payable;
            }
        }

        spoken = Math.Min(units, asked);

        return Math.Max(0, units - asked);
    }

    /// <summary>
    /// The population read for what each bot holds of the four families over its keep: the pack's merchandise less
    /// <see cref="BotUnload.Keeps"/>, and for a bot standing at a bank its bank box's merchandise (surplus by construction). A worn-out
    /// tool is not counted. A bot in a company is left out: it carries nothing for anybody until it is free.
    /// </summary>
    private static void Read(long now)
    {
        if (_read && now - _readTick < HoldersMs)
        {
            return;
        }

        _read = true;
        _readTick = now;
        _holdersRead = 0;
        _unitsRead = 0;

        foreach (var (_, list) in _held)
        {
            list.Clear();
        }

        var bots = BotPopulation.Bots;

        for (var b = 0; b < bots.Count; b++)
        {
            var bot = bots[b];

            if (bot is not { Deleted: false, Alive: true } || bot.Map == null || bot.Map == Map.Internal || bot.Squad != null
                || bot.Backpack is not { } pack)
            {
                continue;
            }

            _tally.Clear();

            var bond = bot.Bond;
            var items = pack.Items;

            for (var i = 0; i < items.Count; i++)
            {
                Count(items[i], bond, _tally);
            }

            if (_tally.Count > 0)
            {
                var keep = BotUnload.Keeps(bot);

                List<Type> kinds = [.. _tally.Keys];

                for (var k = 0; k < kinds.Count; k++)
                {
                    if (!keep.TryGetValue(kinds[k], out var allowed))
                    {
                        continue;
                    }

                    var over = allowed >= int.MaxValue ? 0 : Math.Max(0, _tally[kinds[k]] - allowed);

                    if (over <= 0)
                    {
                        _tally.Remove(kinds[k]);
                    }
                    else
                    {
                        _tally[kinds[k]] = over;
                    }
                }
            }

            if (BotMarket.AtBank(bot) && bot.FindBankNoCreate() is { } box)
            {
                var boxed = box.Items;

                for (var i = 0; i < boxed.Count; i++)
                {
                    Count(boxed[i], bond, _tally);
                }
            }

            if (_tally.Count == 0)
            {
                continue;
            }

            _holdersRead++;

            foreach (var (kind, units) in _tally)
            {
                if (!_held.TryGetValue(kind, out var list))
                {
                    list = [];
                    _held[kind] = list;
                }

                list.Add((bot, units));
                _unitsRead += units;
            }
        }
    }

    private static void Count(Item item, BotBond bond, Dictionary<Type, int> into)
    {
        if (item is not { Deleted: false })
        {
            return;
        }

        var kind = item.GetType();

        if (!Indexed(kind) || !BotShopkeep.Merchandise(item, bond) || Family(kind) == 3 && BotOutfit.Spent(item))
        {
            return;
        }

        into[kind] = into.GetValueOrDefault(kind) + Math.Max(1, item.Amount);
    }

    // ---- The summary and a new world ----------------------------------------------------------------------------------------

    /// <summary>The clause of the <c>Trade:</c> line: by family, what was asked, met before the walk and by which route, walked after the wait, and every gate.</summary>
    public static string Describe()
    {
        Sweep(Core.TickCount);

        var families = new string[Families];

        for (var f = 0; f < Families; f++)
        {
            families[f] =
                $"{_familyNames[f]} {_asked[f]} asked ({_askedUnits[f]} units; {_askedNoCounter[f]} where no counter in reach sold it), met before the walk by a carrier {_metCarrier[f]}, by a trader's haul {_metTrader[f]}, at a bot's shop {_metShop[f]}, {_walked[f]} walked after the wait ({_partMet[f]} part met, {_late[f]} taken back past their line's first {BotLines.GraceMs / 60000} min: failed lines), {_had[f]} had it another way, {_gone[f]} taken back with the buyer dead or out of the world, {_ended[f]} ended otherwise, {_metUnits[f]} units handed for {_metGold[f]}gp; looks that walked as before: {_urgent[f]} for urgency, {_noHolder[f]} for no holder, {_spokenFor[f]} for holders already asked of, {_fewHeld[f]} for no one holder with half of it";
        }

        return $"asked the island before a counter{(Running ? "" : " (off)")} (a holder within {BotCourier.Reach} tiles, a wait of {WaitMs / 60000.0:F0} min): "
               + string.Join("; ", families)
               + $"; urgent by cause: {_urgentNames[0]} {_urgentBy[0]}, {_urgentNames[1]} {_urgentBy[1]}, {_urgentNames[2]} {_urgentBy[2]}, {_urgentNames[3]} {_urgentBy[3]}, {_urgentNames[4]} {_urgentBy[4]}; {UrgentMidWait} taken back mid-wait for urgency, {UrgentCarrier} looks urgent with a carrier on the way, which was waited for; "
               + $"looks that walked as before, across families: {Walking} already on the walk to a counter, {Poor} for the purse or a carrier's {BotCourier.LeastGold}gp least, {Standing} for a want already standing, {Again} for an ask inside {AgainMs / 60000} min, {Refused} refused by the market; "
               + $"{PassedOver} looks passed a waiting supply over for the next, {OnlyWaiting} had nothing short but what was waited for, {CarrierLooks} with a carrier on the way; {Adopted} read back after a restart and taken into the wait; {Cut} bandages cut from cloth handed over; {Open} standing now; "
               + $"holders at the last reading: {_holdersRead} bots holding {_unitsRead} units";
    }

    /// <summary>A new world: the asks and holders were the old one's.</summary>
    public static void Reset()
    {
        _asks.Clear();
        _lately.Clear();
        _scanned.Clear();
        _held.Clear();
        _tally.Clear();
        _read = false;
        _swept = false;
        _tools = null;
        _holdersRead = 0;
        _unitsRead = 0;
        _parkedThisLook = false;
    }
}
