using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Offers the holder of a colony's ship, on the colony's land, the run home: when the store holds <see cref="BotColonies.CargoAt"/>
/// units, or a colonist's shift is over — and no oftener than <see cref="BotColonies.RunEveryMs"/>.
/// </summary>
public sealed class BotColonyRunner : IBotProposer
{
    public const string Office = "colonyrun";

    public static long Asked { get; private set; }

    public static long Early { get; private set; }

    public static long Light { get; private set; }

    public static long NoWay { get; private set; }

    public string Name => Office;

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotColonies.Running || !BotSea.Running || !BotSea.Ready || bot?.Self is not BotMobile { Deleted: false, Alive: true } body)
        {
            return null;
        }

        var colony = BotColonies.OnLand(body);

        if (colony == null || body.Guild is not Guild guild || BotHelm.ShipOf(body) == null || body.Squad != null)
        {
            return null;
        }

        if (BotOffice.Busy(Office, guild) || BotColonies.Passage(guild, BotSailing.Way.Home) != null)
        {
            return null;
        }

        Asked++;

        var now = Core.TickCount;

        if (now - colony.LastRun < BotColonies.RunEveryMs)
        {
            Early++;

            return null;
        }

        var due = false;

        foreach (var (_, since) in colony.Shift)
        {
            if (now - since >= BotColonies.ShiftMs)
            {
                due = true;

                break;
            }
        }

        if (!due && colony.Stock() < BotColonies.CargoAt)
        {
            Light++;

            return null;
        }

        if (!BotColonies.HomeWay(body, out var from, out var to, out var lane, out var home))
        {
            NoWay++;

            return null;
        }

        BotOffice.Offering(Office, guild);

        return new BotColonyRun(colony, guild, body, from, to, lane, home);
    }

    public static void Forget()
    {
        Asked = 0;
        Early = 0;
        Light = 0;
        NoWay = 0;
    }
}

/// <summary>
/// The run home: the store's goods into the captain's pack as far as it can carry them, the colonists whose shift is over at its
/// side at the island's dock, the voyage, the ore melted at a mainland fire, the goods sold over the counters that pay best,
/// and what they fetched shared — the captain's share kept, the hands' share into their banks, the rest into the guild's chest.
/// </summary>
public sealed class BotColonyRun : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotColonyRun));

    public const string Trade = "colony-run";

    /// <summary>What a run is reckoned at before the ledger has measured one.</summary>
    public static double Prior { get; set; } = 45.0;

    /// <summary>How many kinds of goods the captain tries a counter for before it calls the selling done.</summary>
    public static int MostKinds { get; set; } = 6;

    public static long Runs { get; private set; }

    public static long Loaded { get; private set; }

    public static long Units { get; private set; }

    public static long Gold { get; private set; }

    public static long ToChest { get; private set; }

    public static long ToCrew { get; private set; }

    public static long ToCaptain { get; private set; }

    public static long Melted { get; private set; }

    public static long Unsold { get; private set; }

    public static long Lost { get; private set; }

    public static long Sailed { get; private set; }

    private enum Step
    {
        Load,
        ToDock,
        Muster,
        Sail,
        Melt,
        Sell,
        Pay
    }

    private readonly BotColony _colony;

    private readonly Guild _guild;

    private readonly BotMobile _captain;

    private readonly BotDock _from;

    private readonly BotDock _to;

    private readonly BotSeaLane _lane;

    private readonly Point3D _home;

    private readonly BotSailing _passage;

    private readonly double _minutes;

    private readonly HashSet<Type> _tried = [];

    private Step _step;

    private BotVoyage _voyage;

    private bool _afloat;

    private int _units;

    private int _gold;

    private Type _kind;

    private BaseVendor _vendor;

    private Point3D _fire;

    private int _worth;

    public BotColonyRun(BotColony colony, Guild guild, BotMobile captain, BotDock from, BotDock to, BotSeaLane lane, Point3D home)
    {
        _colony = colony;
        _guild = guild;
        _captain = captain;
        _from = from;
        _to = to;
        _lane = lane;
        _home = home;

        _passage = new BotSailing
        {
            Guild = guild,
            Captain = captain,
            Heading = BotSailing.Way.Home,
            Map = captain?.Map,
            From = from,
            To = to,
            Goal = to?.Town?.Square ?? to?.Shore ?? Point3D.Zero,
            Colony = colony,
            State = BotSailing.Tide.Mustering,
            Began = Core.TickCount
        };

        var sail = (lane?.Tiles ?? 0) * (double)BotHelm.StrokeMs / Math.Max(1, BotHelm.Speed) / 60000.0;

        _minutes = Math.Max(5.0, sail + BotColonies.MusterMs / 120000.0 + 6.0);
    }

    public BotSailing Passage => _passage;

    /// <summary>The kind the captain carries most worth of: what The Shadow's ambush weighs it by (<c>BotHighway</c>).</summary>
    public Type Goods => _units > 0 ? typeof(IronIngot) : null;

    /// <summary>Units of the store's goods in the captain's pack now.</summary>
    public int Cargo => _units;

    public override string Kind => Trade;

    public override Map Map => _passage.Map;

    public override Point3D Where =>
        _step switch
        {
            Step.Load => _colony.Site,
            Step.ToDock or Step.Muster => _from?.First?.Shore ?? _from?.Shore ?? Point3D.Zero,
            Step.Sail => _to?.First?.Shore ?? _to?.Shore ?? Point3D.Zero,
            Step.Melt when _fire != Point3D.Zero => _fire,
            Step.Sell when _vendor != null => _vendor.Location,
            _ => _to?.Shore ?? Point3D.Zero
        };

    public override double Expects => Prior;

    public override double Minutes => _minutes;

    public override double Coin => 1.0;

    /// <summary>The guild's errand: the goods are the guild's, and the captain keeps only its share. Not refused for paying it little.</summary>
    public override bool Unpaid => true;

    public override bool Steadfast => true;

    public override bool Resumes => true;

    public override double HoldsFor => _minutes;

    /// <summary>From the moment the store's goods are in its pack: a captain taken off now carries the guild's goods to nowhere.</summary>
    public override bool Committed => _step > Step.Load;

    public override bool Still => _step == Step.Muster || _voyage is { Still: true };

    public override int Made => _step < Step.Pay ? _worth : 0;

    public override string Stage =>
        _step switch
        {
            Step.Load => $"loading {_guild?.Name}'s colony store on {_colony.Island}",
            Step.ToDock => $"carrying {_units} units of the colony's goods to the dock at {_from?.Name}",
            Step.Muster => $"at the dock at {_from?.Name} with the goods, {_passage.Present()} of {_passage.Wanted} going-home hands at its side",
            Step.Sail => _voyage?.Stage ?? $"at sea for {_to?.Name} with the colony's goods",
            Step.Melt => "melting the colony's ore at a mainland fire",
            Step.Sell => _vendor != null ? $"selling the colony's {_kind?.Name} to {_vendor.Name}" : "looking for counters for the colony's goods",
            _ => "paying out what the goods fetched"
        };

    public override void Taken(IBotWilful bot)
    {
        BotColonies.Open(_passage);
    }

    public override void Paused(IBotWilful bot) => _voyage?.Paused(bot);

    public override void Resumed(IBotWilful bot) => _voyage?.Resumed(bot);

    public override void Drop(IBotWilful bot)
    {
        BotOffice.Release(BotColonyRunner.Office, _guild);

        if (_passage.State != BotSailing.Tide.Over)
        {
            BotColonies.Close(_passage, _passage.State == BotSailing.Tide.Landed ? "landed" : "the captain put the run down");
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Guild != _guild || _from == null || _to == null || _lane == null)
        {
            return BotDoing.Failed("no captain, no guild, or no way home");
        }

        BotOffice.Hold(BotColonyRunner.Office, _guild, 180000);

        for (var guard = 0; guard < 10; guard++)
        {
            var doing = _step switch
            {
                Step.Load => Load(body),
                Step.ToDock => ToDock(body),
                Step.Muster => Muster(bot, body),
                Step.Sail => Sail(bot, body),
                Step.Melt => Melt(bot, body),
                Step.Sell => Sell(bot, body),
                _ => Pay(body)
            };

            if (doing.Kind != BotDoingKind.None)
            {
                return doing;
            }
        }

        return BotDoing.Failed("could not settle on a next step");
    }

    /// <summary>The store's goods into the pack, bars first (light and dear), then wood, then ore, up to <see cref="BotColonies.CargoLoad"/> of what it can carry.</summary>
    private BotDoing Load(BotMobile body)
    {
        var store = _colony.Store;

        if (store is not { Deleted: false })
        {
            return BotDoing.Failed("the colony's store is gone");
        }

        var at = store.GetWorldLocation();

        if (!body.InRange(at, 2))
        {
            return BotDoing.Walk(body.Map, at, BotArrival.Within(1), $"to {_guild.Name}'s colony store");
        }

        var room = (int)(BotLadder.Ceiling(body) * BotColonies.CargoLoad) - BotLadder.Load(body);

        room = Take<BaseIngot>(body, store, room);
        room = Take<Board>(body, store, room);
        room = Take<Log>(body, store, room);
        Take<BaseOre>(body, store, room);

        _colony.LastRun = Core.TickCount;

        // Who goes home with it: every colonist whose shift is over.
        var wanted = 0;
        var now = Core.TickCount;

        foreach (var (serial, since) in _colony.Shift)
        {
            if (serial != body.Serial && now - since >= BotColonies.ShiftMs)
            {
                wanted++;
            }
        }

        _passage.Wanted = Math.Min(Math.Max(0, BotColonies.PartySize - 1), wanted);
        _passage.Least = 0;

        var mine = _colony.Shift.TryGetValue(body.Serial, out var mineSince) && now - mineSince >= BotColonies.ShiftMs;

        if (_units <= 0 && wanted == 0 && !mine)
        {
            return BotDoing.Failed("the store held nothing it could carry, and nobody's shift is over");
        }

        Loaded++;
        _worth = Worth(body);

        logger.Information(
            "{Name} of {Guild} loaded {Units} units of the colony's goods on {Island} (about {Worth}gp) to sail home from {From} to {To}, {Hands} hands' shifts over",
            body.Name,
            _guild.Name,
            _units,
            _colony.Island,
            _worth,
            _from.Name,
            _to.Name,
            wanted
        );

        BotVoice.Say(body, "guild", $"The ship sails home from {_colony.Island} with {_units} loads of our goods. Whoever's shift is done, to the dock.", true);

        _step = Step.ToDock;

        return default;
    }

    /// <summary>Lifts up to the room's weight of one kind out of the store into the pack. Returns the room left.</summary>
    private int Take<T>(BotMobile body, Container store, int room) where T : Item
    {
        var pack = body.Backpack;

        if (pack == null || room <= 0)
        {
            return room;
        }

        List<Item> held = [.. store.Items];

        for (var i = 0; i < held.Count && room > 0; i++)
        {
            if (held[i] is not T item || item.Deleted)
            {
                continue;
            }

            var each = Math.Max(0.1, item.Weight);
            var fits = (int)Math.Floor(room / each);

            if (fits <= 0)
            {
                continue;
            }

            if (fits < item.Amount && Mobile.LiftItemDupe(item, fits) == null)
            {
                continue;
            }

            var amount = item.Amount;

            BotSurplus.Put(body, pack, item);

            _units += amount;
            room -= (int)Math.Ceiling(amount * each);
        }

        return room;
    }

    private BotDoing ToDock(BotMobile body)
    {
        var berth = _from.First;

        if (berth == null)
        {
            return BotDoing.Failed($"{_from.Name} has no berth");
        }

        if (!body.InRange(berth.Shore, 2) || Math.Abs(body.Z - berth.Shore.Z) > 16)
        {
            return BotDoing.Walk(body.Map, berth.Shore, BotArrival.Within(1), $"with the colony's goods to the dock at {_from.Name}");
        }

        _passage.AtDock = Core.TickCount;
        _step = Step.Muster;

        return default;
    }

    private BotDoing Muster(IBotWilful bot, BotMobile body)
    {
        var present = _passage.Present();
        var waited = Core.TickCount - _passage.AtDock;

        if (present < _passage.Wanted && waited < BotColonies.MusterMs / 2)
        {
            return BotDoing.Work($"at the dock at {_from.Name}: {present} of {_passage.Wanted} going-home hands at its side");
        }

        _passage.Aboard.Clear();

        for (var i = 0; i < _passage.Roster.Count; i++)
        {
            if (_passage.Roster[i] != body && _passage.AtHand(_passage.Roster[i]))
            {
                _passage.Aboard.Add(_passage.Roster[i]);
            }
        }

        _voyage = new BotVoyage(body.Map, _from, _to, _lane, _to.Town, _passage.Goal, $"{_guild.Name}'s colony on {_colony.Island}", false, _passage.Aboard);
        _voyage.Taken(bot);
        _step = Step.Sail;

        return default;
    }

    private BotDoing Sail(IBotWilful bot, BotMobile body)
    {
        var doing = _voyage.Advance(bot);

        // Afloat: the muster is closed, and The Shadow hears of a loaded ship and may wait for it at the quay it makes for. See BotPiracy.
        if (!_afloat && BotHelm.Of(body) != null)
        {
            _afloat = true;
            _passage.State = BotSailing.Tide.Sailing;
            Sailed++;
            BotPiracy.Sailing(_passage, _units, _worth);
        }

        switch (doing.Kind)
        {
            case BotDoingKind.Done:
                {
                    _passage.State = BotSailing.Tide.Landed;
                    _step = Step.Melt;
                    _colony.Runs++;
                    Runs++;

                    return default;
                }
            case BotDoingKind.Failed:
                {
                    Lost++;

                    return BotDoing.Failed($"the run home's voyage failed: {doing.Note}");
                }
            default:
                {
                    // Ashore at the far dock with a party of The Shadow lying in wait there: the crew forms up round the captain.
                    if (_afloat && _passage.State == BotSailing.Tide.Sailing && BotHelm.Of(body) == null)
                    {
                        _passage.State = BotSailing.Tide.Landed;
                        BotPiracy.Landing(_passage);
                    }

                    return doing;
                }
        }
    }

    /// <summary>The ore melted at the nearest fire on this land, if there is one: a shopkeeper buys bars, never ore.</summary>
    private BotDoing Melt(IBotWilful bot, BotMobile body)
    {
        if (BotOre.Carried(body) < BotOre.WorthSmelting)
        {
            _step = Step.Sell;

            return default;
        }

        if (_fire == Point3D.Zero)
        {
            _fire = BotGround.Fire(bot, body.Location);

            if (_fire == Point3D.Zero || !BotGates.Joined(body.Map, body.Location, _fire))
            {
                _fire = Point3D.Zero;
                _step = Step.Sell;

                return default;
            }
        }

        if (!body.InRange(_fire, BotOre.FireReach))
        {
            return BotDoing.Walk(body.Map, _fire, BotArrival.Within(BotOre.FireReach), "with the colony's ore to a fire");
        }

        var made = BotOre.Melt(body);

        Melted += Math.Max(0, made);
        _step = Step.Sell;

        return default;
    }

    /// <summary>Kind by kind, the counter that pays best for it once the road is paid for (<see cref="BotShops.Buyer(IBotWilful, Item, int, bool, out int, out double, out int)"/>), and the sale.</summary>
    private BotDoing Sell(IBotWilful bot, BotMobile body)
    {
        var pack = body.Backpack;

        if (pack == null)
        {
            _step = Step.Pay;

            return default;
        }

        if (_kind == null || _vendor is not { Deleted: false })
        {
            _kind = null;
            _vendor = null;

            if (_tried.Count >= MostKinds)
            {
                _step = Step.Pay;

                return default;
            }

            Item sample = null;

            for (var i = 0; i < pack.Items.Count && sample == null; i++)
            {
                if (pack.Items[i] is { Deleted: false, Movable: true } item && item is BaseIngot or Log or Board && !_tried.Contains(item.GetType()))
                {
                    sample = item;
                }
            }

            if (sample == null)
            {
                _step = Step.Pay;

                return default;
            }

            _kind = sample.GetType();
            _tried.Add(_kind);
            _vendor = BotShops.Buyer(bot, sample, pack.GetAmount(_kind), false, out _, out _, out _);

            if (_vendor == null)
            {
                _kind = null;

                return default;
            }
        }

        if (!body.InRange(_vendor.Location, BotShops.CounterReach))
        {
            return BotDoing.Walk(body.Map, _vendor, BotArrival.Within(BotShops.CounterReach), $"with the colony's {_kind.Name} to {_vendor.Name}");
        }

        List<Item> goods = [];

        for (var i = 0; i < pack.Items.Count; i++)
        {
            if (pack.Items[i] is { Deleted: false } item && item.GetType() == _kind)
            {
                goods.Add(item);
            }
        }

        var earned = BotShops.Sell(bot, _vendor, goods, out var units);

        if (earned > 0)
        {
            _gold += earned;

            logger.Information("{Name} sold {Units} {Kind} of {Guild}'s colony to {Vendor} for {Gold}gp", body.Name, units, _kind.Name, _guild.Name, _vendor.Name, earned);
        }

        _kind = null;
        _vendor = null;

        return default;
    }

    /// <summary>
    /// What the goods fetched, shared: the captain keeps its share, the hands who came home with it have theirs put into their
    /// banks, and the rest goes into the guild's chest. Coin moved, none made: it is the counters' gold, out of the captain's
    /// purse — the pack first, then the account.
    /// </summary>
    private BotDoing Pay(BotMobile body)
    {
        var unsold = 0;
        var pack = body.Backpack;

        for (var i = 0; pack != null && i < pack.Items.Count; i++)
        {
            if (pack.Items[i] is { Deleted: false } item && item is BaseIngot or BaseOre or Log or Board)
            {
                unsold += item.Amount;
            }
        }

        Unsold += unsold;

        var total = _gold;
        var captainShare = (int)(total * Math.Clamp(BotColonies.CaptainShare, 0.0, 1.0));
        var crewShare = (int)(total * Math.Clamp(BotColonies.CrewShare, 0.0, 1.0 - Math.Clamp(BotColonies.CaptainShare, 0.0, 1.0)));
        List<Mobile> hands = [];

        for (var i = 0; i < _passage.Aboard.Count; i++)
        {
            if (_passage.Aboard[i] is BotMobile { Deleted: false } hand && hand != body)
            {
                hands.Add(hand);
            }
        }

        if (hands.Count == 0)
        {
            crewShare = 0;
        }

        var chest = total - captainShare - crewShare;
        var paidCrew = 0;

        if (crewShare > 0)
        {
            var each = crewShare / hands.Count;

            for (var i = 0; i < hands.Count && each > 0; i++)
            {
                if (Hand(body, hands[i], each))
                {
                    paidCrew += each;
                }
            }
        }

        // Whatever the hands were not paid goes to the chest with the rest.
        chest += crewShare - paidCrew;

        var paidChest = 0;

        // Not booked aside: the run is the guild's errand (Unpaid), and what it is measured at is the captain's own share, which is
        // also all the guild's dues are taken on when it settles.
        if (chest > 0 && Withdraw(body, chest))
        {
            BotChest.Put(_guild.Name, chest);
            paidChest = chest;
        }

        Gold += total;
        ToChest += paidChest;
        ToCrew += paidCrew;
        ToCaptain += total - paidChest - paidCrew;
        Units += _units;

        _colony.CargoUnits += _units;
        _colony.CargoGold += total;
        _colony.ChestGold += paidChest;
        _colony.CrewGold += paidCrew;
        _colony.CaptainGold += total - paidChest - paidCrew;
        _colony.Last = $"a run home by {body.Name}: {_units} units, {total}gp";

        logger.Information(
            "{Name} brought {Units} units of {Guild}'s colony goods home from {Island}: sold for {Gold}gp — {Chest} into the chest, {Crew} to {Hands} hands, {Captain} kept; {Unsold} units unsold",
            body.Name,
            _units,
            _guild.Name,
            _colony.Island,
            total,
            paidChest,
            paidCrew,
            hands.Count,
            total - paidChest - paidCrew,
            unsold
        );

        BotVoice.Say(body, "guild", $"The colony's goods fetched {total}gp: {paidChest} is in the chest, {paidCrew} for the hands.", true);

        return BotDoing.Done($"ran {_units} units of the colony's goods home and sold them for {total}gp, {paidChest} to the chest");
    }

    /// <summary>Coin out of the captain's purse, the pack first and then the account. False, with nothing moved, when it is not there.</summary>
    private static bool Withdraw(BotMobile body, int gold)
    {
        var pack = body.Backpack;

        if (pack == null || gold <= 0 || BotYield.Spendable(body) < gold)
        {
            return false;
        }

        var fromPack = Math.Min(pack.GetAmount(typeof(Gold)), gold);
        var fromBank = gold - fromPack;

        if (fromPack > 0 && !pack.ConsumeTotal(typeof(Gold), fromPack))
        {
            return false;
        }

        if (fromBank > 0 && !Banker.Withdraw(body, fromBank))
        {
            if (fromPack > 0)
            {
                pack.DropItem(new Gold(fromPack));
            }

            return false;
        }

        return true;
    }

    /// <summary>One hand's share: out of the captain's purse into the hand's account, kept out of the hand's work in hand (its shift is what earned it).</summary>
    private static bool Hand(BotMobile captain, Mobile hand, int gold)
    {
        if (!Withdraw(captain, gold))
        {
            return false;
        }

        if (!Banker.Deposit(hand, gold))
        {
            // Back where it came from rather than lost.
            Banker.Deposit(captain, gold);

            return false;
        }

        BotYield.Aside(hand, -gold);

        return true;
    }

    /// <summary>What the cargo in the pack is worth by the market, for the ledger's "made" while it is carried.</summary>
    private static int Worth(BotMobile body)
    {
        var pack = body.Backpack;
        var worth = 0;

        for (var i = 0; pack != null && i < pack.Items.Count; i++)
        {
            if (pack.Items[i] is { Deleted: false } item && item is BaseIngot or Log or Board or BaseOre)
            {
                worth += item.Amount * BotCrime.Worth(item.GetType(), item is BaseOre ? 2 : 3);
            }
        }

        return worth;
    }

    public static string Describe() =>
        Loaded == 0
            ? "no run home yet"
            : $"runs home: {Loaded} loaded, {Sailed} sailed, {Runs} landed, {Lost} lost; {Units} units sold for {Gold}gp ({ToChest} to chests, {ToCrew} to hands, {ToCaptain} to captains), {Melted} bars melted on the mainland, {Unsold} units unsold; the runner asked {BotColonyRunner.Asked}: {BotColonyRunner.Early} too soon after the last, {BotColonyRunner.Light} too light with no shift over, {BotColonyRunner.NoWay} with no way home";

    public static void Forget()
    {
        Runs = 0;
        Loaded = 0;
        Units = 0;
        Gold = 0;
        ToChest = 0;
        ToCrew = 0;
        ToCaptain = 0;
        Melted = 0;
        Unsold = 0;
        Lost = 0;
        Sailed = 0;
        BotColonyRunner.Forget();
    }
}
