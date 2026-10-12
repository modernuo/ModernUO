using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.Deeds;

namespace Server.BotAI.V2;

/// <summary>
/// The traders as the rest of the shard sees them: what neutrality closes, what a haul is allowed to be, the shopkeepers that
/// sell a kind, and the <c>Traders:</c> line.
///
/// <para>
/// <b>Patrick's order of 30.09.2026, point 5, and plan v2 of 01.10.2026, point 3 (build 385).</b> A trader reads what the
/// island has said it wants (<see cref="BotMarket.Wants"/>), buys it where it is to be had — its own stock, a bot's shop, a
/// shopkeeper in any town — only against a want that is open, carries it to the buyer by road, gate or, once well off, by sea,
/// and hands it over at a meeting (<see cref="BotHandover.Exchange"/>). The want is the whole of the demand it answers, so a
/// trader never buys anything nobody asked for and never floods a shelf: with no open want it has no haul, and does what any
/// bot with nothing to sell does — hunts (Patrick, 30.09: poor bots hunt). Its leftovers are sold by its own shift at
/// Britain's bank (<c>BotShopkeeper.Merchant</c>, the producer role).
/// </para>
///
/// <para>
/// <b>Where the margin comes from, and why it is not a handout.</b> A want names the buyer's price. When that price already
/// pays the road (<see cref="LeastMarkup"/> over what the goods cost the trader) the trader takes it as said. When it does not,
/// the trader offers carriage: the two split what carrying saves the buyer (<see cref="CarriageShare"/>) between the trader's
/// least price and the buyer's own alternative — its walk to its nearest counter and back at the shard's own price of a
/// shopping minute (<see cref="BotShopkeep.WalkGold"/>) — never more than <see cref="MostMarkup"/> over cost. A buyer agrees,
/// because it is cheaper than walking; the units are what it can pay for. Every coin a trader earns is a buyer's coin for
/// goods it asked for. The median purse on 384 was 70gp (the 384 trade baseline), so the split is half, not all.
/// </para>
/// </summary>
public static class BotTrading
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTrading));

    // ---- Dials -------------------------------------------------------------------------------------------------------------

    /// <summary>Whether traders haul at all. Off, they are bots with a blade and a book, and hunt like anybody.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>Whether a neutral class stands outside the companies and the tolls. The guild muster reads the class itself.</summary>
    public static bool NeutralInWars { get; set; } = true;

    /// <summary>Gold a trader keeps back from any purchase: below it, it buys nothing and earns its capital some other way.</summary>
    public static int LeastCapital { get; set; } = 60;

    /// <summary>The least a haul must clear, in gold, to be worth the road.</summary>
    public static int LeastProfit { get; set; } = 12;

    /// <summary>The share over cost a buyer's own offer must reach to be taken as said; under it the trader offers carriage.</summary>
    public static double LeastMarkup { get; set; } = 0.15;

    /// <summary>The most over cost a trader ever asks: at most double.</summary>
    public static double MostMarkup { get; set; } = 1.0;

    /// <summary>
    /// The trader's share of what carrying saves the buyer, between its least price and the buyer's own alternative: half. The
    /// buyers are poor (median 70gp on 384), and every gold over the least price is a unit fewer they can pay for.
    /// </summary>
    public static double CarriageShare { get; set; } = 0.5;

    /// <summary>The claim a haul is offered at, in gold a minute, is its own profit a minute held to this band.</summary>
    public static double LeastClaim { get; set; } = 4.0;

    public static double MostClaim { get; set; } = 200.0;

    /// <summary>How long a want is left to the carriers within reach of it before a trader looks at it. A minute.</summary>
    public static int FreshMs { get; set; } = 60000;

    /// <summary>How long a trader that found nothing worth hauling waits before it reads the wants again.</summary>
    public static int LookMs { get; set; } = 20000;

    /// <summary>The most wants one look weighs, dearest first as the board holds them; the board's cap is 512.</summary>
    public static int MostLooked { get; set; } = 64;

    /// <summary>How many buyers one haul may serve from one purchase: wants for the same kind near the first buyer.</summary>
    public static int MostOrders { get; set; } = 3;

    /// <summary>How far from the first buyer another buyer of the same kind may be to ride on the same haul.</summary>
    public static int BatchTiles { get; set; } = 200;

    /// <summary>The most things one haul carries.</summary>
    public static int MostUnits { get; set; } = 80;

    /// <summary>Within this of a buyer the trader follows it; further off it walks to where the buyer was.</summary>
    public static int NearTiles { get; set; } = 40;

    /// <summary>How long a trader follows a buyer that keeps walking away before it strikes the order. Eight minutes.</summary>
    public static int ChaseMs { get; set; } = 480000;

    /// <summary>How long a haul waits for a buyer lying dead to rise before it strikes the line. Three minutes.</summary>
    public static int DeadWaitMs { get; set; } = 180000;

    /// <summary>The longest a haul may take, pauses not counted, before it is given up. Forty-five minutes.</summary>
    public static int MostHaulMs { get; set; } = 2700000;

    /// <summary>Minutes reckoned for a purchase at the counter and for a handing over, on top of the road.</summary>
    public static double BuyMinutes { get; set; } = 0.5;

    public static double HandMinutes { get; set; } = 0.5;

    /// <summary>
    /// How long a trader stands at its town's bank buying from whoever brings what a far buyer wants (the gathering, see
    /// <see cref="BotHaulFrom.Gather"/>); longer while a carrier is on its way, up to twice this. Five minutes; nought never.
    /// </summary>
    public static int GatherMs { get; set; } = 300000;

    /// <summary>The share of a gathering's asking a trader reckons on being brought, when it weighs one against a counter.</summary>
    public static double GatherYield { get; set; } = 0.5;

    /// <summary>How near the banker a gathering trader stands.</summary>
    public static int BankStand { get; set; } = 4;

    /// <summary>Whether a well-off trader sails to a town no road or gate reaches. See <see cref="SailWealth"/>.</summary>
    public static bool Sails { get; set; } = true;

    /// <summary>
    /// What "well off" is: a trader worth this much (pack and bank) may buy a ship and sail; below it, land and gates. A ship is
    /// <c>BotSea.ListPrice</c> (10,177) and a haul wants capital after it. A trader that already owns a ship sails regardless.
    /// <b>Not read since 06.10.2026 (money15):</b> a haul no longer buys a ship at all (<see cref="CanSail"/>).
    /// </summary>
    public static int SailWealth { get; set; } = 12000;

    /// <summary>How often the <c>Traders:</c> line is written.</summary>
    public static int SayEveryMs { get; set; } = 300000;

    /// <summary>How many closed orders a book keeps, newest first.</summary>
    public static int BookHistory { get; set; } = 40;

    /// <summary>How long the list of shopkeepers that sell a kind is kept before it is read again.</summary>
    public static int SellersMs { get; set; } = 60000;

    /// <summary>
    /// How long a trader leaves a want alone after striking an order on it for the buyer's own doing — walked away from the
    /// meeting, unreachable, refused — before it may take it again. Ten minutes (01.10.2026, build 385 review): nothing in the
    /// haul's own strikes marked the want, so the next look re-took the same want and chased the same buyer for another
    /// <see cref="ChaseMs"/>, as long as the want stood.
    /// </summary>
    public static int ShunMs { get; set; } = 600000;

    /// <summary>How long no trader gathers for a kind after a gathering for it brought nothing. See <c>BotHauler.ShunGathering</c> (build 386).</summary>
    public static int GatherShunMs { get; set; } = 1800000;

    /// <summary>Tiles between samples of a haul's road against the quadrant record. See <c>BotHauler.Deadly</c> (build 386).</summary>
    public static int RoadStep { get; set; } = 32;

    /// <summary>What a sampled square may ask, as a multiple of the trader's own strength, before the road is passed over; nought reads no road. See <c>BotHauler.Deadly</c>.</summary>
    public static double RoadDare { get; set; } = 1.5;

    /// <summary>
    /// <b>The largest share of the traders that may be resting at once (01.10.2026, build 388).</b> Patrick's word of 01.10: at
    /// least half of the traders must be in the world. On 386 three of seven rested together from 13:40 to 18:30 — Kargan 6.3 h,
    /// Vaeril and Norbert 5.3–5.9 h — and the island's own floor (<c>BotRest.FloorShare</c>, a half of everybody) said nothing
    /// about it: the traders are the market's infrastructure, and four of them were the whole of it for five hours. A half:
    /// with seven traders three may rest; the fourth past its hours plays on until one comes back, as the island's floor keeps
    /// bots on. Read by <c>BotRest</c>; one reads no floor.
    /// </summary>
    public static double RestShare { get; set; } = 0.5;

    /// <summary>How near the end of its rest a trader may be brought back early when more than <see cref="RestShare"/> are resting (after a restart, say). Three hours.</summary>
    public static int RestEarlyMinutes { get; set; } = 180;

    /// <summary>
    /// <b>What a trader keeps in its pocket; everything above it goes into its account the moment it is paid (01.10.2026, build
    /// 388).</b> Patrick's word of 01.10: keep the capital in the bank. A trader's capital rode in its pack between banks and a
    /// death took it: on 386 Oriel stood at 0gp three times in twenty minutes, Mardred at 17–21gp for four hours after two
    /// deaths, Vaeril and Norbert came back from their rest with nothing, and 28 % of the traders' looks were "too poor to buy".
    /// The counters and the meetings already draw on the account (<c>BotShops.Buy</c>, <c>BotMarket.Charge</c>), so the capital
    /// buys as well from the bank as from the pocket; what the pocket holds is only what a death can take. A hundred, the
    /// population's own float (<c>BotPurse.KeepBack</c>). Nought keeps everything in the pocket, as before.
    /// </summary>
    public static int PocketFloat { get; set; } = 100;

    /// <summary>The least above the float worth putting away in one go. Twenty.</summary>
    public static int PutAwayLeast { get; set; } = 20;

    /// <summary>Times a trader put its takings above the float away, and the gold; traders past their hours kept in the world by <see cref="RestShare"/>; traders brought back early for it.</summary>
    public static long PutAways { get; internal set; }

    public static long PutAwayGold { get; internal set; }

    public static long RestHeld { get; internal set; }

    public static long RestEarly { get; internal set; }

    /// <summary>A bot of the neutral class, whatever the war switch says: a trader. See <see cref="Neutral"/>.</summary>
    public static bool Trader(Mobile m) => m is BotMobile { Class.Neutral: true };

    /// <summary>
    /// Puts a trader's pocket above <see cref="PocketFloat"/> into its account, from wherever it stands (the engine's
    /// <c>Banker.Deposit</c> reaches the box from anywhere, as <c>BotUnload</c> notes). Returns what went in. Asked at every
    /// look of the hauler's and after every order handed over.
    /// </summary>
    public static int PutAway(Mobile body)
    {
        if (PocketFloat <= 0 || !Trader(body) || body is not { Deleted: false, Alive: true } || body.Backpack is not { } pack)
        {
            return 0;
        }

        var excess = pack.GetAmount(typeof(Gold)) - PocketFloat;

        if (excess < Math.Max(1, PutAwayLeast))
        {
            return 0;
        }

        // A box it has never opened is no box to the engine's deposit; opened here, once.
        if (body.FindBankNoCreate() == null)
        {
            _ = body.BankBox;
        }

        if (!pack.ConsumeTotal(typeof(Gold), excess))
        {
            return 0;
        }

        if (!Banker.Deposit(body, excess))
        {
            pack.DropItem(new Gold(excess));

            return 0;
        }

        PutAways++;
        PutAwayGold += excess;

        return excess;
    }

    // ---- Counters: what the proposer saw -----------------------------------------------------------------------------------

    public static long Looks { get; internal set; }

    public static long Poor { get; internal set; }

    public static long Barren { get; internal set; }

    public static long Offered { get; internal set; }

    public static long Fresh { get; internal set; }

    public static long ToTraders { get; internal set; }

    public static long OwnWant { get; internal set; }

    public static long NoSource { get; internal set; }

    public static long Thin { get; internal set; }

    public static long Unjoined { get; internal set; }

    public static long Broke { get; internal set; }

    public static long Perilous { get; internal set; }

    public static long Sealed { get; internal set; }

    public static long Heavy { get; internal set; }

    /// <summary>
    /// Shopkeepers' counters passed over because the island already holds a pile of the kind (<see cref="BotGlut.Glutted"/>):
    /// a trader carrying a shopkeeper's goods into a glut would be the flood Patrick's point 8 forbids with the trader's own
    /// margin on it (01.10.2026, build 385 review). The bots' shops, the trader's stock and a gathering — all of which drain the
    /// pile — are still weighed.
    /// </summary>
    public static long Glutted { get; internal set; }

    /// <summary>Wants passed over because this trader struck an order on them lately for the buyer's own doing. See <see cref="ShunMs"/>.</summary>
    public static long Shunned { get; internal set; }

    /// <summary>Wants passed over because the road to the source or the buyer crosses ground past the trader's strength. See <c>BotHauler.Deadly</c>.</summary>
    public static long DeadlyRoad { get; internal set; }

    /// <summary>
    /// <b>The dungeon block (03.10.2026, build next).</b> Wants passed over for a buyer down there that a walk or a haul from
    /// outside lately found no way to (<see cref="BotUnreached.Refuses"/>), and wants or sources passed over because the trader,
    /// or the counter, stands in the block and the next leg leaves its own land (<see cref="BotUnreached.Shut"/>). On live 396, 33
    /// of the 35 orders struck "no way through to the buyer" touched the block, and 18 of the 21 failed hauls were these.
    /// </summary>
    public static long Unreached { get; internal set; }

    public static long Stranded { get; internal set; }

    /// <summary>Gatherings not offered because no bot on the island was seen holding the kind this half hour. See <c>BotGlut.Seen</c>.</summary>
    public static long GatherBarren { get; internal set; }

    /// <summary>Gatherings not offered because one for the kind lately brought nothing. See <see cref="GatherShunMs"/>.</summary>
    public static long GatherShunned { get; internal set; }

    // ---- Counters: what the hauls did --------------------------------------------------------------------------------------

    public static long Taken { get; internal set; }

    public static long Finished { get; internal set; }

    public static long FailedHauls { get; internal set; }

    public static long DroppedHauls { get; internal set; }

    public static long Deaths { get; internal set; }

    public static long FromStock { get; internal set; }

    public static long FromShops { get; internal set; }

    public static long FromCounters { get; internal set; }

    public static long BoughtUnits { get; internal set; }

    public static long BoughtGold { get; internal set; }

    public static long Repicked { get; internal set; }

    public static long Gathers { get; internal set; }

    /// <summary>Gatherings said over the trader's own standing want for the kind, topped up rather than refused. See BotHaul.Gather.</summary>
    public static long GatherOverOwn { get; internal set; }

    public static long GatheredUnits { get; internal set; }

    public static long GatheredGold { get; internal set; }

    /// <summary>Gatherings nobody brought anything to.</summary>
    public static long GatherEmpty { get; internal set; }

    /// <summary>Gatherings that came up short and bought the rest at a counter.</summary>
    public static long GatherTopped { get; internal set; }

    public static long Handovers { get; internal set; }

    public static long HandedUnits { get; internal set; }

    public static long HandedGold { get; internal set; }

    /// <summary>What the goods handed over cost the trader: takings less this is the margin.</summary>
    public static long HandedCost { get; internal set; }

    /// <summary>Gold paid above the buyers' own offers, as carriage they agreed to.</summary>
    public static long Carriage { get; internal set; }

    public static long Agreed { get; internal set; }

    public static long Adopted { get; internal set; }

    public static long Leftover { get; internal set; }

    public static long Sailings { get; internal set; }

    public static long Landed { get; internal set; }

    public static long SeaLost { get; internal set; }

    // ---- Counters: neutrality ----------------------------------------------------------------------------------------------

    /// <summary>Times a company's door — enlistment, levy, press, delve, war company, or raising one — was shut to a trader.</summary>
    public static long CompanyRefused { get; private set; }

    /// <summary>A company's call — the hunt's enlistment, a band against a strong foe — never offered to a trader, by review.</summary>
    public static long CallsPassed { get; private set; }

    /// <summary>Coin a trader took on a patrolling guild's land that owed no toll because the trader is neutral.</summary>
    public static long TollWaived { get; private set; }

    /// <summary>
    /// Whether this mobile stands outside every war: a bot of a <see cref="BotClass.Neutral"/> class, while
    /// <see cref="NeutralInWars"/> holds.
    ///
    /// <para>
    /// <b>What neutrality closes, in one place.</b> No guild (<c>BotGuilds.Outside</c> reads the class), so nothing that picks
    /// an enemy or an ally by guild — <c>BotRegard.Hostile</c> and <c>AtWar</c>, the feud and its rallies and quarrels
    /// (<c>BotFeuder</c>), a war company's attackers (<c>BotSquad.Consider</c>), war kills and plunder (<c>BotWar</c>), the
    /// envoys and the duke's meetings — can ever name a trader. No company (<c>BotSquads.Form</c> and <c>Join</c> ask
    /// <see cref="Refuses"/>), so no levy, harrow, delve, press or war company carries one. No toll (<c>BotToll.Rate</c>,
    /// <c>Exempted</c>, the wardens' list). And the producer role keeps it out of the robber's, the skulker's and the
    /// tourney's lists. Self-defence is a reflex and stays one: a trader set upon hits back.
    /// </para>
    /// </summary>
    public static bool Neutral(Mobile m) => NeutralInWars && m is BotMobile { Class: { Neutral: true } };

    /// <summary>A company's door asked about a neutral bot: shut, and counted.</summary>
    public static bool Refuses(Mobile m)
    {
        if (!Neutral(m))
        {
            return false;
        }

        CompanyRefused++;

        return true;
    }

    /// <summary>A proposer of companies asked about a neutral bot: it offers nothing, counted apart from the doors.</summary>
    public static bool Passes(Mobile m)
    {
        if (!Neutral(m))
        {
            return false;
        }

        CallsPassed++;

        return true;
    }

    /// <summary>A neutral bot's coin on a patrolling guild's land, let through toll-free. From <c>BotToll.Exempted</c>.</summary>
    public static void Waive() => TollWaived++;

    /// <summary>
    /// The porter's keep list, for a trader in the middle of a haul: the goods bought for the orders still open in its book are
    /// promised, not surplus, and must ride in the pack to their buyers. From <c>BotUnload.Needed</c>.
    /// </summary>
    public static void Keep(IBotWilful bot, Dictionary<Type, int> keep)
    {
        if (keep == null || bot?.Resolve is not { } resolve)
        {
            return;
        }

        var haul = resolve.Deed as BotHaul ?? resolve.Paused?.Deed as BotHaul;

        if (haul?.Goods is not { } kind)
        {
            return;
        }

        var owed = haul.Owed;

        if (owed <= 0)
        {
            return;
        }

        var already = keep.GetValueOrDefault(kind);

        if (already < int.MaxValue)
        {
            keep[kind] = Math.Max(already, owed);
        }
    }

    // ---- The counters that sell a kind --------------------------------------------------------------------------------------

    private static readonly Dictionary<Type, (List<BaseVendor> Vendors, long Tick)> _sellers = [];

    private static bool _surveyed;

    /// <summary>
    /// Every town's shopkeepers written into <see cref="BotShops"/>' list, once: a trader buys in other towns, and the list
    /// otherwise holds only the counters somebody has already walked near. One spatial query a town.
    /// </summary>
    public static void Survey(Map map)
    {
        if (_surveyed || map == null || map == Map.Internal || !BotTowns.Ready)
        {
            return;
        }

        _surveyed = true;

        var towns = BotTowns.All;
        var found = 0;

        for (var i = 0; i < towns.Count; i++)
        {
            if (towns[i] is { } town && (town.FromHome || town.BySea))
            {
                found += BotShops.Survey(map, town.Square);
            }
        }

        logger.Information("Traders read the counters of every town they can reach: {Found} shopkeepers added to the list", found);
    }

    /// <summary>The shopkeepers on the island that sell this kind now, kept <see cref="SellersMs"/>. Callers test the map.</summary>
    public static List<BaseVendor> Sellers(Type kind)
    {
        var now = Core.TickCount;

        if (!_sellers.TryGetValue(kind, out var known) || now - known.Tick >= SellersMs)
        {
            var list = known.Vendors ?? [];
            list.Clear();

            var shops = BotShops.Shops;

            for (var i = 0; i < shops.Count; i++)
            {
                if (shops[i] is { Deleted: false } vendor && BotShops.Sells(vendor, kind, out _))
                {
                    list.Add(vendor);
                }
            }

            known = (list, now);
            _sellers[kind] = known;
        }

        return known.Vendors;
    }

    /// <summary>
    /// What one of these costs this buyer at its own nearest counter, the walk there and back included at the shard's price of a
    /// shopping minute, spread over the units: the most a buyer would rationally pay a trader to bring them. When no counter
    /// sells the kind, the most the buyer's want could ever rise to.
    /// </summary>
    public static double Alternative(Mobile buyer, Type kind, int units, BotWant want)
    {
        var best = double.MaxValue;
        var vendors = Sellers(kind);
        var walk = BotShopkeep.WalkGold;
        var n = Math.Max(1, units);

        for (var i = 0; i < vendors.Count; i++)
        {
            var vendor = vendors[i];

            if (vendor.Deleted || vendor.Map != buyer?.Map)
            {
                continue;
            }

            var price = BotShops.Price(vendor, kind);

            if (price <= 0)
            {
                continue;
            }

            var per = price + 2.0 * BotAppraisal.Travel(buyer.Location, vendor.Location) * walk / n;

            if (per < best)
            {
                best = per;
            }
        }

        if (best == double.MaxValue)
        {
            best = Math.Max(1, want?.Anchor ?? 1) * BotMarket.MostMultiple;
        }

        // <b>Never past what the material is worth to anybody (02.10.2026, build next; Patrick: "a raw material is never worth
        // more than twice what it becomes").</b> The buyer's own walk to the butcher priced a cook's rib at 16gp and more, so the
        // carriage the trader asked for came to 17–20gp a rib, for a meal worth 6. See BotWant.Ceiling.
        return want is { Ceiling: > 0 } ? Math.Min(best, want.Ceiling) : best;
    }

    /// <summary>
    /// Whether a source costs the trader as much as the want's ceiling or more, so no price the buyer may pay leaves a margin:
    /// counted (<see cref="OverCeiling"/>) and passed over rather than weighed and found thin.
    /// </summary>
    internal static bool OverItsCeiling(BotWant want, int cost)
    {
        // <b>A want read back from the save is not bought for until its ceiling is read (03.10.2026, build next).</b> The board is
        // made before anything is priced, and traders wrote orders for read-back rib wants three seconds after the start, at 20gp
        // a rib against an offer of 6. Raw meat, whose ceiling is the meal and is known the moment the cooking system is built, is
        // read here when the market's beat has not yet, and passed over until it is. Any other kind is left to the beat, whose
        // first look comes after the counters are surveyed (a ceiling read from half the prices would bring an offer down for
        // good), and is weighed meanwhile as a want said now would be (review of build next, F5). See BotMarket.Recap.
        if (want is { CeilingPending: true } && Meat(want.Kind))
        {
            BotMarket.Recap(want);

            if (want.CeilingPending)
            {
                CeilingUnread++;

                return true;
            }
        }

        if (want is not { Ceiling: > 0 } || cost < want.Ceiling)
        {
            return false;
        }

        OverCeiling++;

        return true;
    }

    /// <summary>Sources a trader passed over for costing as much as the want's material ceiling or more. See <see cref="OverItsCeiling"/>.</summary>
    public static long OverCeiling { get; internal set; }

    /// <summary>Wants read back from the save passed over before their material's ceiling could be read. See <see cref="OverItsCeiling"/>.</summary>
    public static long CeilingUnread { get; internal set; }

    /// <summary>Whether a kind is raw meat a cook makes a meal of. See <see cref="BotOven.Raw"/>.</summary>
    private static bool Meat(Type kind)
    {
        var raw = BotOven.Raw;

        for (var i = 0; i < raw.Count; i++)
        {
            if (raw[i] == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// What a trader asks for one, given what it cost: the buyer's own offer when that pays <see cref="LeastMarkup"/> over cost;
    /// else the least price plus <see cref="CarriageShare"/> of the way up to the buyer's own alternative
    /// (<see cref="Alternative"/>), capped at <see cref="MostMarkup"/> over cost; and when the buyer's own walk is cheaper than
    /// the trader's least price, what that walk costs (the margin then usually fails the haul's least profit). Never under the
    /// buyer's offer.
    /// </summary>
    public static int Quote(int cost, int offer, double alternative)
    {
        var least = (int)Math.Ceiling(Math.Max(1, cost) * (1.0 + LeastMarkup));

        if (offer >= least)
        {
            return offer;
        }

        var ceiling = Math.Min((int)Math.Ceiling(Math.Max(1, cost) * (1.0 + MostMarkup)), (int)Math.Floor(alternative));

        if (ceiling <= least)
        {
            return Math.Max(offer, ceiling);
        }

        var price = least + (int)((ceiling - least) * Math.Clamp(CarriageShare, 0.0, 1.0));

        // Night carriage pays a quarter more (07.10.2026, events17), never past the buyer's ceiling. See BotNight.Carriage.
        price = BotNight.Carriage(least, price, ceiling);

        return Math.Max(offer, price);
    }

    /// <summary>What a trader's own stock of a kind is reckoned at: the island's cheapest counter, or the market's own price.</summary>
    public static int BookValue(Type kind, int fallback)
    {
        var lowest = BotShops.Lowest(kind);

        return Math.Max(1, lowest > 0 ? lowest : BotMarket.Worth(kind, fallback));
    }

    private static readonly Dictionary<Type, int> _weights = [];

    /// <summary>The weight of one of a kind, read once off a sample. Whole stones; at least one tenth of nothing.</summary>
    public static double Weight(Type kind)
    {
        if (kind == null)
        {
            return 1.0;
        }

        if (_weights.TryGetValue(kind, out var tenths))
        {
            return tenths / 10.0;
        }

        var sample = kind.CreateInstance<Item>();
        tenths = 10;

        if (sample != null)
        {
            tenths = Math.Max(0, (int)Math.Round(sample.Weight * 10.0));
            sample.Delete();
        }

        _weights[kind] = tenths;

        return tenths / 10.0;
    }

    /// <summary>Whether a kind is goods a trader may carry: not a container, a deed, a key or a book of anybody's.</summary>
    public static bool Goods(Type kind) =>
        kind != null && !typeof(Container).IsAssignableFrom(kind) && !typeof(BaseBoatDeed).IsAssignableFrom(kind)
        && !typeof(HouseDeed).IsAssignableFrom(kind) && !typeof(Key).IsAssignableFrom(kind) && !typeof(Spellbook).IsAssignableFrom(kind)
        && !typeof(Runebook).IsAssignableFrom(kind) && kind.Namespace?.StartsWith("Server.BotAI", StringComparison.Ordinal) != true;

    /// <summary>
    /// Whether this trader can sail: it holds a ship of its own, whatever its wealth, as before.
    /// A haul never buys the ship (06.10.2026, money15): no haul on this island has paid one back — on 04.10 one haul went by sea,
    /// in a ship a journey had bought — so the sea is for the trader that already has one. See <see cref="BotSea.Owns"/>.
    /// </summary>
    public static bool CanSail(BotMobile body) =>
        Sails && body != null && BotSea.Running && BotSea.Ready && BotSea.Owns(body);

    // ---- The line ----------------------------------------------------------------------------------------------------------

    public static string Describe()
    {
        var traders = 0;
        var hauling = 0;
        var keeping = 0;
        var hunting = 0;
        var other = 0;
        var wealth = 0L;
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is not { Deleted: false } bot || bot.Class is not { Neutral: true })
            {
                continue;
            }

            traders++;
            wealth += BotYield.Wealth(bot);

            var kind = bot.Resolve?.Deed?.Kind;

            if (kind == BotHaul.Trade)
            {
                hauling++;
            }
            else if (kind == "shopkeep")
            {
                keeping++;
            }
            else if (kind is "hunt" or "prowl")
            {
                hunting++;
            }
            else
            {
                other++;
            }
        }

        var ended = Finished + FailedHauls + DroppedHauls + Deaths;
        var share = ended > 0 ? Finished * 100.0 / ended : 0.0;
        var margin = HandedGold - HandedCost;

        return $"{traders} traders worth {wealth}gp ({hauling} hauling, {keeping} keeping shop, {hunting} hunting, {other} at other work); "
               + $"hauls: {Taken} taken, {Finished} finished (at least one order handed over), {FailedHauls} failed, {DroppedHauls} ended from outside (put down for good, or a watchdog), {Deaths} died ({share:F1}% finished); "
               + $"{Looks} looks: {Offered} hauls offered, {Poor} too poor to buy (under {LeastCapital}gp), {Barren} found nothing worth hauling; wants passed over: {Fresh} left to near carriers for their first {FreshMs / 1000}s, {ToTraders} a trader's own, {OwnWant} wanted by the trader itself, {NoSource} nowhere to buy, {Thin} under {LeastProfit}gp of margin, {Broke} the buyer could not pay, {Unjoined} across water with no ship, {Perilous} the buyer on deadly ground, {Sealed} no way to the buyer, {Unreached} the buyer down the dungeon block where a walk or a haul from outside lately found no way, {Stranded} wants or counters out of the dungeon block or across its lands from a trader or a counter standing in it, {Heavy} too heavy to carry, {Glutted} shopkeepers' counters not bought from into a glut, {Shunned} lately struck for the buyer's own doing, {DeadlyRoad} over a road that has killed past the trader's strength, {OverCeiling} sources costing as much as the want's material ceiling or more, {CeilingUnread} wants read back from the save before their ceiling could be read; "
               + $"bought: {FromCounters} lots at shopkeepers, {FromShops} at bots' shops, {FromStock} from its own stock, {BoughtUnits} things for {BoughtGold}gp ({Repicked} counters found empty or out of reach and another tried); "
               + $"gathered from the bots of the trader's own town for buyers no carrier reaches: {Gathers} gatherings ({GatherOverOwn} said over the trader's own standing want), {GatheredUnits} things brought for {GatheredGold}gp, {GatherEmpty} brought nothing, {GatherTopped} topped up at a counter, {GatherBarren} not offered for a kind no bot was seen holding, {GatherShunned} not offered after an empty one; "
               + $"sold: {Handovers} handovers of {HandedUnits} things for {HandedGold}gp against wants, margin {margin}gp ({Carriage}gp of it carriage {Agreed} buyers agreed to above their own offer), {Adopted} orders taken up on the road, {Leftover} things left over for the shop; "
               + $"sea: {Sailings} sailings, {Landed} landed, {SeaLost} lost; "
               + $"book: {BotOrders.Describe()}; "
               + $"neutral in wars: {CompanyRefused} company doors shut (joining or raising one), {CallsPassed} company calls not offered (enlistment, bands), {TollWaived} tolls waived; in no guild by class; "
               + $"capital: {PutAways} times a trader put {PutAwayGold}gp in all into its account above a pocket float of {PocketFloat}gp; rest: {RestHeld} traders past their hours kept in the world so no more than {RestShare:P0} of the traders rest at once, {RestEarly} brought back early for it";
    }

    public static void Forget()
    {
        PutAways = 0;
        PutAwayGold = 0;
        OverCeiling = 0;
        CeilingUnread = 0;
        RestHeld = 0;
        RestEarly = 0;
        Looks = 0;
        Poor = 0;
        Barren = 0;
        Offered = 0;
        Fresh = 0;
        ToTraders = 0;
        OwnWant = 0;
        NoSource = 0;
        Thin = 0;
        Unjoined = 0;
        Broke = 0;
        Perilous = 0;
        Sealed = 0;
        Unreached = 0;
        Stranded = 0;
        Heavy = 0;
        Glutted = 0;
        Shunned = 0;
        DeadlyRoad = 0;
        GatherBarren = 0;
        GatherShunned = 0;
        Taken = 0;
        Finished = 0;
        FailedHauls = 0;
        DroppedHauls = 0;
        Deaths = 0;
        FromStock = 0;
        FromShops = 0;
        FromCounters = 0;
        BoughtUnits = 0;
        BoughtGold = 0;
        Repicked = 0;
        Gathers = 0;
        GatherOverOwn = 0;
        GatheredUnits = 0;
        GatheredGold = 0;
        GatherEmpty = 0;
        GatherTopped = 0;
        Handovers = 0;
        HandedUnits = 0;
        HandedGold = 0;
        HandedCost = 0;
        Carriage = 0;
        Agreed = 0;
        Adopted = 0;
        Leftover = 0;
        Sailings = 0;
        Landed = 0;
        SeaLost = 0;
        CompanyRefused = 0;
        CallsPassed = 0;
        TollWaived = 0;
        _sellers.Clear();
        _surveyed = false;
        BotHauler.Forget();
        BotOrders.Forget();
    }
}
