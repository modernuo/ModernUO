using System;
using Server.Guilds;
using Server.Logging;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// The sea as the rest of the population sees it: which towns a traveller may sail to, the dock it sets out from, whether
/// it can have a ship, and the one summary line.
///
/// <para>
/// <b>Patrick's point 7 of 29.09.2026: "teach the bots to travel by sea, on boats."</b> The traveller offers towns a road
/// reaches (<see cref="BotTraveller"/>); asked first, this offers — by <see cref="Share"/> for a bot on home's land, always
/// for a bot on an island — a town no road reaches whose dock has a lane from a dock the bot can walk to. On an island the
/// sea is the only way anywhere, so a bot there travels by sea or not at all, and when nothing on the island is worth doing
/// it is offered the way home by sea (<see cref="BotSeafarer"/>).
/// </para>
///
/// <para>
/// <b>A ship costs what the shopkeepers ask: 10,177 gold for a small one, from a shipwright or a provisioner.</b> On a world
/// wiped that morning the middling purse was 66 gold and the fattest 9,850 (18:58 on 29.09.2026), so a guild stands what a
/// member lacks (<see cref="GuildPays"/>, the way supplies are funded — <see cref="BotProvision.Fund"/>), and a ship once
/// bought is kept as the bot's own and sails every voyage after. <see cref="Grant"/> hands one over for nothing, for proving
/// the voyage on a young world; it is off.
/// </para>
/// </summary>
public static class BotSea
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSea));

    public static bool Running { get; set; } = true;

    /// <summary>
    /// The chance that a traveller on home's land who could sail is offered a town across the sea rather than one by road. A
    /// quarter: six sea towns against ten road towns, and a voyage is a quarter of an hour at the least.
    /// </summary>
    public static double Share { get; set; } = 0.25;

    /// <summary>The longest voyage offered, in tiles of walk and sea together. Britain to Skara Brae round the south is 5,568.</summary>
    public static int Furthest { get; set; } = 8000;

    /// <summary>Whether a bot's guild stands what it lacks for a ship, from its chest and its members, as for supplies.</summary>
    public static bool GuildPays { get; set; } = true;

    /// <summary>Whether a bot that cannot afford a ship is handed one for nothing. For trials on a young world; off.</summary>
    public static bool Grant { get; set; }

    /// <summary>What a small ship costs where no shopkeeper selling one is known yet: <c>SBShipwright</c>'s and <c>SBProvisioner</c>'s price.</summary>
    public static int ListPrice { get; set; } = 10177;

    /// <summary>
    /// An order by hand, through the debugger's door: <c>dial BotSea.Order Adela to Moonglow</c> presses that bot to sail
    /// there now (<see cref="BotWill.Press"/>), whatever it was doing, and the answer is in the log. Read and cleared by the
    /// survey's clock.
    /// </summary>
    public static string Order { get; set; } = "";

    public static bool Ready => BotSeaLanes.Ready;

    public static long Asked { get; private set; }

    public static long Offered { get; private set; }

    public static long Nowhere { get; private set; }

    public static long Unafforded { get; private set; }

    public static long Islanders { get; private set; }

    public static long Ordered { get; private set; }

    /// <summary>
    /// Travellers not offered a voyage because they hold no ship and the journey would have had to buy one. See
    /// <see cref="Owns"/>: a journey pays nothing, so no voyage on this island pays a ship back.
    /// </summary>
    public static long Shipless { get; private set; }

    /// <summary>Colonists on their colony's island not offered a journey by sea: the ship they may hold is the colony's (07.10.2026).</summary>
    public static long ColonyKept { get; private set; }

    /// <summary>Lanes passed over by a traveller or a trader because they have lost more voyages than they delivered. See <see cref="Shut"/>.</summary>
    public static long LanesShut { get; private set; }

    /// <summary>
    /// Whether this bot holds a ship of its own (or one is handed over, <see cref="Grant"/>): the only way a journey or a trader's
    /// haul sails.
    ///
    /// <para>
    /// <b>No voyage on this island pays a ship back (06.10.2026, money15).</b> On 04.10 (build 400, 04:20–15:37) nine small ships
    /// were bought at 10,177gp each and three more in the evening, 122,124gp in all; every one was bought by "took on voyage:
    /// looking for a ship", the traveller's journey (<see cref="Travel"/>), which is worth eight a minute of curiosity and pays no
    /// coin (the six arrivals: "0 coin" each). Not one was bought for a trader's haul. The ships did not last: thirteen lie at
    /// anchor, twelve of them on the way to Nujel'm ((3514–3546, 1066–1087), (3346, 1246)) and one at (3012, 748), left there when the captain died
    /// aboard or was wrecked and the engine refused the dry-dock for want of the key ("NoKey"), so the next journey bought
    /// another (Norna twice in seven hours). A ship is no longer bought for a voyage that pays nothing: a journey sails only
    /// in a ship the bot holds. The one buyer left is the way home from an island (<see cref="BotSeafarer"/>), where the sea is
    /// the only way off and nothing else will take the bot home, and an order by hand.
    /// </para>
    /// </summary>
    public static bool Owns(Mobile body) => body != null && (Grant || BotHelm.ShipOf(body) != null);

    /// <summary>
    /// Whether a lane has lost more voyages than it has delivered — a death aboard, a wreck, a ship put back — and is passed over
    /// by those who choose to sail. The memory is the session's (<see cref="BotSeaLane.Lost"/>): the island's measure of a way
    /// that kills its sailors, as the failure of a road is remembered against the road.
    /// </summary>
    public static bool Shut(BotSeaLane lane) => lane != null && lane.Lost > lane.Sailed;

    /// <summary>
    /// The traveller's hook. True when the sea has decided — <paramref name="voyage"/> is the voyage, or null for "nothing" —
    /// and false to leave the choice to the roads as before. A bot at sea is decided (nothing); a bot on home's land is
    /// asked by <see cref="Share"/>; a bot on an island always.
    /// </summary>
    public static bool Travel(IBotWilful bot, BotTowns.Town here, string from, out BotDeed voyage)
    {
        voyage = null;

        var body = bot?.Self as BotMobile;
        var map = body?.Map;

        if (!Running || !Ready || body == null || map == null || map != BotSeaChart.Map)
        {
            return false;
        }

        if (BotHelm.Of(body) != null)
        {
            return true;
        }

        // A colonist on its colony's island holding the colony's ship sails it home with the goods, not on a journey (sea20).
        if (BotColonies.OnShift(body))
        {
            ColonyKept++;

            return true;
        }

        var ashore = BotGates.Joined(map, body.Location, BotPopulation.Where);

        if (ashore && Utility.RandomDouble() >= Share)
        {
            return false;
        }

        Asked++;

        if (!ashore)
        {
            Islanders++;
        }

        // A journey pays no coin, so it never buys a ship: only a bot that holds one is offered the sea. See Owns. Asked
        // before the lanes are weighed, which also spares the planning for the many who hold none.
        if (!Owns(body))
        {
            Shipless++;

            return !ashore;
        }

        var towns = BotTowns.All;
        Span<double> weights = stackalloc double[towns.Count];
        var plans = new (BotDock From, BotSeaLane Lane)[towns.Count];
        var total = 0.0;

        for (var i = 0; i < towns.Count; i++)
        {
            weights[i] = 0.0;

            var town = towns[i];
            var dock = BotDocks.Of(town);

            if (town == here || dock == null || BotGates.Joined(map, body.Location, dock.Shore))
            {
                continue;
            }

            if (!Plan(map, body.Location, dock, out var start, out var lane, out var tiles, true) || tiles > Furthest)
            {
                continue;
            }

            plans[i] = (start, lane);
            weights[i] = 1.0 / (1.0 + lane.Sailed / Math.Max(0.1, BotTraveller.Curiosity));
            total += weights[i];
        }

        if (total <= 0.0)
        {
            Nowhere++;

            return !ashore;
        }

        var roll = Utility.RandomDouble() * total;
        var picked = -1;

        for (var i = 0; i < towns.Count && picked < 0; i++)
        {
            roll -= weights[i];

            if (weights[i] > 0.0 && roll <= 0.0)
            {
                picked = i;
            }
        }

        if (picked < 0)
        {
            for (var i = towns.Count - 1; i >= 0 && picked < 0; i--)
            {
                if (weights[i] > 0.0)
                {
                    picked = i;
                }
            }
        }

        var target = towns[picked];

        Offered++;
        voyage = new BotVoyage(map, plans[picked].From, BotDocks.Of(target), plans[picked].Lane, target, target.Square, from, false);

        return true;
    }

    /// <summary>
    /// The dock to set out from for <paramref name="to"/>: of the docks a walker at <paramref name="at"/> can reach, the one
    /// that makes walk and voyage together shortest — a bot at Britain bound for Skara Brae walks to Yew's dock rather than
    /// sail round the continent. <paramref name="tiles"/> is the walk plus the lane. <paramref name="wary"/>: a lane that has
    /// lost more voyages than it delivered is passed over (<see cref="Shut"/>) — by those who choose to sail, not by a bot whose
    /// only way home is the sea.
    /// </summary>
    public static bool Plan(Map map, Point3D at, BotDock to, out BotDock from, out BotSeaLane lane, out int tiles, bool wary = false)
    {
        from = null;
        lane = null;
        tiles = int.MaxValue;

        var docks = BotDocks.All;

        for (var i = 0; i < docks.Count; i++)
        {
            var dock = docks[i];

            if (dock == to || !BotGates.Joined(map, at, dock.Shore))
            {
                continue;
            }

            var way = BotSeaLanes.Between(dock, to);

            if (way == null)
            {
                continue;
            }

            if (wary && Shut(way))
            {
                LanesShut++;

                continue;
            }

            // A walker makes about as many tiles a second as a ship: running bots step every 200ms, the ship three tiles
            // every 750ms. Counted alike.
            var cost = BotHelm.Away(at, dock.Shore) + way.Tiles;

            if (cost < tiles)
            {
                tiles = cost;
                from = dock;
                lane = way;
            }
        }

        return from != null;
    }

    /// <summary>
    /// Whether this bot has a ship or could have one: in its pack, within its purse, within what its guild could stand, or
    /// handed over (<see cref="Grant"/>). Asked before a voyage is offered, so a bot is not sent to a shipwright to be
    /// turned away.
    /// </summary>
    public static bool CanSail(BotMobile body)
    {
        if (body == null)
        {
            return false;
        }

        if (Grant || BotHelm.ShipOf(body) != null)
        {
            return true;
        }

        var vendor = BotShops.Nearest((Mobile)body, typeof(SmallBoatDeed));
        var price = vendor != null ? BotShops.Price(vendor, typeof(SmallBoatDeed)) : ListPrice;

        if (price <= 0)
        {
            price = ListPrice;
        }

        var have = BotYield.Spendable(body);

        if (have >= price)
        {
            return true;
        }

        if (!GuildPays || body.Guild is not Guild guild)
        {
            return false;
        }

        // What the guild could stand, as BotProvision.Fund would draw it: the chest above what it saves for (BotDues.Spare),
        // then each member's purse above what it keeps back.
        var could = have + BotDues.Spare(guild);

        for (var i = 0; i < guild.Members.Count && could < price; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false } mate && mate != body)
            {
                could += Math.Max(0, BotYield.Spendable(mate) - BotGuilds.Keep);
            }
        }

        return could >= price;
    }

    /// <summary>The order by hand, if one was written. See <see cref="Order"/>.</summary>
    public static void Obey()
    {
        var order = Order;

        if (string.IsNullOrWhiteSpace(order) || !Ready)
        {
            return;
        }

        Order = "";

        var split = order.IndexOf(" to ", StringComparison.OrdinalIgnoreCase);
        var who = split > 0 ? order[..split].Trim() : order.Split(' ', 2)[0].Trim();
        var where = split > 0 ? order[(split + 4)..].Trim() : order.Split(' ', 2).Length > 1 ? order.Split(' ', 2)[1].Trim() : "";

        BotMobile bot = null;
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count && bot == null; i++)
        {
            if (bots[i] is { Deleted: false } one && one.Name != null && one.Name.StartsWith(who, StringComparison.OrdinalIgnoreCase))
            {
                bot = one;
            }
        }

        var town = BotTowns.Find(where);
        var dock = town != null ? BotDocks.Of(town) : null;

        if (bot == null || town == null || dock == null)
        {
            logger.Information("Sea: the order \"{Order}\" names {Problem}", order, bot == null ? "no bot" : town == null ? "no town" : $"a town with no dock, {town.Name}");

            return;
        }

        if (!Plan(bot.Map, bot.Location, dock, out var from, out var lane, out var tiles))
        {
            logger.Information("Sea: the order \"{Order}\": no dock {Name} can walk to has a lane to {Town}", order, bot.Name, town.Name);

            return;
        }

        var here = BotTowns.Nearest(bot.Location);
        // An order by hand may buy the ship it needs: somebody asked for this voyage by name.
        var voyage = new BotVoyage(bot.Map, from, dock, lane, town, town.Square, here?.Name ?? "the wild", false, null, true);

        if (BotWill.Press(bot, voyage, "ordered to sea by hand (BotSea.Order)"))
        {
            Ordered++;

            logger.Information("Sea: {Name} is ordered to sail from {From} to {Town}, {Tiles} tiles of walk and sea", bot.Name, from.Name, town.Name, tiles);
        }
    }

    /// <summary>The summary line: the three numbers that matter first, then every part's own account.</summary>
    public static string Describe() =>
        !Running
            ? "the sea is off"
            : $"{BotHelm.Embarked} voyages begun, {BotHelm.Arrived} arrived, {BotHelm.Lost} lost; {BotVoyage.Bought} ships bought for {BotVoyage.BoughtGold}gp, {BotVoyage.Granted} handed over; "
              + $"{BotSeaChart.Describe()}; {BotDocks.Describe()}; {BotSeaLanes.Describe()}; asked {Asked} travellers ({Islanders} on an island): {Shipless} holding no ship, which a journey does not buy (it pays 0gp: no voyage here pays a ship back), {Offered} offered a voyage, {Nowhere} with nowhere to sail, {ColonyKept} colonists kept to their colony; {LanesShut} lanes passed over for losing more voyages than they delivered ({ShutLanes()}); "
              + $"{BotSeafarer.Describe()}; {Ordered} ordered by hand; {BotVoyage.Describe()}; {BotHelm.Describe()}; {BotSeaCourse.Describe()}; {BotColonies.Describe()}";

    /// <summary>The lanes shut now, by their docks and their record, for the line.</summary>
    private static string ShutLanes()
    {
        var lanes = BotSeaLanes.All;
        var named = new System.Text.StringBuilder();

        for (var i = 0; i < lanes.Count; i++)
        {
            var lane = lanes[i];

            if (!Shut(lane))
            {
                continue;
            }

            if (named.Length > 0)
            {
                named.Append(", ");
            }

            named.Append($"{lane.A?.Name}–{lane.B?.Name} {lane.Lost} lost to {lane.Sailed} arrived");
        }

        return named.Length == 0 ? "none shut" : named.ToString();
    }

    public static void Forget()
    {
        Asked = 0;
        Offered = 0;
        Nowhere = 0;
        Unafforded = 0;
        Shipless = 0;
        LanesShut = 0;
        ColonyKept = 0;
        Islanders = 0;
        Ordered = 0;
        Order = "";
        BotSeafarer.Forget();
        BotColonies.Forget();
        BotVoyage.Forget();
        BotHelm.Forget();
        BotSeaLanes.Forget();
        BotSeaCourse.Forget();
        BotDocks.Forget();
        BotSeaChart.Forget();
    }
}

/// <summary>
/// Offers a bot on an island — ground no road joins to its home — the way home by sea, once it has had nothing worth doing
/// for as long as the walk home waits (<see cref="BotHomeward.BarrenFor"/>). The walk home cannot be offered there: its
/// place is on another land and the appraisal refuses it.
/// </summary>
public sealed class BotSeafarer : IBotProposer
{
    public static long Asked { get; private set; }

    public static long OnHome { get; private set; }

    public static long Busy { get; private set; }

    public static long NoWay { get; private set; }

    public static long Unafforded { get; private set; }

    public static long Sent { get; private set; }

    /// <summary>Colonists on their colony's island, not offered the way home: the colony's ship takes them at their shift's end.</summary>
    public static long Colonists { get; private set; }

    public string Name => "Seafarer";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self as BotMobile;
        var map = body?.Map;

        if (!BotSea.Running || !BotSea.Ready || body == null || !body.Alive || map == null || map != BotSeaChart.Map)
        {
            return null;
        }

        if (bot is IBotSquadMember { Squad: not null } || BotDelveParty.Delving(body) || BotDungeon.Under(body.Location) || BotHelm.Of(body) != null)
        {
            return null;
        }

        // A colonist on its colony's island is at work there, not stranded: it goes home on the colony's own ship (BotColonyRun).
        if (BotColonies.OnShift(body))
        {
            Colonists++;

            return null;
        }

        Asked++;

        var hearth = BotSeat.Home(body);

        if (BotGates.Joined(map, body.Location, hearth))
        {
            OnHome++;

            return null;
        }

        if (bot.Resolve?.Urges?.BarrenMinutes(Core.TickCount) < BotHomeward.BarrenFor)
        {
            Busy++;

            return null;
        }

        BotDock start = null;
        BotDock end = null;
        BotSeaLane lane = null;
        var best = int.MaxValue;
        var docks = BotDocks.All;

        for (var i = 0; i < docks.Count; i++)
        {
            var dock = docks[i];

            if (!BotGates.Joined(map, dock.Shore, hearth) || !BotSea.Plan(map, body.Location, dock, out var from, out var way, out var tiles))
            {
                continue;
            }

            var cost = tiles + BotHelm.Away(dock.Shore, hearth);

            if (cost < best)
            {
                best = cost;
                start = from;
                end = dock;
                lane = way;
            }
        }

        if (end == null)
        {
            NoWay++;

            return null;
        }

        if (!BotSea.CanSail(body))
        {
            Unafforded++;

            return null;
        }

        Sent++;

        // The way home from an island may still buy its ship: the sea is the only way off, and a bot left there has nothing.
        return new BotVoyage(map, start, end, lane, null, hearth, BotTowns.Nearest(body.Location)?.Name ?? "the wild", true, null, true);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been asked to sail home"
            : $"{Asked} asked to sail home: {OnHome} on home's land, {Busy} not yet out of work, {NoWay} with no dock and lane home, {Unafforded} without the means of a ship, {Sent} sent; {Colonists} colonists left to their colony's ship";

    public static void Forget()
    {
        Asked = 0;
        OnHome = 0;
        Busy = 0;
        NoWay = 0;
        Unafforded = 0;
        Sent = 0;
        Colonists = 0;
    }
}
