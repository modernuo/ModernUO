using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Buying things from the shopkeepers. The capability, available to every bot rather than to a trade.
///
/// <para>
/// <b>A purchase goes through the shopkeeper's own <c>OnBuyItems</c>.</b> That is the same call a player's
/// shop window makes: it charges the shard's real prices out of the pack and the account behind it, and
/// hands over real goods. A bot is a customer on exactly the terms a player is, which is worth more than it
/// sounds — every price scalar, every access rule and every stock limit applies without being reimplemented,
/// and nothing here can accidentally invent goods or money.
/// </para>
///
/// <para>
/// <b>Two lines that are not obvious and both were paid for in the first version.</b> Shelves refill on a
/// timer that is only wound when somebody <em>opens</em> the shop window — and bots never open one, so a
/// shop a bot has cleaned out stays empty for good unless the restock is asked for. And prices carry the
/// shard's own scalars, brought up to date only on demand.
/// </para>
/// </summary>
public static class BotShops
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotShops));

    /// <summary>How far around a bot one sweep looks for shopkeepers.</summary>
    public static int Reach { get; set; } = 160;

    /// <summary>How near a shopkeeper a bot has to stand to trade with it.</summary>
    public static int CounterReach { get; set; } = 3;

    /// <summary>
    /// How many shopkeepers the population may remember.
    ///
    /// <para>
    /// <b>Ninety-six was Britain's alone (29.09.2026, residences).</b> At the boot of 18:38 the list was full eight sweeps in,
    /// every one of them round Britain (49, 33, 7 …, "now 96"), and the sweep round Cove at (2257, 1207) found "0 shopkeepers"
    /// with seven standing there — so a bot living in any other town was sent back to Britain for a pair of scissors. The
    /// spawn files put some 230 shopkeepers in the six mainland towns; two hundred and fifty-six holds them all, and the cost
    /// is one pass over the list per purchase chosen.
    /// </para>
    ///
    /// <para>
    /// <b>1024 since build 323:</b> the moongates (317) and the sea (321) put the island towns inside the population's reach
    /// (<c>BotPopulation.Within</c>), and 256 was full two minutes into 323's boot — "Found 0 shopkeepers within 160 tiles of
    /// (1279, 1772) (now 256)".
    /// </para>
    /// </summary>
    public static int MaxShops { get; set; } = 1024;

    private static readonly List<BaseVendor> _shops = [];

    private static readonly List<(Map Map, Point3D Where)> _swept = [];

    public static IReadOnlyList<BaseVendor> Shops => _shops;

    public static long Bought { get; private set; }

    public static long Spent { get; private set; }

    public static long Sold { get; private set; }

    /// <summary>Gold this population has brought into the world over a counter. The only faucet there is.</summary>
    public static long Earned { get; private set; }

    /// <summary>Shopkeepers passed over because the shard has already proved there is no way through to them.</summary>
    public static long Walled { get; private set; }

    /// <summary>Counters passed over as on another land, or in a pocket no gate leaves. See <see cref="Nearest(IBotWilful, Type, Point3D)"/> (build 386).</summary>
    public static long Unjoined { get; private set; }

    /// <summary>
    /// How many times one errand may be sent on to another shopkeeper when the shelf it walked to has emptied.
    ///
    /// <para>
    /// <b>A shelf emptied by the bot in front is not a failed trade.</b> Every restart puts every alchemist on the island
    /// at the same nearest counter within the first second: on builds 41 and 42 fourteen of them failed "the shelf holds
    /// no Bottle at any price" in the boot's first minute, and glass and scrolls together came to about twenty-seven such
    /// failures a boot. <see cref="Nearest(IBotWilful, Type)"/> already passes an empty shelf over, so the next choice is a
    /// shelf with stock; two goes, because a herd that empties one shelf can empty a second.
    /// </para>
    /// </summary>
    public static int RepickLimit { get; set; } = 2;

    /// <summary>Errands sent on to another shopkeeper because the shelf emptied before they arrived. See <see cref="RepickLimit"/>.</summary>
    public static long Repicked { get; private set; }

    /// <summary>
    /// The shopkeeper to try next, when the one an errand walked to has just refused for an empty shelf; null when the
    /// refusal was for anything else, when the errand has had its goes, or when nobody else has it.
    /// </summary>
    public static BaseVendor Next(IBotWilful bot, BaseVendor emptied, Type wanted, ref int tries)
    {
        if (tries >= RepickLimit || emptied == null || wanted == null || Sells(emptied, wanted, out _))
        {
            return null;
        }

        var next = Nearest(bot, wanted);

        if (next == null || next == emptied)
        {
            return null;
        }

        tries++;
        Repicked++;

        return next;
    }

    /// <summary>Whether this patch of the world has already been swept for shopkeepers.</summary>
    public static bool Swept(Map map, Point3D around)
    {
        for (var i = 0; i < _swept.Count; i++)
        {
            if (_swept[i].Map == map && Utility.InRange(_swept[i].Where, around, Reach / 2))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Writes down every shopkeeper within reach. One spatial query — shopkeepers are mobiles, so unlike
    /// forges they answer one.
    /// </summary>
    public static int Survey(Map map, Point3D around)
    {
        if (map == null || map == Map.Internal || Swept(map, around))
        {
            return 0;
        }

        _swept.Add((map, around));

        var found = 0;

        foreach (var vendor in map.GetMobilesInRange<BaseVendor>(around, Reach))
        {
            if (vendor.Deleted || _shops.Count >= MaxShops || !BotPopulation.Within(map, vendor.Location))
            {
                continue;
            }

            if (_shops.Contains(vendor))
            {
                continue;
            }

            _shops.Add(vendor);
            found++;
        }

        logger.Information(
            "Found {Found} shopkeepers within {Reach} tiles of {Where} on {Map} (now {Total})",
            found,
            Reach,
            around,
            map,
            _shops.Count
        );

        return found;
    }

    /// <summary>
    /// What the town's shelves have run out of, by kind, and how often somebody was turned away.
    ///
    /// <para>
    /// <b>A shortage the shard felt everywhere and recorded nowhere.</b> <c>BotShopper</c> keeps a tally of
    /// what bots are short of, and it is a good one — but it only ever sees a bot's <em>kit</em>: bandages,
    /// reagents, tools, bottles it wears. A crafter's raw material goes through <c>BotStores</c> and
    /// <c>BotBullion</c> instead, so "the shelf holds no Bottle at any price" was written eight times in ten
    /// minutes, over and over, into a tally that had no row for it. Anything reading shortages to decide what
    /// to stock — the guild counters do — was therefore blind to exactly the shortages that stop a craft.
    /// </para>
    ///
    /// <para>
    /// Counted here, at the counter, where the refusal actually happens and whatever the errand was called.
    /// It is a fact about the town rather than about the bot: an empty shelf is empty for everybody.
    /// </para>
    /// </summary>
    private static readonly Dictionary<Type, long> _dry = [];

    /// <summary>What the town has run out of, commonest first. See <see cref="_dry"/>.</summary>
    public static List<(Type Kind, long Times)> Dry()
    {
        List<(Type Kind, long Times)> found = [];

        foreach (var (kind, times) in _dry)
        {
            found.Add((kind, times));
        }

        found.Sort((a, b) => b.Times.CompareTo(a.Times));

        return found;
    }

    /// <summary>The commonest of them, for the shops line.</summary>
    private static string Driest()
    {
        Type worst = null;
        long most = 0;

        foreach (var (kind, times) in _dry)
        {
            if (times > most)
            {
                most = times;
                worst = kind;
            }
        }

        return worst == null ? "nothing" : $"{worst.Name} ({most} times)";
    }

    /// <summary>How often the shelves are looked at to see whether any is due a refill.</summary>
    public static int KeepEveryMs { get; set; } = 60000;

    /// <summary>Shelves refilled because their hour had come round. See <see cref="Keep"/>.</summary>
    public static long Refills { get; private set; }

    private static long _kept;

    private static bool _everKept;

    /// <summary>
    /// Refills any shelf whose hour has come round, whether or not anybody is standing at it.
    ///
    /// <para>
    /// <b>A shop that has sold out can only be refilled by somebody buying from it, and nobody is sent to a
    /// shop that has sold out.</b> The engine refills on two events and this population causes neither:
    /// <c>OnOpenShop</c>, which is a player opening the window, and <c>BotShops.Buy</c>, which requires a bot
    /// already standing at the counter with money out. But <see cref="Sells"/> answers false once
    /// <c>info.Amount</c> reaches nought, and <see cref="Nearest"/> only ever offers a shop that
    /// <see cref="Sells"/> approved — so the last bottle bought off a shelf is the last bottle that shelf
    /// ever holds, unless some other bot happens to want something else from the same vendor.
    /// </para>
    ///
    /// <para>
    /// That is the self-blocking shape this project has paid for before: a mechanism that switches itself off
    /// exactly when it is needed. Measured 09.09.2026: nine failures of "the shelf holds no Bottle at any
    /// price" in ten minutes, against a shard whose alchemists refill hourly and whose engine doubles a
    /// sold-out entry's stock at the next refill — demand the population was generating and could not reach.
    /// </para>
    ///
    /// <para>
    /// A shelf refilling on its own clock is nearer the engine's intent than one refilling on footfall, not
    /// further from it: the shop-window check exists because that is the only moment a refill matters to a
    /// human. For this population the equivalent moment is any moment at all, so it is asked once a minute
    /// and <c>Restock</c> itself does nothing until the vendor's own hour is up.
    /// </para>
    /// </summary>
    public static void Keep()
    {
        var now = Core.TickCount;

        if (_everKept && now - (_kept + KeepEveryMs) < 0)
        {
            return;
        }

        _kept = now;
        _everKept = true;

        for (var i = 0; i < _shops.Count; i++)
        {
            var vendor = _shops[i];

            if (vendor is not { Deleted: false } || !vendor.IsActiveSeller)
            {
                continue;
            }

            if (Core.Now - vendor.LastRestock <= vendor.RestockDelay)
            {
                continue;
            }

            vendor.Restock();
            Refills++;
        }
    }

    /// <summary>
    /// Whether this shopkeeper sells the thing, and what it is asking.
    ///
    /// The entry is needed rather than only the price, because buying wants the serial of the display object
    /// the shopkeeper matches an order against — the same object a player's shop window shows a picture of.
    /// </summary>
    public static bool Sells(BaseVendor vendor, Type wanted, out GenericBuyInfo entry)
    {
        entry = null;

        if (vendor == null || vendor.Deleted || wanted == null || !vendor.IsActiveSeller)
        {
            return false;
        }

        var offered = vendor.GetBuyInfo();

        for (var i = 0; i < offered.Length; i++)
        {
            if (offered[i] is not GenericBuyInfo info || info.Type != wanted || info.Amount <= 0)
            {
                continue;
            }

            entry = info;

            return true;
        }

        return false;
    }

    /// <summary>
    /// The ledger's key for "I could not get to this shop".
    ///
    /// <para>
    /// Per bot, for the reason the forges taught: a counter behind a river is behind a river only for whoever
    /// is on the wrong bank. A tailor whose nearest cloth shop cannot be reached used to be handed that same
    /// shop every time it asked — fourteen failed sewing trips in ten minutes, each one a walk that ended in a
    /// refusal.
    /// </para>
    /// </summary>
    public const string ShopKind = "shop";

    /// <summary>
    /// The nearest shopkeeper selling this that this bot has not lately failed to reach.
    /// </summary>
    public static BaseVendor Nearest(IBotWilful bot, Type wanted) => Nearest(bot, wanted, Point3D.Zero);

    /// <summary>
    /// The same, nearest to <paramref name="from"/> rather than to the bot (the bot's own when zero): a guild's supplier buys in
    /// its guild's own town, so it asks for the counter nearest the hall (01.10.2026, Patrick: "a guild's traders prefer to
    /// trade in their home town"). Reached from where the bot stands, as ever.
    /// </summary>
    public static BaseVendor Nearest(IBotWilful bot, Type wanted, Point3D from)
    {
        var body = bot?.Self;
        var ledger = bot?.Resolve?.Ledger;

        if (body == null || ledger == null)
        {
            return Nearest(body, wanted);
        }

        BaseVendor best = null;
        var bestAway = double.MaxValue;
        var map = body.Map;

        if (map == null || map == Map.Internal || wanted == null)
        {
            return null;
        }

        for (var i = 0; i < _shops.Count; i++)
        {
            var vendor = _shops[i];

            // <b>A shop beyond the bot's leash is not its shop (29.09.2026).</b> Three of The Needle's bots died on the eight hundred tiles
            // between Cove and Britain in one hour, each walking to buy a pair of scissors at eleven gold. See BotPopulation.Leash.
            if (vendor.Deleted || vendor.Map != map || !BotPopulation.Within(map, vendor.Location, bot?.Self))
            {
                continue;
            }

            if (ledger.Cautious(ShopKind, map, vendor.Location) || !Sells(vendor, wanted, out _))
            {
                continue;
            }

            // <b>A counter on another land, or in a pocket no gate leaves, is nobody's counter (01.10.2026, build 386).</b> Twelve
            // restock errands on build 386 ended "no way through to Tamma in Serpent's Hold", and two each to Delucia and
            // Moonglow: the leash test above passes a counter across the water, and the sealed-pocket test below is asked of
            // the graph, which places a counter on an island as placed. The samplers' own test (BotPopulation.Reachable: the
            // lands and the reach map, one lookup) is asked here as well.
            if (!BotPopulation.Reachable(map, body.Location, vendor.Location))
            {
                Unjoined++;

                continue;
            }

            // <b>A counter over a road that kills is somebody else's counter (01.10.2026, build 388).</b> Restocks were a tenth of
            // 386's deaths, every one on the road to a shop chosen by price and distance alone; the road is weighed here, so the
            // buyer is sent to the next counter it can reach alive rather than refused the errand. See BotDanger.Road.
            if (BotDanger.Road(body, map, body.Location, vendor.Location, BotQuad.Strength(body), out _, out _, out _))
            {
                DeadlyRoad++;

                continue;
            }

            // The caution above is this bot's own and forgets in five minutes. This is the shard's, it costs
            // a dictionary lookup, and it does not forget: no way through is no way through for anybody. See
            // the twin of this line in the overload below for what it was costing.
            if (Unplaced(map, vendor) || BotReach.Ask(map, body.Location, vendor.Location, BotArrival.Within(CounterReach)) == BotReachVerdict.Sealed)
            {
                Walled++;

                continue;
            }

            var origin = from == Point3D.Zero ? body.Location : from;
            var dx = vendor.X - origin.X;
            var dy = vendor.Y - origin.Y;
            var away = Math.Sqrt(dx * dx + dy * dy);

            if (away >= bestAway)
            {
                continue;
            }

            best = vendor;
            bestAway = away;
        }

        return best;
    }

    /// <summary>The nearest remembered shopkeeper that sells this, or null.</summary>
    private static readonly Dictionary<Serial, (bool Placed, int Epoch)> _placed = [];

    /// <summary>Shopkeepers passed over for standing where the walkers' graph places no tile within their counter's reach.</summary>
    public static long Unplaceable { get; private set; }

    /// <summary>Counters passed over for a road that asks more than the buyer brings. See <see cref="BotDanger.Road"/>.</summary>
    public static long DeadlyRoad { get; private set; }

    /// <summary>
    /// <b>A counter no road reaches is not a shop (30.09.2026, build 357).</b> Shannon by Trinsic, at (1675, 2988), stands where
    /// the navigation graph "cannot place the goal": every walk to her was planned straight, came back short and burned the
    /// ceiling — Odile for seven minutes on 355, 714 of the ceiling, and Galadrion 180 short searches in the first quarter hour of
    /// 356, the chase share falling to 77 % and then 57 %. The failure of a walk that follows a body is kept out of the shard's
    /// refusals on purpose, so every bot sent there learned it alone. Whether the graph places any tile within CounterReach of
    /// the shopkeeper is asked once per shopkeeper per count of the graph's components, and one it places none of is passed over.
    /// </summary>
    private static bool Unplaced(Map map, BaseVendor vendor)
    {
        if (map == null || vendor == null || !Server.Engines.Pathing.Tiered.NavigationService.ComponentsCounted(map))
        {
            return false;
        }

        var epoch = Server.Engines.Pathing.Tiered.NavigationService.ComponentEpoch(map);

        if (_placed.TryGetValue(vendor.Serial, out var known) && known.Epoch == epoch)
        {
            return !known.Placed;
        }

        var placed = false;
        var at = vendor.Location;

        for (var dx = -CounterReach; dx <= CounterReach && !placed; dx++)
        {
            for (var dy = -CounterReach; dy <= CounterReach; dy++)
            {
                if (BotStep.Settle(map, at.X + dx, at.Y + dy, out var z)
                    && Server.Engines.Pathing.Tiered.NavigationService.ComponentOf(map, new Point3D(at.X + dx, at.Y + dy, z)) >= 0)
                {
                    placed = true;

                    break;
                }
            }
        }

        if (_placed.Count >= 4096)
        {
            _placed.Clear();
        }

        _placed[vendor.Serial] = (placed, epoch);

        if (!placed)
        {
            Unplaceable++;
            logger.Information(
                "{Name} at {Where} stands where the walkers' graph places no tile within {Reach} of the counter; no bot is sent there",
                vendor.Name,
                vendor.Location,
                CounterReach
            );
        }

        return !placed;
    }

    public static BaseVendor Nearest(Mobile bot, Type wanted)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal || wanted == null)
        {
            return null;
        }

        BaseVendor best = null;
        var bestAway = double.MaxValue;

        for (var i = 0; i < _shops.Count; i++)
        {
            var vendor = _shops[i];

            if (vendor.Deleted || vendor.Map != map || !BotPopulation.Within(map, vendor.Location, bot))
            {
                continue;
            }

            if (!Sells(vendor, wanted, out _))
            {
                continue;
            }

            // <b>A shopkeeper behind something is still the nearest shopkeeper.</b> This ranks by the crow's
            // flight and asked nothing about the road, so a counter on an upper floor or across water was
            // chosen by everybody underneath it — and the deed that took it walked, failed and was offered
            // the same shop again. Thirty-one peddling errands ended "no way through" in ninety minutes on
            // 03.09.2026, and the same chooser answers for restocking, acquiring, inscribing and sewing.
            //
            // The overload above already declines a shop this bot has lately failed to reach; that is a
            // private opinion with a five-minute memory. This is the shard's own, it costs a dictionary
            // lookup, and no way through is no way through for anybody. See BotGround.Nearest, which has
            // asked exactly this of every forge and counter for a fortnight.
            if (Unplaced(map, vendor) || BotReach.Ask(map, bot.Location, vendor.Location, BotArrival.Within(CounterReach)) == BotReachVerdict.Sealed)
            {
                Walled++;

                continue;
            }

            var away = bot.GetDistanceToSqrt(vendor.Location);

            if (away >= bestAway)
            {
                continue;
            }

            best = vendor;
            bestAway = away;
        }

        return best;
    }

    /// <summary>What this shopkeeper is asking for one of those, or zero if it does not sell them.</summary>
    public static int Price(BaseVendor vendor, Type wanted) => Sells(vendor, wanted, out var entry) ? entry.Price : 0;

    /// <summary>How long the island's lowest counter price for a kind is kept before it is read again. See <see cref="Lowest"/>.</summary>
    public static int LowestMs { get; set; } = 300000;

    private static readonly Dictionary<Type, (int Price, long At)> _lowest = [];

    /// <summary>
    /// The least any shopkeeper the population knows of asks for one of these, or nought when none of them sells it.
    ///
    /// <para>
    /// Placeless, as the market is: a stall is bought from wherever the buyer stands, and each buyer weighs it against its own
    /// nearest counter, so the price every buyer can be sure of beating is the lowest. Read off the known counters (at most
    /// <see cref="MaxShops"/>) and kept <see cref="LowestMs"/>, because it is asked on a sale and a sale can happen a few times a
    /// second. Asked of the shelf's price whatever its stock: an empty shelf still names what it will ask when it refills.
    /// </para>
    /// </summary>
    public static int Lowest(Type kind)
    {
        if (kind == null)
        {
            return 0;
        }

        var now = Core.TickCount;

        if (_lowest.TryGetValue(kind, out var known) && now - known.At < LowestMs)
        {
            return known.Price;
        }

        var lowest = 0;

        for (var i = 0; i < _shops.Count; i++)
        {
            var vendor = _shops[i];

            if (vendor == null || vendor.Deleted || !vendor.IsActiveSeller)
            {
                continue;
            }

            var offered = vendor.GetBuyInfo();

            for (var j = 0; j < offered.Length; j++)
            {
                if (offered[j] is GenericBuyInfo info && info.Type == kind && info.Price > 0 && (lowest == 0 || info.Price < lowest))
                {
                    lowest = info.Price;
                }
            }
        }

        _lowest[kind] = (lowest, now);

        return lowest;
    }

    /// <summary>
    /// What a bot's own goods of this kind should open at: the shelf price where a shopkeeper in reach
    /// stocks it, and only then the trade's own guess.
    ///
    /// <para>
    /// <b>An opening ask above the shelf is an ask nobody on this island can rationally take.</b>
    /// <see cref="BotShopper"/> compares stall against counter and gives a tie to one of ours, so a reagent
    /// listed at five where a herbalist sells garlic at three is a reagent that never moves: 1986 of them sat
    /// on stalls in one window while every caster that wanted one walked past and paid the world instead. The
    /// fletcher already carries this lesson about arrows; this is where the rest of the shard asks it.
    /// </para>
    ///
    /// <para>
    /// Measured off the engine rather than declared, like the loot floor in <c>BotSlay.Rifle</c> — there is no
    /// table here to go stale. The fallback is only ever reached where nothing within reach stocks the thing
    /// at all, which is exactly where a bot's stall is the only supply there is.
    /// </para>
    /// </summary>
    /// <param name="survey">The caller sweeps for counters once, before its own loop, and passes false.</param>
    public static int Shelf(IBotWilful bot, Type kind, int fallback, bool survey = false)
    {
        var body = bot?.Self;

        if (body == null || kind == null)
        {
            return fallback;
        }

        if (survey)
        {
            Survey(body.Map, body.Location);
        }

        var shop = Nearest(bot, kind);
        var price = shop == null ? 0 : Price(shop, kind);

        return price > 0 ? price : fallback;
    }

    /// <summary>
    /// Whether this shopkeeper will <em>buy</em> this thing, and what it pays for one.
    ///
    /// <para>
    /// Asked of an actual item rather than of a type, because that is the only question the engine answers:
    /// <c>IsSellable</c> looks at the object, and a shopkeeper that buys daggers is being asked about this
    /// dagger. So a caller with nothing in hand has to find something to show.
    /// </para>
    /// </summary>
    public static bool Buys(BaseVendor vendor, Item item, out int price)
    {
        price = 0;

        if (vendor == null || vendor.Deleted || item == null || item.Deleted || !vendor.IsActiveBuyer)
        {
            return false;
        }

        var counters = vendor.GetSellInfo();

        for (var i = 0; i < counters.Length; i++)
        {
            var counter = counters[i];

            if (!counter.IsSellable(item))
            {
                continue;
            }

            price = counter.GetSellPriceFor(item);

            return price > 0;
        }

        return false;
    }

    /// <summary>
    /// The nearest shopkeeper that buys this and that <b>this bot</b> has not lately failed to reach.
    ///
    /// <para>
    /// <b>The selling side had no such question and the buying side did, which is the whole of why one
    /// shopkeeper could swallow an afternoon.</b> Gus buys iron ingots and stands on a plateau at height
    /// thirty; Calla walked at him thirty-one times in one hour on 26.08.2026 and Alden eight, because the
    /// nearest buyer was chosen on distance alone and nothing about the last thirty attempts entered into
    /// it. <see cref="Nearest(IBotWilful, Type)"/> has asked this since the counters were fixed; this side
    /// simply never grew the same overload.
    /// </para>
    ///
    /// <para>
    /// The bot's own note, not the shard's — another bot on the right side of the hill reaches Gus perfectly
    /// well, and telling everybody otherwise would close the only ingot buyer on the island.
    /// </para>
    /// </summary>
    public static BaseVendor Buyer(IBotWilful bot, Item item, out int price) => Buyer(bot, item, 1, false, out price, out _, out _);

    /// <summary>
    /// The buyer a load of <paramref name="units"/> should walk to: the one where it fetches most once the road is paid for.
    ///
    /// <para>
    /// <b>The road is a price, and the capital pays for one (30.09.2026, Patrick's evening order: «the traders must put
    /// Britain first as the centre of trade»).</b> This chose the nearest buyer on distance alone, and every other town stands 800 tiles and
    /// more from Britain, so no load was ever carried to the capital however much it was worth. Each buyer is now weighed at
    /// <c>units × its price × the capital's premium − tiles × <see cref="BotPeddler.PettyPerTile"/></c>: the premium
    /// (<see cref="BotCapital.Premium"/>) only where <see cref="BotCapital.Pull"/> lets it count — a load drawn out of a bank box at a
    /// bank (<paramref name="drawn"/>), a counter in the capital within the bot's leash and <see cref="BotCapital.Reach"/>, on a joined land,
    /// with the treasury able to pay — and the road at the price the peddler's "worth less than the walk" gate already reads, so the
    /// counter chosen is never one that gate then refuses for its road. Among buyers paying the same, the nearest wins as before.
    /// <paramref name="factor"/> is the premium counted for the one chosen (one where none), and <paramref name="nearer"/> how many
    /// tiles off the nearest buyer stood, so the peddler can say when the capital drew a load past it.
    /// </para>
    ///
    /// <para>
    /// <b>And the two questions <see cref="Nearest(IBotWilful, Type)"/> has asked since build 357, which this side never grew.</b> A
    /// counter the walkers' graph places no tile beside, or one the shard has proved there is no way to, is passed over here too
    /// (<see cref="Walled"/>): a road priced across the island to a counter nobody can reach is the worst of both.
    /// </para>
    /// </summary>
    public static BaseVendor Buyer(IBotWilful bot, Item item, int units, bool drawn, out int price, out double factor, out int nearer)
    {
        price = 0;
        factor = 1.0;
        nearer = -1;

        var body = bot?.Self;
        var ledger = bot?.Resolve?.Ledger;

        if (body == null)
        {
            return null;
        }

        if (ledger == null)
        {
            return Buyer(body, item, out price);
        }

        var map = body.Map;

        if (map == null || map == Map.Internal || item == null)
        {
            return null;
        }

        units = Math.Max(1, units);

        var kind = item.GetType();
        BaseVendor best = null;
        var bestNet = double.MinValue;
        var bestTiles = int.MaxValue;
        var refused = BotCapital.Why.NotCapital;
        var refusedWith = double.MinValue;
        var road = Math.Max(0.0, BotPeddler.PettyPerTile);

        for (var i = 0; i < _shops.Count; i++)
        {
            var vendor = _shops[i];

            if (vendor.Deleted || vendor.Map != map || !BotPopulation.Within(map, vendor.Location, bot?.Self))
            {
                continue;
            }

            if (ledger.Cautious(ShopKind, map, vendor.Location) || !Buys(vendor, item, out var paying))
            {
                continue;
            }

            if (Unplaced(map, vendor) || BotReach.Ask(map, body.Location, vendor.Location, BotArrival.Within(CounterReach)) == BotReachVerdict.Sealed)
            {
                Walled++;

                continue;
            }

            var tiles = Math.Max(Math.Abs(vendor.X - body.X), Math.Abs(vendor.Y - body.Y));

            if (nearer < 0 || tiles < nearer)
            {
                nearer = tiles;
            }

            var gross = units * (double)paying;
            var net = gross - tiles * road;
            var f = 1.0;

            // Asked only of a counter the premium would make the best, so the land's lookup is paid a handful of times a look.
            // The premium for this kind at this price, cut at the island's lowest ask: see BotCapital.Factor.
            var premium = BotCapital.Factor(kind, paying);
            var with = gross * premium - tiles * road;

            if (premium > 1.0 && (with > bestNet || with == bestNet && tiles < bestTiles))
            {
                var why = BotCapital.Pull(bot, vendor, drawn);

                if (why == BotCapital.Why.Pulled)
                {
                    net = with;
                    f = premium;
                }
                else if (why != BotCapital.Why.NotCapital && with > refusedWith)
                {
                    refused = why;
                    refusedWith = with;
                }
            }

            if (net < bestNet || net == bestNet && tiles >= bestTiles)
            {
                continue;
            }

            best = vendor;
            bestNet = net;
            bestTiles = tiles;
            price = paying;
            factor = f;
        }

        // A capital's counter that would have beaten the one chosen at the capital's price, and did not count it: the reason,
        // once for this choice.
        if (best != null && refusedWith > bestNet && !BotCapital.In(best.Location))
        {
            BotCapital.Passed(refused);
        }

        return best;
    }

    /// <summary>
    /// The nearest remembered shopkeeper that buys this, and what it pays for one.
    ///
    /// <b>Distance only.</b> For asking what a thing is worth, where reachability is beside the point — see
    /// the overload above for choosing somewhere to walk to.
    /// </summary>
    public static BaseVendor Buyer(Mobile bot, Item item, out int price)
    {
        price = 0;

        var map = bot?.Map;

        if (map == null || map == Map.Internal || item == null)
        {
            return null;
        }

        BaseVendor best = null;
        var bestAway = double.MaxValue;

        for (var i = 0; i < _shops.Count; i++)
        {
            var vendor = _shops[i];

            if (vendor.Deleted || vendor.Map != map || !BotPopulation.Within(map, vendor.Location, bot))
            {
                continue;
            }

            if (!Buys(vendor, item, out var paying))
            {
                continue;
            }

            var away = bot.GetDistanceToSqrt(vendor.Location);

            if (away >= bestAway)
            {
                continue;
            }

            best = vendor;
            bestAway = away;
            price = paying;
        }

        return best;
    }

    /// <summary>
    /// Sells things over a counter and says how much gold came back.
    ///
    /// <para>
    /// <b>This is the only place in the project where gold enters the world.</b> Trade between bots moves coin
    /// about; a shopkeeper's purse creates it. That is why the first version drained its world by 110,900 in a
    /// night — every bot was pointed at the one faucet — and it is why nothing here decides on its own to come
    /// here: see <see cref="BotPeddle"/> for the one condition under which it is allowed, which is that the
    /// population has already been offered the goods and refused them.
    /// </para>
    ///
    /// <para>
    /// The takings are <b>measured rather than added up</b>, because the engine decides how much of the order
    /// it honours: it refuses anything not in the seller's own pack, anything immovable, anything not standard
    /// loot — which is a free guarantee that <em>bound gear cannot be sold</em>, since the bind marks it
    /// <c>Newbied</c> — and anything above its own per-visit limit. Believing the asking prices would be
    /// counting an intention.
    /// </para>
    /// </summary>
    public static int Sell(IBotWilful bot, BaseVendor vendor, List<Item> goods) =>
        Sell(bot, vendor, goods, out _);

    /// <summary>
    /// The same, saying how many <em>units</em> actually crossed the counter.
    ///
    /// <para>
    /// <b>Every sale on this shard was reported against a denominator that did not belong to it.</b> The
    /// count said here was the number of stacks, and the peddler's own line used the number it had reclaimed
    /// from a stall — while the goods handed over are everything of that kind in the pack, stall stock and
    /// carried stock together. So "sold 4 Iron Ingot for 316gp" was four off a stall, seventy-five out of the
    /// pack and 316gp for all seventy-nine: four gold an ingot, the ordinary price, printed as seventy-nine.
    /// A person reading the log for what iron is worth on this island got a number an order of magnitude out,
    /// in the one place prices are read. Units are counted before the sale, because the engine deletes the
    /// stacks as it takes them.
    /// </para>
    /// </summary>
    public static int Sell(IBotWilful bot, BaseVendor vendor, List<Item> goods, out int units) => Sell(bot, vendor, goods, out units, out _);

    /// <summary>
    /// The same, saying what the city paid on top when the counter stands in the capital (<paramref name="premium"/>, see
    /// <see cref="BotCapital.Sold"/>). What is returned, and what the "sold" line says, is still the shopkeeper's own gold: that
    /// line is read by the watch for the world's income over a counter, and the premium is the city's coin, counted in the
    /// city's line and the capital's.
    /// </summary>
    public static int Sell(IBotWilful bot, BaseVendor vendor, List<Item> goods, out int units, out int premium)
    {
        units = 0;
        premium = 0;

        var body = bot?.Self;
        var pack = body?.Backpack;

        if (pack == null || vendor == null || vendor.Deleted || goods == null || goods.Count == 0)
        {
            return 0;
        }

        if (!body.InRange(vendor.Location, CounterReach) || !vendor.IsActiveBuyer)
        {
            return 0;
        }

        List<SellItemResponse> order = [];

        // Kind, units and the shopkeeper's price per unit, line by line, kept for the balance once the engine has deleted the
        // stacks. See BotGlut.Peddled.
        List<(Type Kind, int Amount, int Paying)> lines = [];

        // What the capital's counter owes on top, line by line, reckoned before the engine deletes the stacks. See BotCapital.
        var capital = BotCapital.In(vendor.Location);
        long owed = 0;

        for (var i = 0; i < goods.Count; i++)
        {
            var item = goods[i];

            if (item == null || item.Deleted || !item.Movable || !item.IsStandardLoot())
            {
                continue;
            }

            if (item.RootParent != body || !Buys(vendor, item, out var paying))
            {
                continue;
            }

            var amount = Math.Max(1, item.Amount);

            units += amount;

            if (capital)
            {
                owed += BotCapital.Owed(item.GetType(), amount, paying);
            }

            order.Add(new SellItemResponse(item, amount));
            lines.Add((item.GetType(), amount, paying));
        }

        if (order.Count == 0)
        {
            units = 0;

            return 0;
        }

        var before = BotYield.Wealth(body);

        if (!vendor.OnSellItems(body, order))
        {
            units = 0;

            return 0;
        }

        var earned = BotYield.Wealth(body) - before;

        if (earned <= 0)
        {
            units = 0;

            return 0;
        }

        // What a shopkeeper pays for a kind, learned here and nowhere else: the one price goods nobody asks for really have
        // (01.10.2026, Patrick's point 8). See BotGlut.Peddled and BotGlut.Scrap.
        for (var i = 0; i < lines.Count; i++)
        {
            var (kind, amount, paying) = lines[i];

            BotGlut.Peddled(kind, amount, amount * paying);
        }

        Sold += order.Count;
        Earned += earned;

        // <b>The number changed and the shape did not, and that is deliberate.</b> watch-shard.sh reads this
        // line for the whole shard's income — the awk that sums it wants "sold N things to Somebody for Ngp"
        // with the gold as a clean field. Saying the stack count as well, in any position, either breaks the
        // match or leaves a comma stuck to the gold: it read nought gold entering the world beside forty-six
        // sales at 21:12 on 04.09.2026. A log line another tool parses is an interface. The stack count is
        // not worth a reader's attention anyway — units and gold are the two numbers a price is made of.
        logger.Information(
            "{Name} sold {Units} things to {Vendor} for {Gold}gp",
            body.Name,
            units,
            vendor.Name,
            earned
        );

        // Booked by where the counter stands, and the capital's price paid on top there. See BotCapital.
        premium = BotCapital.Sold(body, vendor, units, earned, owed);

        // The town's charter for a craft takes its levy out of what the counter paid, into the holder's chest (07.10.2026,
        // charters20). What is returned stays the shopkeeper's gold, the world's income; the levy moves coin, it makes none.
        BotCharters.Counter(body, vendor.Location, lines, earned);

        return earned;
    }

    /// <summary>
    /// Buys up to <paramref name="amount"/> of something, and says how many were actually bought.
    ///
    /// <para>
    /// The affordability is worked out here rather than left to the shopkeeper, because the engine takes an
    /// order whole or not at all: an order for ten when the purse holds eight buys nothing and says nothing.
    /// </para>
    /// </summary>
    public static int Buy(IBotWilful bot, BaseVendor vendor, Type wanted, int amount) =>
        Buy(bot, vendor, wanted, amount, out _);

    /// <summary>
    /// The same purchase, and <paramref name="refused"/> says which gate closed when nothing was bought.
    ///
    /// <para>
    /// <b>Six ways to come away empty were printed as one sentence, and it hid the largest fault on the
    /// shard.</b> "The shop would not sell it" was written knowing there were three cases — the comment
    /// beside it says "sold out, priced out, or refused" — and reasoned that all three were the shop's
    /// business rather than the bot's. That is true of what to do next and false of what to log: on the night
    /// of 25.08.2026 this sentence appeared 1929 times in half an hour, against nought successful purchases
    /// of a bandage ever, and there was no way to tell an empty shelf from an empty purse without reading the
    /// source. A branch nobody can count is a branch nobody can fix — the same rule that governs the
    /// heartbeat lines governs this.
    /// </para>
    /// </summary>
    public static int Buy(IBotWilful bot, BaseVendor vendor, Type wanted, int amount, out string refused)
    {
        refused = null;

        var body = bot?.Self;
        var pack = body?.Backpack;

        if (pack == null || vendor == null || vendor.Deleted || amount <= 0 || !vendor.IsActiveSeller)
        {
            refused = "there is no shopkeeper to buy from";

            return 0;
        }

        if (!body.InRange(vendor.Location, CounterReach))
        {
            refused = "it is not close enough to the counter";

            return 0;
        }

        // Shelves empty as they are bought from and refill on a timer that is only wound when somebody opens
        // the shop window. Bots never open one, so without this a shop a bot has cleaned out stays empty.
        if (Core.Now - vendor.LastRestock > vendor.RestockDelay)
        {
            vendor.Restock();
        }

        // Prices carry the shard's own scalars and are only brought up to date on demand.
        vendor.UpdateBuyInfo();

        if (!Sells(vendor, wanted, out var entry) || entry.Price <= 0)
        {
            _dry.TryGetValue(wanted, out var turned);
            _dry[wanted] = turned + 1;

            refused = $"the shelf holds no {wanted.Name} at any price";

            return 0;
        }

        if (entry.GetDisplayEntity() is not { Deleted: false } display)
        {
            refused = $"the shopkeeper has no {wanted.Name} to show";

            return 0;
        }

        var price = entry.Price;

        // What it may spend at a counter (02.10.2026, build 391): what it has free. Not even its own want for this kind: a counter
        // purchase does not bring that want down (the shopper takes the want back before it walks), so money spent here out of
        // the want's reservation would leave the want promising money that is gone. See BotMarket.Reserved.
        var purse = BotYield.Spendable(body);

        // The buyer's guild's standing in the town this shop stands in (29.09.2026): a town's tasks done or failed make its
        // prices a few percent kinder or dearer to that guild. Judged at the dearer price, so the surcharge is always payable.
        // See BotBurgh.Factor and BotBurgh.Settle below.
        var factor = BotBurgh.Factor(body, vendor, wanted);
        var dear = factor > 1.0 ? (int)Math.Ceiling(price * factor) : price;
        var affordable = Math.Min(Math.Min(amount, entry.Amount), purse / dear);

        if (affordable <= 0)
        {
            refused = purse < dear
                ? $"{purse}gp will not buy one {wanted.Name} at {dear}gp"
                : $"the shelf is down to {entry.Amount} {wanted.Name}";

            return 0;
        }

        // <b>Money in the bank is only money if something fetches it, and nothing did.</b> The engine pays a
        // bill under two thousand gold out of the backpack and never looks at the account — while five places
        // in this project put coin <em>into</em> an account and not one takes any out, the sole withdrawal on
        // the shard being the bot market charging its own buyers. So the population's takings drained one way
        // into the bank and stayed there.
        //
        // <see cref="BotYield.Wealth"/> counts the account and the pocket together, and every affordability
        // judgement a bot makes rests on that sum. Either the sum is a lie or the money is reachable; this
        // makes it reachable, which is the half that keeps a rich bot able to buy a bandage. Withdrawn first
        // and carried second, and put straight back if the pack will not take it — the account is debited by
        // the withdrawal itself, so the other order would mint gold out of nothing.
        var bill = affordable * price;
        var carried = pack.GetAmount(typeof(Gold));

        if (carried < bill)
        {
            var owing = bill - carried;

            if (!Banker.Withdraw(body, owing))
            {
                refused = $"{carried}gp in the pack and the bank would not find the other {owing}gp";

                return 0;
            }

            var drawn = new Gold(owing);

            if (!pack.TryDropItem(body, drawn, false))
            {
                drawn.Delete();
                Banker.Deposit(body, owing);

                refused = $"the pack would not hold the {owing}gp it drew to pay with";

                return 0;
            }
        }

        List<BuyItemResponse> order = [new BuyItemResponse(display.Serial, affordable)];

        if (!vendor.OnBuyItems(body, order))
        {
            refused = $"{vendor.Name} turned down {affordable} {wanted.Name} at {price}gp with {purse}gp to hand";

            return 0;
        }

        Bought += affordable;
        Spent += affordable * price;

        // Booked by where the counter stands, for the capital's share of the island's buying. Nothing about the price changes.
        BotCapital.Bought(vendor, affordable * price);

        // The engine took the shelf's price; the guild's standing is settled on top of it — a share handed back, never more
        // than this purchase paid, or the rest taken. No gold is made by it. See BotBurgh.Settle.
        BotBurgh.Settle(body, affordable * price, factor);

        logger.Information(
            "{Name} bought {Amount} {Item} from {Vendor} for {Cost}gp",
            body.Name,
            affordable,
            wanted.Name,
            vendor.Name,
            affordable * price
        );

        return affordable;
    }

    /// <summary>
    /// Everything forgotten. Shopkeepers are mobiles of a world that is being replaced, and a reference to
    /// one of those is a reference to a deleted object.
    /// </summary>
    public static void Reset()
    {
        _lowest.Clear();
        _shops.Clear();
        _swept.Clear();

        Bought = 0;
        Walled = 0;
        Unjoined = 0;
        Spent = 0;
        Sold = 0;
        Earned = 0;
        Refills = 0;
        _dry.Clear();
        _kept = 0;
        _everKept = false;
    }

    public static string Describe() =>
        $"{_shops.Count} shopkeepers known from {_swept.Count} sweeps; {Bought} things bought for {Spent}gp, {Sold} sold for {Earned}gp, {Walled} counters passed over for having no way through to them, {Unjoined} as on another land or in a pocket no gate leaves and {DeadlyRoad} for a road that asks more than the buyer brings, {Refills} shelves refilled on their own hour, {Repicked} errands sent on to another shopkeeper when the shelf emptied before they arrived, {BotPeddle.FromBox} loads ({BotPeddle.FromBoxUnits} things) taken out of a bank box at a bank to be carried to one, {BotRestock.FellThrough} errands that found their guild's shelf bought out and went on to one; the town is oftenest out of {Driest()}; {BotCapital.Describe()}";
}
