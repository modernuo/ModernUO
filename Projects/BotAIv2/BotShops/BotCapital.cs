using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Britain as the centre of trade: the capital's shopkeepers pay better for goods, a load drawn out of a bank box walks to them
/// when the better price pays for the road, and a bot with a shop's worth of goods, offered a journey, takes it to the capital.
///
/// <para>
/// <b>Patrick's order of 30.09.2026, evening: «rework the trade: the traders must put Britain first as the centre
/// of trade».</b> Measured on the day's events (<c>logs/bot-events.ndjson</c>, 00:00–18:40): Britain already carried half the
/// counter trade — 2031 of 3865 purchases from shopkeepers (64524 of 120124gp), 560 of 1268 sales to them (36091 of 83463gp),
/// 510 of 1158 trips to a bank, 108 of 341 bots' shop shifts — and none of it came from beyond Britain's own ring. Every
/// chooser of a counter was "the nearest", and every other town stands 800 to 2200 tiles off (Skara Brae 803, Cove 826, Yew
/// 992, Trinsic 1124, Minoc 1129, Vesper 1478, Jhelom 2200): of the 3865 purchases only 21 began within 600 tiles of Britain
/// and were made elsewhere. Nothing made the capital worth a road, so nobody took one.
/// </para>
///
/// <para>
/// <b>What makes it worth one, and nobody is ordered there.</b> A road is not free: after build 350 restocks that walked 600
/// tiles and more died 22 times in 191 (11.5 %) against 0.8 % under 200, and journeys between towns 51 times in 195. So the
/// capital is a price and not a rule. Its counters pay <see cref="Premium"/> times their own price, never above what the island's
/// shopkeepers ask for the thing (<see cref="Factor"/>: no counter may pay more than a shelf asks, or buying at one and selling at
/// the next is a trade), the difference out of the city's treasury (<see cref="BotCity.Draw"/>) above <see cref="Reserve"/> — a
/// lever with a budget, never more than the mint — and the peddler weighs every buyer by what the load fetches there less the
/// road at the peddler's own price of a tile (<see cref="BotPeddler.PettyPerTile"/>, the number its "worth less than the walk"
/// gate already reads, so the chooser and the gate cannot disagree). Four hundred gold of ingots from Skara Brae pays for its
/// road; a handful of reagents does not, and goes where it always went.
/// </para>
///
/// <para>
/// <b>Who is pulled, and who trades at home.</b> The premium is counted in the choice (<see cref="Pull"/>) only for a load drawn
/// out of a bank box at a bank for the walk (until 01.10.2026, a load on a stall, which rode safe in the market); goods already
/// riding about in the pack go to the best buyer at its own price — and only where the capital's counter lies within the bot's own
/// leash from home (<see cref="BotPopulation.Leash"/>: a newcomer at a quarter of its trade stays within three hundred tiles of
/// its seat), within <see cref="Reach"/> of the bot, on a land the gates join to the bot's (<see cref="BotGates.Joined"/>), and
/// while the treasury can pay. Jhelom, Minoc, Vesper and the islands trade at home. The price itself is paid to whoever sells
/// at the capital's counters, pulled or not: a price is a price.
/// </para>
///
/// <para>
/// <b>Everything here is counted where it happens</b> — sales and purchases at the capital's counters against everywhere else,
/// the premium paid and the premium the treasury could not meet, loads walked past a nearer buyer, the reasons the price did
/// not pull, shop shifts, and the traders' journeys — and printed in the <c>Trade:</c> line (<see cref="Describe"/>).
/// </para>
/// </summary>
public static class BotCapital
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCapital));

    /// <summary>Whether the capital pays its price and pulls at all. Off, every counter is chosen at its own price and nothing is paid on top.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The town that is the centre of trade: the name of a <c>TownRegion</c> (<see cref="BotTowns"/>).</summary>
    public static string Name { get; set; } = "Britain";

    /// <summary>
    /// What the capital's shopkeepers pay against their own price. A quarter more: on the day's sales that is at most some 375gp
    /// an hour on what was sold there anyway (less where <see cref="Factor"/> cuts it at the shelf), an eighth of the city's mint,
    /// and it pulls the loads worth 0.4 gold a tile of extra road or more — up to some twenty a day by the day's events, most of
    /// them ingots from Skara Brae, Cove and Yew. A half would pull about twice as many.
    /// </summary>
    public static double Premium { get; set; } = 1.25;

    /// <summary>How far from the bot the capital's price reaches in the choice, a backstop behind the bot's own leash. Yew's square is 992 tiles from Britain's.</summary>
    public static int Reach { get; set; } = 1000;

    /// <summary>
    /// What the premium leaves in the city's treasury: <see cref="BotCity.FairReserve"/> unless dialled apart — the board's errands,
    /// the bounties and the standing orders are paid from the same purse, and a fair already leaves them this much.
    /// </summary>
    public static int Reserve
    {
        get => _reserve ?? BotCity.FairReserve;
        set => _reserve = value;
    }

    private static int? _reserve;

    // ---- Counters. Every gate its own name; the Trade: line prints all of them. ----------------------------------------

    /// <summary>Sales over the capital's counters: how many, the things in them, and the shopkeepers' own gold for them.</summary>
    public static long Sales { get; private set; }

    public static long SoldUnits { get; private set; }

    public static long SoldGold { get; private set; }

    /// <summary>The same, over every other counter.</summary>
    public static long AwaySales { get; private set; }

    public static long AwayUnits { get; private set; }

    public static long AwayGold { get; private set; }

    /// <summary>Purchases from the capital's shopkeepers, the gold paid, and the same everywhere else.</summary>
    public static long Purchases { get; private set; }

    public static long PaidGold { get; private set; }

    public static long AwayPurchases { get; private set; }

    public static long AwayPaidGold { get; private set; }

    /// <summary>Sales the city paid the premium on, and what it came to.</summary>
    public static long Premiums { get; private set; }

    public static long PremiumGold { get; private set; }

    /// <summary>Sales whose premium the treasury could not meet in full (at its reserve), and the gold short.</summary>
    public static long Short { get; private set; }

    public static long ShortGold { get; private set; }

    /// <summary>Peddles offered to the capital's counter past a nearer buyer, because the price paid for the road.</summary>
    public static long Offered { get; private set; }

    /// <summary>Of those, loads actually sold there: the loads, their things, the gold (the premium in it), and the extra tiles walked.</summary>
    public static long Carried { get; private set; }

    public static long CarriedUnits { get; private set; }

    public static long CarriedGold { get; private set; }

    public static long CarriedTiles { get; private set; }

    /// <summary>Choices of a buyer where the capital's price would have won and was not counted, by the reason. See <see cref="Pull"/>.</summary>
    public static long Carrying { get; private set; }

    public static long Leashed { get; private set; }

    public static long Afar { get; private set; }

    public static long Unjoined { get; private set; }

    public static long Broke { get; private set; }

    /// <summary>Bots' shop shifts run at the capital's banks, those that sold anything, and the takings; the same elsewhere.</summary>
    public static long Shifts { get; private set; }

    public static long ShiftSales { get; private set; }

    public static long ShiftGold { get; private set; }

    public static long AwayShifts { get; private set; }

    public static long AwayShiftSales { get; private set; }

    public static long AwayShiftGold { get; private set; }

    /// <summary>Journeys of bots with a shop's worth of goods sent to the capital, and traders whose road there was not offered. See <see cref="BotTraveller"/>.</summary>
    public static long Journeys { get; private set; }

    public static long Unroaded { get; private set; }

    private static bool _saidFirst;

    /// <summary>Why the capital's price did or did not count in one choice.</summary>
    public enum Why
    {
        Pulled,
        NotCapital,
        Pack,
        Leash,
        Afar,
        Land,
        Treasury
    }

    /// <summary>The capital as a town, once the towns have been read; null before, or when no town carries <see cref="Name"/>.</summary>
    public static BotTowns.Town Town => Running ? BotTowns.Find(Name) : null;

    /// <summary>Whether this place stands in the capital: inside the box of the town named <see cref="Name"/>.</summary>
    public static bool In(Point3D where)
    {
        if (!Running)
        {
            return false;
        }

        var town = BotTowns.Of(where);

        return town != null && string.Equals(town.Name, Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether the treasury can pay the premium now. What the chooser asks before it counts it, and what the counter pays out of.</summary>
    public static bool Funded => Running && Premium > 1.0 && BotCity.Holds(Reserve);

    /// <summary>
    /// What the capital's counters pay for one of <paramref name="kind"/> against the shopkeeper's own <paramref name="price"/>:
    /// <see cref="Premium"/>, <b>never above what the island's shopkeepers ask for one</b> (<see cref="BotShops.Lowest"/>).
    ///
    /// <para>
    /// <b>A counter that pays more than a shelf asks is a carousel waiting for its errand.</b> The first version lost its economy to
    /// two bots selling the same reagents back and forth four thousand times. The engine's own prices keep a shopkeeper's offer
    /// under its shelf — an iron ingot fetches 4 where it costs 5, bloodmoss 4 against 5 — and a flat quarter on top would lift
    /// arrows (2 against 2), leather (5 against 6) and ribs (6 against 7) to or past their shelf, where buying at one counter and
    /// selling at the next pays. So the premium is cut at the lowest ask: ingots and bloodmoss keep the whole quarter, leather a
    /// fifth, arrows nothing; a thing no shopkeeper sells keeps the whole quarter, and goods of quality that already fetch the
    /// shelf's price or more (an exceptional blade) get nothing on top. One answer, asked by the chooser and by the counter alike.
    /// </para>
    /// </summary>
    public static double Factor(Type kind, int price)
    {
        if (!Running || Premium <= 1.0 || price <= 0)
        {
            return 1.0;
        }

        var shelf = kind == null ? 0 : BotShops.Lowest(kind);

        return shelf <= 0 ? Premium : Math.Clamp(shelf / (double)price, 1.0, Premium);
    }

    /// <summary>What the city owes on top for <paramref name="amount"/> of <paramref name="kind"/> sold at <paramref name="price"/> apiece. See <see cref="Factor"/>.</summary>
    public static long Owed(Type kind, int amount, int price) =>
        amount <= 0 || price <= 0 ? 0 : (long)Math.Floor(amount * (double)price * (Factor(kind, price) - 1.0));

    /// <summary>
    /// Whether the capital's price counts when this bot weighs this shopkeeper, and why not. Asked by the peddler's chooser
    /// (<see cref="BotShops.Buyer(IBotWilful, Item, int, bool, out int, out double, out int)"/>) only for a counter the premium
    /// would make the best, so the land's lookup is paid for a handful of counters a look, not for every one in Britain.
    /// </summary>
    /// <param name="drawn">
    /// The load is drawn out of the bot's bank box at a bank for this walk (01.10.2026; until then, a load waiting safe on a stall);
    /// false for goods already riding about in the pack.
    /// </param>
    public static Why Pull(IBotWilful bot, BaseVendor vendor, bool drawn)
    {
        var body = bot?.Self;

        if (!Running || Premium <= 1.0 || body == null || vendor == null || vendor.Map != body.Map || !In(vendor.Location))
        {
            return Why.NotCapital;
        }

        if (!drawn)
        {
            return Why.Pack;
        }

        if (!Utility.InRange(body.Location, vendor.Location, Reach))
        {
            return Why.Afar;
        }

        if (!Utility.InRange(BotPopulation.Centre(body), vendor.Location, BotPopulation.Leash(body)))
        {
            return Why.Leash;
        }

        if (!BotGates.Joined(body.Map, body.Location, vendor.Location))
        {
            return Why.Land;
        }

        return Funded ? Why.Pulled : Why.Treasury;
    }

    /// <summary>A choice of buyer in which the capital's price would have won and did not count, by the reason. Once a choice.</summary>
    internal static void Passed(Why why)
    {
        switch (why)
        {
            case Why.Pack:
                {
                    Carrying++;
                    break;
                }
            case Why.Leash:
                {
                    Leashed++;
                    break;
                }
            case Why.Afar:
                {
                    Afar++;
                    break;
                }
            case Why.Land:
                {
                    Unjoined++;
                    break;
                }
            case Why.Treasury:
                {
                    Broke++;
                    break;
                }
        }
    }

    /// <summary>A peddle offered to the capital's counter past a nearer buyer.</summary>
    internal static void Offer() => Offered++;

    /// <summary>
    /// A sale over a counter, booked by where the counter stands, and the capital's price paid on top of the shopkeeper's
    /// <paramref name="earned"/> when it stands in the capital. Returns what the city paid.
    ///
    /// <para>
    /// Into the pack first — <c>BaseVendor.OnSellItems</c> has just taken the goods out of it, whole stacks, so the coin has its
    /// place — then the account, then the pack regardless, as a bot's shop pays its keeper (<c>BotShopkeep.Pay</c>): a premium the
    /// city has already taken out of its purse is never lost for want of a slot.
    /// </para>
    /// </summary>
    /// <param name="owed">What the order came to at the capital's price over the shopkeeper's, line by line (<see cref="Owed"/>).</param>
    public static int Sold(Mobile seller, BaseVendor vendor, int units, int earned, long owed)
    {
        if (seller == null || vendor == null || earned <= 0)
        {
            return 0;
        }

        if (!In(vendor.Location))
        {
            AwaySales++;
            AwayUnits += Math.Max(0, units);
            AwayGold += earned;

            return 0;
        }

        Sales++;
        SoldUnits += Math.Max(0, units);
        SoldGold += earned;

        // The engine decides how much of the order it honours, so the premium is never more than the premium on what it paid.
        owed = Math.Min(owed, (long)Math.Floor(earned * Math.Max(0.0, Premium - 1.0)));

        if (owed <= 0)
        {
            return 0;
        }

        var paid = BotCity.Draw((int)Math.Min(owed, int.MaxValue), Reserve);

        if (paid < owed)
        {
            Short++;
            ShortGold += owed - paid;
        }

        if (paid <= 0)
        {
            return 0;
        }

        Hand(seller, paid);

        Premiums++;
        PremiumGold += paid;

        logger.Information(
            "{Name} was paid {Paid}gp more by the city for selling {Units} things at the capital's counter to {Vendor} ({Earned}gp from the shopkeeper; ×{Premium:F2}{Short}); {Purse}gp left in the treasury",
            seller.Name,
            paid,
            units,
            vendor.Name,
            earned,
            Premium,
            paid < owed ? $", {owed - paid}gp of it short at the treasury's reserve" : "",
            BotCity.Purse
        );

        if (!_saidFirst)
        {
            _saidFirst = true;
            BotEvents.Post("market", seller, $"was paid {paid}gp on top by the city for selling at the capital's counter");
        }

        return paid;
    }

    /// <summary>The coin into the seller's hands: the pack, the account, the pack regardless.</summary>
    private static void Hand(Mobile seller, int amount)
    {
        var pack = seller.Backpack;

        if (pack != null && amount <= 60000)
        {
            var coin = new Gold(amount);

            if (pack.TryDropItem(seller, coin, false))
            {
                return;
            }

            coin.Delete();
        }

        if (Banker.Deposit(seller, amount))
        {
            return;
        }

        if (pack == null)
        {
            logger.Error("{Amount}gp the city paid {Name} at the capital's counter had nowhere to go and was lost", amount, seller.Name);

            return;
        }

        while (amount > 0)
        {
            var part = Math.Min(amount, 60000);

            pack.DropItem(new Gold(part));
            amount -= part;
        }
    }

    /// <summary>A load a peddle walked past a nearer buyer to the capital's counter, sold there: the things, the gold with the premium, the extra tiles.</summary>
    internal static void Delivered(int units, int gold, int tiles)
    {
        Carried++;
        CarriedUnits += Math.Max(0, units);
        CarriedGold += Math.Max(0, gold);
        CarriedTiles += Math.Max(0, tiles);
    }

    /// <summary>A purchase from a shopkeeper, booked by where the counter stands. Nothing about the price changes.</summary>
    public static void Bought(BaseVendor vendor, int gold)
    {
        if (vendor == null || gold <= 0)
        {
            return;
        }

        if (In(vendor.Location))
        {
            Purchases++;
            PaidGold += gold;
        }
        else
        {
            AwayPurchases++;
            AwayPaidGold += gold;
        }
    }

    /// <summary>A bots' shop shift that ran its course, booked by the town it stood in. Called from <c>BotShopkeep.Close</c>.</summary>
    public static void Shifted(string town, int sold, int takings)
    {
        var here = town != null && string.Equals(town, Name, StringComparison.OrdinalIgnoreCase);

        if (here)
        {
            Shifts++;
            ShiftSales += sold > 0 ? 1 : 0;
            ShiftGold += Math.Max(0, takings);
        }
        else
        {
            AwayShifts++;
            AwayShiftSales += sold > 0 ? 1 : 0;
            AwayShiftGold += Math.Max(0, takings);
        }
    }

    /// <summary>
    /// Whether this bot is a trader for the journey: it keeps shop (<see cref="BotShopkeeper.Merchant"/>, a maker or gatherer since
    /// 01.10.2026), holds a shop's worth of what the island is short of or somebody asks for (<see cref="BotShopkeep.Look"/> against
    /// <see cref="BotShopkeep.OpenWorth"/>, the very test that offers a shift) and is not an outlaw a town's guards would take for a
    /// fight. See <see cref="BotTraveller"/>.
    /// </summary>
    public static bool Trader(BotMobile bot)
    {
        if (!Running || !BotShopkeep.Running || bot == null || bot.Kills >= 5 || bot.Criminal || BotUnderworld.Member(bot)
            || !BotShopkeeper.FightersKeepShop && !BotShopkeeper.Merchant(bot))
        {
            return false;
        }

        return BotShopkeep.Look(bot).Demanded >= BotShopkeep.OpenWorth;
    }

    /// <summary>A trader's journey sent to the capital, or a trader whose road there was not on offer.</summary>
    internal static void Journey(bool sent)
    {
        if (sent)
        {
            Journeys++;
        }
        else
        {
            Unroaded++;
        }
    }

    private static string Share(long here, long away) => here + away <= 0 ? "0 %" : $"{100.0 * here / (here + away):F0} %";

    public static string Describe() =>
        !Running
            ? "the capital's price is off"
            : $"the capital ({Name}) — {Sales} sales over its counters, {SoldUnits} things for {SoldGold}gp, against {AwaySales} elsewhere for {AwayGold}gp ({Share(SoldGold, AwayGold)} of the gold); "
              + $"{PremiumGold}gp paid on top by the city at ×{Premium:F2} over {Premiums} sales, {Short} short by {ShortGold}gp at the treasury's reserve of {Reserve}gp; "
              + $"{Offered} peddles offered past a nearer buyer and {Carried} sold there ({CarriedUnits} things, {CarriedGold}gp with the premium, {CarriedTiles} tiles further than the nearest), "
              + $"its price not counted {Leashed} times beyond the bot's leash, {Afar} beyond {Reach} tiles, {Unjoined} on a land no gate joins, {Broke} with the treasury at its reserve and {Carrying} for goods carried in the pack; "
              + $"{Purchases} purchases from its shopkeepers for {PaidGold}gp against {AwayPurchases} elsewhere for {AwayPaidGold}gp ({Share(PaidGold, AwayPaidGold)}); "
              + $"{Shifts} bots' shop shifts at its banks ({ShiftSales} sold anything, {ShiftGold}gp) against {AwayShifts} elsewhere ({AwayShiftSales}, {AwayShiftGold}gp); "
              + $"{Journeys} traders' journeys sent to it and {Unroaded} traders with no road to it on offer";

    public static void Forget()
    {
        Sales = 0;
        SoldUnits = 0;
        SoldGold = 0;
        AwaySales = 0;
        AwayUnits = 0;
        AwayGold = 0;
        Purchases = 0;
        PaidGold = 0;
        AwayPurchases = 0;
        AwayPaidGold = 0;
        Premiums = 0;
        PremiumGold = 0;
        Short = 0;
        ShortGold = 0;
        Offered = 0;
        Carried = 0;
        CarriedUnits = 0;
        CarriedGold = 0;
        CarriedTiles = 0;
        Carrying = 0;
        Leashed = 0;
        Afar = 0;
        Unjoined = 0;
        Broke = 0;
        Shifts = 0;
        ShiftSales = 0;
        ShiftGold = 0;
        AwayShifts = 0;
        AwayShiftSales = 0;
        AwayShiftGold = 0;
        Journeys = 0;
        Unroaded = 0;
        _saidFirst = false;
    }
}
