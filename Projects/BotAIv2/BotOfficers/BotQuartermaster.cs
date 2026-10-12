using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The quartermaster's work: a shift at the guild's store, the kits it issues, the surplus it takes in, the wants it posts for
/// the guild and the surplus it sells.
///
/// <para>
/// <b>A shift like a shop's.</b> The quartermaster walks to the store (<see cref="BotGuildStore.At"/>) and keeps it open for
/// <see cref="ShiftMinutes"/>, then rests <see cref="RestMs"/>; it is offered a shift sooner when members are waiting for a kit.
/// While it is open: a member standing within <see cref="Counter"/> tiles with capped surplus over its allowance hands it in
/// (<see cref="BotGuildStore.Hand"/>); a member short of its kit and poor or new walks in and draws what it lacks
/// (<see cref="BotKitDraw"/>, <see cref="BotGuildStore.Issue"/>) — a debt; the goods the guild bought are put away and the
/// quartermaster is paid back by the chest at the want's price; once a shift it posts a want for the kind the store is shortest
/// of, puts one lot of surplus on the hall's merchant and carries the merchant's takings into the chest.
/// </para>
///
/// <para>
/// <b>The guild's wants are the chest's purchases, said by the quartermaster.</b> The market takes a want only from a bot that can
/// pay it (<see cref="BotMarket.Ask"/>), so the quartermaster asks in its own name, pays the carrier from its own purse like any
/// buyer, and the chest pays it back unit for unit at the want's price when the goods go into the store — never before, never for
/// goods it did not get. A want is posted only when the chest holds the whole bill. What the store aims to hold of a kind is half
/// what the members' kits ask (<see cref="TargetShare"/>), twice that while the guild is at war or its chest saves for a war
/// (<see cref="WarFactor"/>, "what to buy for a war by the chest's purpose"). Its lines are the guild's procurement, counted apart
/// from the members' own Needs lines (<c>BotLines.IsProcurement</c>).
/// </para>
/// </summary>
public sealed class BotQuartermaster : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotQuartermaster));

    public string Name => "Quartermaster";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>What a shift is reckoned at a minute: the guild's duty, unpaid.</summary>
    public static double Prior { get; set; } = 30.0;

    /// <summary>And per member waiting for a kit.</summary>
    public static double WaitingWorth { get; set; } = 8.0;

    public static int ShiftMinutes { get; set; } = 6;

    /// <summary>The rest between shifts; a shift with members waiting is offered after <see cref="WaitedRestMs"/>.</summary>
    public static int RestMs { get; set; } = 2400000;

    public static int WaitedRestMs { get; set; } = 300000;

    /// <summary>How far from the store a quartermaster is offered a shift.</summary>
    public static int Reach { get; set; } = 200;

    /// <summary>How near the store the quartermaster stands, and how near it a member must be to hand in or draw.</summary>
    public static int StoreTiles { get; set; } = 4;

    public static int Counter { get; set; } = 6;

    /// <summary>How often an open store looks round it.</summary>
    public static int ActMs { get; set; } = 5000;

    /// <summary>Whether the quartermaster posts the guild's wants at all.</summary>
    public static bool Wants { get; set; } = true;

    /// <summary>The most units one guild want asks for.</summary>
    public static int WantMost { get; set; } = 30;

    /// <summary>The least shortfall worth a want.</summary>
    public static int WantLeast { get; set; } = 10;

    /// <summary>What the store aims to hold of a kind, as a share of what the members' kits ask of it.</summary>
    public static double TargetShare { get; set; } = 0.5;

    public static double WarFactor { get; set; } = 2.0;

    /// <summary>How long a guild want is followed before what is left of it is let go.</summary>
    public static int WantHoldMs { get; set; } = 5400000;

    // ---- counters ----------------------------------------------------------------------------------------------------------

    public static long Shifts { get; private set; }

    public static long ShiftMs { get; private set; }

    public static long Opened { get; private set; }

    public static long WantsPosted { get; private set; }

    public static long WantUnits { get; private set; }

    public static long WantsAtWar { get; private set; }

    public static long WantUnfunded { get; private set; }

    public static long WantRefused { get; private set; }

    public static long WantLapsed { get; private set; }

    public static long Delivered { get; private set; }

    public static long Reimbursed { get; private set; }

    public static long ReimburseShort { get; private set; }

    public static long ShelfGold { get; private set; }

    private static readonly Dictionary<string, long> _gates = new(StringComparer.Ordinal);

    private static void Gate(string why) => _gates[why] = _gates.GetValueOrDefault(why) + 1;

    // ---- open stores and guild wants ---------------------------------------------------------------------------------------

    /// <summary>Each guild's open store: the quartermaster keeping it and where.</summary>
    private static readonly Dictionary<string, (BotMobile Qm, Map Map, Point3D At, long Until)> _open = new(StringComparer.Ordinal);

    private static readonly Dictionary<Serial, long> _rested = [];

    /// <summary>Members of each guild that have taken a draw and are walking in. Counted, not trusted: a draw ends by itself.</summary>
    private static readonly Dictionary<string, int> _waiting = new(StringComparer.Ordinal);

    private sealed class Order
    {
        public Type Kind;

        public int Units;

        public int Price;

        public int In;

        public int PaidBack;

        public long Posted;

        public BotWant Want;

        public string Qm;
    }

    private static readonly Dictionary<string, Order> _orders = new(StringComparer.Ordinal);

    /// <summary>Whether this guild's store is open now, and who keeps it where.</summary>
    public static bool Open(Guild guild, out BotMobile qm, out Map map, out Point3D at) => Open(guild, out qm, out map, out at, out _);

    /// <summary>The same, and how long it stays open, in milliseconds.</summary>
    public static bool Open(Guild guild, out BotMobile qm, out Map map, out Point3D at, out long left)
    {
        qm = null;
        map = null;
        at = Point3D.Zero;
        left = 0;

        if (guild?.Name == null || !_open.TryGetValue(guild.Name, out var open))
        {
            return false;
        }

        if (open.Qm is not { Deleted: false, Alive: true } || !BotOfficers.Holds(open.Qm, BotOfficerPost.Quartermaster) || open.Qm.Resolve?.Deed is not Shift)
        {
            _open.Remove(guild.Name);

            return false;
        }

        (qm, map, at, var until) = open;
        left = until - Core.TickCount;

        return true;
    }

    internal static void Waiting(string guild, int delta)
    {
        if (guild != null)
        {
            _waiting[guild] = Math.Max(0, _waiting.GetValueOrDefault(guild) + delta);
        }
    }

    /// <summary>Whether a want on the board is one of the guilds' (procurement for the store, not a member's own need).</summary>
    public static bool Procures(BotWant want)
    {
        if (want == null)
        {
            return false;
        }

        foreach (var order in _orders.Values)
        {
            if (ReferenceEquals(order.Want, want))
            {
                return true;
            }
        }

        return false;
    }

    // ---- the proposer ------------------------------------------------------------------------------------------------------

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotOfficers.On(BotOfficerPost.Quartermaster) || !BotGuildStore.Running || bot?.Self is not BotMobile body)
        {
            return null;
        }

        if (body.Guild is not Guild guild || !BotOfficers.Holds(body, BotOfficerPost.Quartermaster))
        {
            return null;
        }

        if (!body.Alive || body.Tired || body.Squad != null || body.Resolve?.Deed is Shift)
        {
            return null;
        }

        if (!BotGuildStore.At(guild, out var map, out var at))
        {
            Gate("no hall or house to keep a store at");

            return null;
        }

        if (body.Map != map || BotDungeon.Under(body.Location))
        {
            Gate("off the home ground");

            return null;
        }

        if (!body.InRange(at, Reach))
        {
            Gate("far from the store");

            return null;
        }

        var now = Core.TickCount;
        var rested = _rested.TryGetValue(body.Serial, out var last) ? now - last : long.MaxValue;

        if (rested < WaitedRestMs)
        {
            return null;
        }

        // Inside the long rest only members waiting for a kit bring the quartermaster back early; asked only then.
        var waiting = Needy(guild);

        if (rested < RestMs && waiting == 0)
        {
            Gate("resting, nobody waiting for a kit");

            return null;
        }

        if (!BotGates.Joined(map, body.Location, at))
        {
            Gate("no road to the store");

            return null;
        }

        return new Shift(guild, map, at, waiting);
    }

    /// <summary>Members of this guild a kit could be issued to now: poor or new, short of their kit, owing nothing.</summary>
    private static int Needy(Guild guild)
    {
        var needy = 0;

        for (var i = 0; i < guild.Members.Count && needy < 8; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false, Alive: true } m && m.Map != null && m.Map != Map.Internal
                && BotGuildStore.Unfit(m) == null && BotGuildStore.Lacks(m, guild.Name))
            {
                needy++;
            }
        }

        return needy;
    }

    // ---- the shift ---------------------------------------------------------------------------------------------------------

    public sealed class Shift : BotDeed
    {
        public const string Trade = "keep-store";

        private readonly Guild _guild;

        private readonly Map _map;

        private readonly Point3D _at;

        private readonly int _waiting;

        private bool _in;

        private long _since;

        private long _acted;

        private bool _wanted;

        private bool _shelved;

        private int _issued;

        private int _handed;

        private int _bought;

        public Shift(Guild guild, Map map, Point3D at, int waiting)
        {
            _guild = guild;
            _map = map;
            _at = at;
            _waiting = waiting;
        }

        public override string Kind => Trade;

        public override Map Map => _map;

        public override Point3D Where => _at;

        public override double Minutes => ShiftMinutes + 2.0;

        public override double Expects => Prior + WaitingWorth * _waiting;

        public override bool Unpaid => true;

        public override bool Committed => _in;

        public override bool Still => _in;

        public override string Stage => _in ? $"keeping {_guild.Name}'s store" : $"on the way to keep {_guild.Name}'s store";

        public override void Taken(IBotWilful bot)
        {
            if (bot?.Self is BotMobile qm)
            {
                _rested[qm.Serial] = Core.TickCount;
            }
        }

        public override void Drop(IBotWilful bot) => Close(bot?.Self as BotMobile, false);

        public override BotDoing Advance(IBotWilful bot)
        {
            if (bot?.Self is not BotMobile qm)
            {
                return BotDoing.Failed("no body");
            }

            if (_guild.Disbanded || !ReferenceEquals(qm.Guild, _guild) || !BotOfficers.Holds(qm, BotOfficerPost.Quartermaster))
            {
                Close(qm, false);

                return BotDoing.Done($"no longer {_guild.Name}'s quartermaster");
            }

            if (!qm.InRange(_at, StoreTiles))
            {
                if (_in)
                {
                    // Pushed off its post: walk back, the shift's clock goes on.
                    _in = false;
                }

                return BotDoing.Walk(_map, _at, BotArrival.Within(StoreTiles), $"to keep {_guild.Name}'s store");
            }

            var now = Core.TickCount;

            if (!_in)
            {
                _in = true;

                if (_since == 0)
                {
                    _since = now;
                    _acted = now - ActMs;
                    _open[_guild.Name] = (qm, _map, _at, now + ShiftMinutes * 60000L);
                    Opened++;
                    qm.Journey?.Finish();

                    var fill = new Dictionary<string, string> { ["stock"] = BotGuildStore.Stock(_guild.Name) };

                    BotVoice.Say(qm, "guild", BotVoice.Phrase("officers:store-open", null, qm, fill) ?? $"The store is open at the hall: {fill["stock"]}.", force: true);
                }
            }

            if (now - _acted >= ActMs)
            {
                _acted = now;

                // Open again after anything that set the shift aside for a moment (a blow, a heal): the store is open while it stands.
                _open[_guild.Name] = (qm, _map, _at, _since + ShiftMinutes * 60000L);
                Act(qm, now);
            }

            if (now - _since < ShiftMinutes * 60000L)
            {
                return BotDoing.Work($"keeping {_guild.Name}'s store");
            }

            var minutes = (now - _since) / 60000.0;

            Close(qm, true);

            return BotDoing.Done(
                $"kept {_guild.Name}'s store {minutes:F1} minutes: {_issued} kits issued, {_handed} lots taken in from members, {_bought} units the guild bought put away; it holds {BotGuildStore.Stock(_guild.Name, 3)}"
            );
        }

        private void Close(BotMobile qm, bool stood)
        {
            if (_since != 0)
            {
                if (_open.TryGetValue(_guild.Name, out var open) && ReferenceEquals(open.Qm, qm))
                {
                    _open.Remove(_guild.Name);
                }

                if (stood)
                {
                    Shifts++;
                    ShiftMs += Core.TickCount - _since;
                }

                _since = 0;
            }

            if (qm != null)
            {
                _rested[qm.Serial] = Core.TickCount;
            }
        }

        private void Act(BotMobile qm, long now)
        {
            _bought += Settle(qm, _guild);
            _handed += TakeIn(qm, _guild, _at);


            if (!_wanted)
            {
                _wanted = true;
                Want(qm, _guild);
            }

            if (!_shelved)
            {
                _shelved = true;

                var shelved = BotGuildStore.Shelve(qm, _guild);

                // The hall merchant's takings beyond its float into the chest, by the quartermaster's hand.
                if (BotShelf.Of(_guild) is { Deleted: false } merchant)
                {
                    var given = BotShelf.Collect(qm, merchant);

                    if (given > 0 && BotMarket.Charge(qm, given))
                    {
                        BotYield.Aside(qm, given);
                        BotChest.Put(_guild.Name, given);
                        ShelfGold += given;

                        logger.Information(
                            "{Qm} the quartermaster carried {Gold}gp of the hall merchant's takings into {Guild}'s chest{Shelved}; the chest holds {Chest}gp",
                            qm.Name,
                            given,
                            _guild.Name,
                            shelved > 0 ? $" after shelving {shelved} units of the store's surplus" : "",
                            BotChest.Holds(_guild.Name)
                        );
                    }
                }
            }

            _issued += Kits(qm, _guild, _at);
        }
    }

    /// <summary>The kits of members standing at the store with a draw in hand. Returns how many were issued.</summary>
    private static int Kits(BotMobile qm, Guild guild, Point3D at)
    {
        var issued = 0;

        foreach (var m in qm.Map.GetMobilesInRange<BotMobile>(at, Counter))
        {
            if (m is not { Deleted: false, Alive: true } || !ReferenceEquals(m.Guild, guild) || m.Resolve?.Deed is not BotKitDraw.Draw draw || draw.Served)
            {
                continue;
            }

            draw.Serve(qm);

            if (draw.Served)
            {
                issued++;
            }
        }

        return issued;
    }

    /// <summary>Members standing at the store with capped surplus over their allowance hand it in, a whole lot at a time.</summary>
    private static int TakeIn(BotMobile qm, Guild guild, Point3D at)
    {
        var lots = 0;

        foreach (var m in qm.Map.GetMobilesInRange<BotMobile>(at, Counter))
        {
            if (m is not { Deleted: false, Alive: true } || !ReferenceEquals(m.Guild, guild) || m.Backpack is not { } pack)
            {
                continue;
            }

            var keep = BotUnload.Keeps(m);

            foreach (var (kind, cap) in keep)
            {
                if (cap <= 0 || cap >= int.MaxValue || !BotGuildStore.Storable(kind))
                {
                    continue;
                }

                var allowed = Math.Max(cap, BotProvision.Floor(m, kind));
                var have = pack.GetAmount(kind);

                if (have - allowed < BotUnload.KitSlack)
                {
                    continue;
                }

                var lot = pack.FindItemByType(kind);

                if (lot == null || BotBinding.IsBound(lot, m.Bond))
                {
                    continue;
                }

                var spare = Math.Min(have - allowed, Math.Max(1, lot.Amount));

                if (spare < Math.Max(1, lot.Amount))
                {
                    // The engine's own split: the original keeps the amount asked for, so this lot is the surplus.
                    if (Mobile.LiftItemDupe(lot, spare) == null)
                    {
                        continue;
                    }
                }

                if (BotGuildStore.Hand(m, guild, lot))
                {
                    lots++;
                }
                else if (!lot.Deleted && lot.Parent == null)
                {
                    pack.DropItem(lot);
                }
            }
        }

        return lots;
    }

    // ---- the guild's wants -------------------------------------------------------------------------------------------------

    /// <summary>How often the guilds' bought goods are put away and paid back, wherever the quartermaster stands.</summary>
    public static int SettleMs { get; set; } = 30000;

    private static long _settled;

    private static readonly List<string> _settling = [];

    /// <summary>
    /// The guilds' wants settled on the clock: the goods a carrier handed the quartermaster go into the store and the chest pays it
    /// back at once, before its porter can carry them to its own bank box. From the module's timer.
    /// </summary>
    internal static void SettleAll(long now)
    {
        if (_orders.Count == 0 || now - _settled < SettleMs)
        {
            return;
        }

        _settled = now;
        _settling.Clear();
        _settling.AddRange(_orders.Keys);

        for (var i = 0; i < _settling.Count; i++)
        {
            if (!_orders.TryGetValue(_settling[i], out var order))
            {
                continue;
            }

            var guild = BotGuilds.Named(_settling[i]);
            var qm = BotOfficers.Named(order.Qm);

            if (guild == null || guild.Disbanded)
            {
                _orders.Remove(_settling[i]);
                WantLapsed++;

                continue;
            }

            if (qm is { Deleted: false } && qm.Map != null && qm.Map != Map.Internal)
            {
                Settle(qm, guild);
            }
            else if (now - order.Posted >= WantHoldMs)
            {
                _orders.Remove(_settling[i]);
                WantLapsed++;
            }
        }
    }

    /// <summary>
    /// Once a shift: the kind the store is shortest of against what the members' kits ask, asked for on the board in the
    /// quartermaster's name, when the chest holds the bill.
    /// </summary>
    private static void Want(BotMobile qm, Guild guild)
    {
        if (!Wants || _orders.TryGetValue(guild.Name, out var standing) && BotMarket.Stands(standing.Want))
        {
            return;
        }

        var atWar = BotWar.Fighting(guild.Name) > 0 || BotPurpose.Goal(guild, out var aim, out _) && aim != null && aim.Contains("war", StringComparison.OrdinalIgnoreCase);
        var factor = TargetShare * (atWar ? WarFactor : 1.0);

        Dictionary<Type, int> asks = [];

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is not BotMobile { Deleted: false } m)
            {
                continue;
            }

            Add(asks, m, typeof(Bandage));

            var bottles = m.Class == null ? null : BotOutfit.PotionsFor(m.Class);

            for (var b = 0; bottles != null && b < bottles.Count; b++)
            {
                Add(asks, m, bottles[b].Kind);
            }
        }

        Type best = null;
        var shortest = 0;

        foreach (var (kind, ask) in asks)
        {
            var target = Math.Min(BotGuildStore.Most(kind), (int)Math.Ceiling(ask * factor));
            var lack = target - BotGuildStore.Held(guild.Name, kind);

            if (lack > shortest)
            {
                shortest = lack;
                best = kind;
            }
        }

        if (best == null || shortest < WantLeast)
        {
            return;
        }

        var units = Math.Min(WantMost, shortest);
        var price = BotGuildStore.Price(best);
        var bill = units * price;

        if (BotChest.Holds(guild.Name) < bill)
        {
            WantUnfunded++;

            return;
        }

        var want = BotMarket.Ask(qm, best, units, price);

        if (want == null)
        {
            WantRefused++;

            return;
        }

        _orders[guild.Name] = new Order { Kind = best, Units = units, Price = want.Offer, Posted = Core.TickCount, Want = want, Qm = qm.Name };
        WantsPosted++;
        WantUnits += units;

        if (atWar)
        {
            WantsAtWar++;
        }

        logger.Information(
            "{Qm} the quartermaster posted {Guild}'s want: {Units} {Kind} at {Price}gp each, the store holding {Held} against kits asking {Ask}{War}; the chest pays it back on delivery and holds {Chest}gp",
            qm.Name,
            guild.Name,
            units,
            best.Name,
            want.Offer,
            BotGuildStore.Held(guild.Name, best),
            asks[best],
            atWar ? " (twice the stock: the guild is at war or saves for one)" : "",
            BotChest.Holds(guild.Name)
        );

        var fill = new Dictionary<string, string>
        {
            ["amount"] = units.ToString(),
            ["item"] = BotVoice.Words(best.Name),
            ["price"] = want.Offer.ToString()
        };

        BotVoice.Say(qm, "trade", BotVoice.Phrase("officers:want", null, qm, fill) ?? $"The guild needs {units} {BotVoice.Words(best.Name)}.", topic: best.Name);
    }

    private static void Add(Dictionary<Type, int> asks, BotMobile m, Type kind)
    {
        var floor = BotProvision.Floor(m, kind);

        if (floor > 0)
        {
            asks[kind] = asks.GetValueOrDefault(kind) + floor;
        }
    }

    /// <summary>
    /// The goods the guild's want brought, out of the quartermaster's pack into the store, and the chest's payment back to it at
    /// the want's price for exactly what went in. Returns the units put away.
    /// </summary>
    private static int Settle(BotMobile qm, Guild guild)
    {
        if (!_orders.TryGetValue(guild.Name, out var order) || order.Qm != qm.Name)
        {
            return 0;
        }

        var arrived = Math.Min(order.Units, order.Want?.Filled ?? 0) - order.In;
        var units = arrived <= 0 ? 0 : BotGuildStore.Bought(qm, guild, order.Kind, arrived);

        if (units > 0)
        {
            // What the quartermaster really paid the carriers for what arrived and is not paid back yet, for the share of it that
            // went into the store: a carriage agreed over the offer is the guild's cost too, and goods it did not get are not.
            var spent = Math.Max(0, (order.Want?.Paid ?? units * order.Price) - order.PaidBack);
            var owed = (int)Math.Min(spent, (long)spent * units / Math.Max(1, arrived));

            order.In += units;
            Delivered += units;

            var paid = owed <= 0 ? 0 : BotChest.Draw(guild.Name, owed);

            order.PaidBack += owed;

            if (paid > 0)
            {
                BotMarket.Refund(qm, paid);
                BotYield.Aside(qm, -paid);
                Reimbursed += paid;
            }

            if (paid < owed)
            {
                ReimburseShort++;
            }

            logger.Information(
                "{Qm} the quartermaster put {Units} {Kind} the guild bought into {Guild}'s store, and the chest paid it back {Paid} of {Owed}gp; {In} of {Of} in",
                qm.Name,
                units,
                order.Kind.Name,
                guild.Name,
                paid,
                owed,
                order.In,
                order.Units
            );
        }

        if (order.In >= order.Units || Core.TickCount - order.Posted >= WantHoldMs || !BotMarket.Stands(order.Want) && order.In >= (order.Want?.Filled ?? 0))
        {
            if (order.In < order.Units)
            {
                WantLapsed++;

                if (BotMarket.Stands(order.Want))
                {
                    BotMarket.Withdrawn(qm, order.Kind, BotWithdraw.Other);
                }
            }

            _orders.Remove(guild.Name);
        }

        return units;
    }

    // ---- telling -----------------------------------------------------------------------------------------------------------

    public static string Gates()
    {
        if (_gates.Count == 0)
        {
            return "none";
        }

        var say = "";

        foreach (var (why, n) in _gates)
        {
            say += (say.Length == 0 ? "" : ", ") + $"{why} {n}";
        }

        return say;
    }

    public static string Describe()
    {
        var waiting = 0;

        foreach (var n in _waiting.Values)
        {
            waiting += n;
        }

        return $"{Shifts} shifts of {ShiftMs / 60000} minutes ({Opened} opened, {_open.Count} open now, {waiting} members walking in), wants {(Wants ? "on" : "OFF")}: {WantsPosted} posted for {WantUnits} units ({WantsAtWar} at war's stock), "
               + $"{WantUnfunded} not posted for a chest short of the bill, {WantRefused} refused by the market, {WantLapsed} let go unfilled, {_orders.Count} standing; {Delivered} units delivered and {Reimbursed}gp paid back by the chests ({ReimburseShort} short); "
               + $"{ShelfGold}gp of the merchants' takings carried into the chests; draws: {BotKitDraw.Describe()}; passed over: {Gates()}";
    }

    public static void Forget()
    {
        Shifts = 0;
        ShiftMs = 0;
        Opened = 0;
        WantsPosted = 0;
        WantUnits = 0;
        WantsAtWar = 0;
        WantUnfunded = 0;
        WantRefused = 0;
        WantLapsed = 0;
        Delivered = 0;
        Reimbursed = 0;
        ReimburseShort = 0;
        ShelfGold = 0;
        _gates.Clear();
        _open.Clear();
        _rested.Clear();
        _waiting.Clear();
        _orders.Clear();
    }
}

/// <summary>
/// A member short of its kit, poor or new, walks to its guild's open store and draws what it lacks: a debt to the chest
/// (<see cref="BotGuildStore.Issue"/>). Offered only while the store is open and within <see cref="Reach"/>.
/// </summary>
public sealed class BotKitDraw : IBotProposer
{
    public string Name => "KitDraw";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>What drawing a kit is reckoned at a minute: what going out short of it costs, unpaid.</summary>
    public static double Prior { get; set; } = 40.0;

    public static int Reach { get; set; } = 100;

    /// <summary>How often one member may be offered a draw.</summary>
    public static int EveryMs { get; set; } = 1800000;

    /// <summary>How long the store must stay open yet for a draw to be offered.</summary>
    public static int LeastLeftMs { get; set; } = 150000;

    /// <summary>How long a member walks for a draw before the store is given up on.</summary>
    public static int WalkMs { get; set; } = 240000;

    public static long Offered { get; private set; }

    public static long Took { get; private set; }

    public static long Drawn { get; private set; }

    public static long Closed { get; private set; }

    public static long Empty { get; private set; }

    public static long Late { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    private static readonly Dictionary<string, long> _gates = new(StringComparer.Ordinal);

    private static void Gate(string why) => _gates[why] = _gates.GetValueOrDefault(why) + 1;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotOfficers.On(BotOfficerPost.Quartermaster) || bot?.Self is not BotMobile body || body.Guild is not Guild guild)
        {
            return null;
        }

        if (!BotQuartermaster.Open(guild, out var qm, out var map, out var at, out var left) || ReferenceEquals(qm, body))
        {
            return null;
        }

        // Not with the store about to shut: a walk that finds it shut is a failure for nothing.
        if (left < LeastLeftMs)
        {
            return null;
        }

        if (!body.Alive || body.Squad != null || body.Map != map || body.Resolve?.Deed is Draw)
        {
            return null;
        }

        if (!body.InRange(at, Reach))
        {
            Gate("too far from the open store");

            return null;
        }

        if (_last.TryGetValue(body.Serial, out var last) && Core.TickCount - last < EveryMs)
        {
            return null;
        }

        if (BotGuildStore.Unfit(body) is { } unfit)
        {
            Gate(unfit);

            return null;
        }

        if (!BotGuildStore.Lacks(body, guild.Name))
        {
            Gate("the store holds nothing it lacks");

            return null;
        }

        if (!BotGates.Joined(map, body.Location, at))
        {
            Gate("no road to the store");

            return null;
        }

        Offered++;

        return new Draw(guild, map, at);
    }

    public sealed class Draw : BotDeed
    {
        public const string Trade = "draw-kit";

        private readonly Guild _guild;

        private readonly Map _map;

        private readonly Point3D _at;

        private long _began;

        private bool _counted;

        private string _what;

        private int _owed;

        private int _units;

        private string _qm;

        public Draw(Guild guild, Map map, Point3D at)
        {
            _guild = guild;
            _map = map;
            _at = at;
        }

        /// <summary>Whether the quartermaster has handed this draw its kit (or found nothing for it).</summary>
        public bool Served { get; private set; }

        public override string Kind => Trade;

        public override Map Map => _map;

        public override Point3D Where => _at;

        public override double Minutes => 3.0;

        public override double Expects => Prior;

        public override bool Unpaid => true;

        public override string Stage => Served ? "drawing a kit from the store" : $"on the way to {_guild.Name}'s store for a kit";

        public override void Taken(IBotWilful bot)
        {
            _began = Core.TickCount;
            Took++;

            if (bot?.Self is { } body)
            {
                _last[body.Serial] = _began;
            }

            BotQuartermaster.Waiting(_guild.Name, 1);
            _counted = true;
        }

        public override void Drop(IBotWilful bot) => Uncount();

        private void Uncount()
        {
            if (_counted)
            {
                _counted = false;
                BotQuartermaster.Waiting(_guild.Name, -1);
            }
        }

        /// <summary>From the quartermaster's look at the store: the kit handed over.</summary>
        internal void Serve(BotMobile qm)
        {
            if (Served)
            {
                return;
            }

            var member = Mine;

            if (member == null)
            {
                return;
            }

            Served = true;
            _qm = qm.Name;
            _units = BotGuildStore.Issue(qm, member, out _what, out _owed);

            if (_units > 0)
            {
                var fill = new Dictionary<string, string> { ["other"] = member.Name, ["what"] = _what, ["owed"] = _owed.ToString() };

                BotVoice.Say(qm, "local", BotVoice.Phrase("officers:kit", null, qm, fill) ?? $"Here's your kit, {member.Name}: {_what}.", force: true);
            }
        }

        private BotMobile _mine;

        private BotMobile Mine => _mine;

        public override BotDoing Advance(IBotWilful bot)
        {
            if (bot?.Self is not BotMobile body)
            {
                return BotDoing.Failed("no body");
            }

            _mine = body;

            if (Served)
            {
                Uncount();

                if (_units > 0)
                {
                    Drawn++;

                    return BotDoing.Done($"drew a kit from {_guild.Name}'s store from {_qm}: {_what}, {_owed}gp owed to the chest");
                }

                Empty++;

                return BotDoing.Failed($"{_guild.Name}'s store had nothing my pack would take of what I lack");
            }

            if (!BotQuartermaster.Open(_guild, out var qm, out _, out _))
            {
                Uncount();
                Closed++;

                return BotDoing.Failed($"{_guild.Name}'s store shut before I got there");
            }

            if (Core.TickCount - _began >= WalkMs)
            {
                Uncount();
                Late++;

                return BotDoing.Failed($"could not reach {_guild.Name}'s store in {WalkMs / 60000} minutes");
            }

            if (!body.InRange(_at, BotQuartermaster.Counter - 1))
            {
                return BotDoing.Walk(_map, _at, BotArrival.Within(BotQuartermaster.Counter - 2), $"to {_guild.Name}'s store for a kit");
            }

            // At the counter: the quartermaster's look hands it over within a few seconds.
            if (qm.InRange(body, BotQuartermaster.Counter + 2))
            {
                Serve(qm);
            }

            return BotDoing.Work($"waiting at {_guild.Name}'s store for a kit");
        }
    }

    public static string Describe()
    {
        var say = "";

        foreach (var (why, n) in _gates)
        {
            say += (say.Length == 0 ? "" : ", ") + $"{why} {n}";
        }

        return $"{Offered} offered, {Took} taken, {Drawn} drawn, {Empty} found nothing to draw, {Closed} found the store shut, {Late} too late (passed over: {(say.Length == 0 ? "none" : say)})";
    }

    public static void Forget()
    {
        Offered = 0;
        Took = 0;
        Drawn = 0;
        Closed = 0;
        Empty = 0;
        Late = 0;
        _last.Clear();
        _gates.Clear();
    }
}
