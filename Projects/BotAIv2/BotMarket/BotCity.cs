using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// The city's treasury: the one purse on the shard that mints coin, and everything the city does is paid from it.
///
/// <para>
/// <b>A lever with a budget is a lever a model may hold.</b> Until 16.09.2026 the city was one button on the
/// administrator's dashboard — buy ten lots from afar, paid for out of nothing, without limit — and for that reason it
/// could be given to nobody else. Patrick's order: whatever the city can do must be in Argus's and the debuggers'
/// hands. So the city has a purse. It fills at <see cref="MintPerHour"/> up to <see cref="Cap"/> and never faster,
/// every purchase comes out of it, and a purchase it cannot pay for is refused and counted. A watcher that buys
/// everything on the market at three in the morning has spent the treasury, not printed one.
/// </para>
///
/// <para>
/// <b>Two ways the city spends.</b> A buy takes whole lots from the bots' shops standing open by the bank, and pays what was
/// asked. A standing order names a thing, an amount and a price, and the clock takes that thing from any open shop whenever it
/// is on show at or under the price — the outside demand a producer sees as goods that sell as fast as they are made. Both are
/// booked as sales, so the shops learn from them like from any other.
/// </para>
///
/// <para>
/// <b>At the shops, never off stalls (01.10.2026, the auction removed on Patrick's order of 30.09).</b> The city used to buy off
/// the placeless stalls from wherever it was; now it buys only where a keeper is standing at its pitch with the goods
/// (<see cref="BotShopkeep.CityBuy"/>): the town's own buyer at the market, as a shopkeeper buys over its counter. The goods
/// leave the world as they always did — bought by somewhere that is not here — and the keeper is paid in coin into its pack.
/// </para>
/// </summary>
public static class BotCity
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCity));

    /// <summary>Whether the treasury mints, serves and sells at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>What the treasury opens with at a boot.</summary>
    public static int Opening { get; set; } = 1000;

    /// <summary>What the mint adds in an hour, spread over the clock's ticks.</summary>
    public static int MintPerHour { get; set; } = 3000;

    /// <summary>The most the treasury may hold. The mint stops here; spending starts it again.</summary>
    public static int Cap { get; set; } = 20000;

    /// <summary>How often the clock turns: the mint, and the standing orders served at the bots' shops.</summary>
    public static int EveryMs { get; set; } = 60000;

    /// <summary>Most standing orders at once.</summary>
    public static int MostWants { get; set; } = 8;

    /// <summary>What is in the treasury now.</summary>
    public static int Purse { get; private set; }

    public static long Minted { get; private set; }

    public static long Spent { get; private set; }

    public static long Lots { get; private set; }

    public static long Units { get; private set; }

    /// <summary>Things bought against a standing order.</summary>
    public static long Served { get; private set; }

    /// <summary>Standing orders filled to the last unit.</summary>
    public static long Filled { get; private set; }

    /// <summary>Buys refused because the treasury was empty.</summary>
    public static long Refused { get; private set; }

    /// <summary>What the guilds have paid the city for ground (and for houses, and what outlaws were fined), all told, every coin into the purse.</summary>
    public static long Taxed { get; private set; }

    /// <summary>Of <see cref="Taxed"/>, what came in while the purse stood at or above <see cref="Cap"/>: until build 408, destroyed.</summary>
    public static long AboveCap { get; private set; }

    /// <summary>
    /// Coin the guilds paid for ground, into the treasury. Patrick's fifth City idea of 16.09.2026.
    ///
    /// <para>
    /// <b>The cap is the mint's ceiling, not the purse's (06.10.2026, build 408).</b> This took only what fitted under
    /// <see cref="Cap"/> and the rest of the payment was simply gone: on build 405 26 claims paid 104,000gp and about 64,000 of
    /// it was destroyed against a full purse — 05:05 to 05:10 the city was taxed 23,000 more, spent 3,409 and held 933 more,
    /// so 18,658gp vanished in five minutes. A payment is coin that already exists, and Patrick's rule is that no money is
    /// made or destroyed quietly: the whole of it goes into the purse, and the mint (<see cref="Mint"/>), which makes coin,
    /// still stops at the cap. What came in above the cap is counted (<see cref="AboveCap"/>), and the fairs, errands and
    /// bounties spend it like any other coin in the purse.
    /// </para>
    /// </summary>
    public static void Tax(int coin)
    {
        if (coin <= 0)
        {
            return;
        }

        Taxed += coin;
        AboveCap += coin - Math.Min(coin, Math.Max(0, Cap - Purse));
        Purse = (int)Math.Min(int.MaxValue, (long)Purse + coin);
    }

    /// <summary>Claim money paid back to the guilds for squares on town ground (07.10.2026, <c>BotTownGround</c>), and of it how much the purse could not cover.</summary>
    public static long Unclaimed { get; private set; }

    public static long UnclaimedShort { get; private set; }

    /// <summary>
    /// <b>Claim money going back where it came from</b> (Patrick, 07.10.2026: "remove such claims right now and give the guilds
    /// their money back"). As much of <paramref name="coin"/> as the purse holds leaves it; the caller puts the whole of
    /// <paramref name="coin"/> in the guild's chest, so what the purse could not cover is counted here, never hidden. Not
    /// booked as spent: the ground was never sold, the payment is undone.
    /// </summary>
    public static int Unclaim(int coin)
    {
        if (coin <= 0)
        {
            return 0;
        }

        var paid = Math.Min(coin, Math.Max(0, Purse));

        Purse -= paid;
        Unclaimed += coin;
        UnclaimedShort += coin - paid;

        return paid;
    }

    /// <summary>What the city has paid on top of the capital's counters, and over how many sales. See <see cref="BotCapital"/>.</summary>
    public static long CapitalPaid { get; private set; }

    public static long CapitalSales { get; private set; }

    /// <summary>Whether the purse holds anything above <paramref name="reserve"/>: what the capital's chooser asks before it counts the premium.</summary>
    public static bool Holds(int reserve) => Running && Purse > Math.Max(0, reserve);

    /// <summary>
    /// <b>The capital's price, out of the purse (30.09.2026, Patrick's evening order: Britain the centre of trade).</b> As much of
    /// <paramref name="gold"/> as the purse holds above <paramref name="reserve"/>, taken out and booked as spent; the caller
    /// hands it to the seller (<see cref="BotCapital.Sold"/>). Nothing when the city is not running. Out of the treasury rather
    /// than made on the spot, so the premium is a lever with a budget: never more than the mint, and stopped by an empty purse
    /// the way a buy or a fair is.
    /// </summary>
    public static int Draw(int gold, int reserve)
    {
        if (!Running || gold <= 0)
        {
            return 0;
        }

        var paid = Math.Min(gold, Math.Max(0, Purse - Math.Max(0, reserve)));

        if (paid <= 0)
        {
            return 0;
        }

        Purse -= paid;
        Spent += paid;
        CapitalPaid += paid;
        CapitalSales++;

        return paid;
    }

    /// <summary>Coin taken out of the purse and held for a promise on the board of errands. See <see cref="BotQuests"/>.</summary>
    public static long Escrowed { get; private set; }

    /// <summary>The purse asked to hold this much for an errand; false, and nothing moves, if it cannot.</summary>
    public static bool Escrow(int gold)
    {
        if (gold <= 0 || Purse < gold)
        {
            return false;
        }

        Purse -= gold;
        Escrowed += gold;

        return true;
    }

    /// <summary>
    /// Coin an errand read back from the save already holds: taken from the purse before the restart, and still owed. The
    /// purse itself is read back without it, so only the count of what is held is put right — after build 95's boot the
    /// city's line said "0gp held for the board's errands" with a 150gp errand standing on the board.
    /// </summary>
    public static void Held(int gold)
    {
        if (gold > 0)
        {
            Escrowed += gold;
        }
    }

    /// <summary>Held coin back in the purse: the errand lapsed or was withdrawn.</summary>
    public static void Refund(int gold)
    {
        if (gold <= 0)
        {
            return;
        }

        Purse += gold;
        Escrowed = Math.Max(0, Escrowed - gold);
    }

    /// <summary>Held coin paid out to whoever did the errand.</summary>
    public static void Disbursed(int gold)
    {
        if (gold <= 0)
        {
            return;
        }

        Escrowed = Math.Max(0, Escrowed - gold);
        Spent += gold;
    }

    /// <summary>
    /// <b>For Patrick's reset (07.10.2026, reset26): whatever is still counted as held, once every holder has handed its coin back,
    /// goes back into the purse.</b> Every holder of the purse's coin — the board's errands, the events, the watches' wages, the
    /// stewards' coffers — is wiped by the reset and hands back what it holds through <see cref="Refund"/> first. What is left of
    /// <see cref="Escrowed"/> after that was taken out of the purse for a promise nobody can keep any more: a holder whose own
    /// account of it was lost (a deleted bot's deed is forgotten, never dropped). It is the purse's coin, so it goes back into the
    /// purse rather than staying counted as held by nobody — and the next boot, which counts as held only what a store reads back,
    /// would otherwise lose it without a word. Said in the log when it is not nought. Returns the gold.
    /// </summary>
    public static int Unhold()
    {
        var rest = (int)Math.Min(int.MaxValue, Math.Max(0L, Escrowed));

        if (rest > 0)
        {
            logger.Warning("The treasury: {Gold}gp was still counted as held with no holder left after the wipe; back into the purse", rest);
            Refund(rest);
        }

        Escrowed = 0;

        return rest;
    }

    private sealed class Order
    {
        public Type Kind;

        public string Name;

        public int Amount;

        public int Price;

        public string By;
    }

    private static readonly List<Order> _wants = [];

    /// <summary>A bounty on ground: paid, split evenly, to the company that clears the square it is in.</summary>
    private sealed class Prize
    {
        public Map Map;

        public Point3D Where;

        public int Gold;

        public string By;

        public long SetTick;
    }

    /// <summary>A bounty on a red's head: paid to whoever catches it.</summary>
    private sealed class Price
    {
        public Serial Who;

        public string Name;

        public int Gold;

        public string By;
    }

    private static readonly List<Prize> _bounties = [];

    private static readonly List<Price> _heads = [];

    /// <summary>How long a bounty on ground stands before it lapses. Two hours.</summary>
    public static int BountyMs { get; set; } = 7200000;

    // ---- Prices on kinds (02.10.2026, build 389; Patrick's option 3: half the mint from the fairs to bounties on the kinds that
    // kill bots, split like spoils, the mint itself unchanged) ------------------------------------------------------------
    //
    // <b>The mint reached the rich.</b> On build 386 the treasury minted 22,000gp in 7.5 hours and spent 13,700 of it at four
    // fairs, buying from the ten bots who keep shops: 44 shifts earned 14,920gp, 339gp a shift, the best-paid work on the shard,
    // while quests and bounties paid nothing. A price on the head of what has lately killed bots is pay per kill, like the
    // corpse's own gold, and it goes to whoever opens the corpse — the hunters, most of them poor.

    private sealed class KindPrice
    {
        public Type Kind;

        public string Name;

        /// <summary>A head's price.</summary>
        public int Gold;

        /// <summary>Of ours that fell to the kind inside the window, when the price was last set.</summary>
        public int Kills;

        public long SetTick;

        public int Paid;

        public int Heads;
    }

    private sealed class Killer
    {
        public double Power;

        public int Kills;

        public long LastTick;
    }

    private static readonly List<KindPrice> _kinds = [];

    private static readonly Dictionary<Type, Killer> _killers = [];

    /// <summary>What each hand has collected in heads inside its hour. See <see cref="HeadHourCap"/>.</summary>
    private static readonly Dictionary<Serial, (long Since, int Gold)> _collected = [];

    private static readonly List<Mobile> _hands = [];

    /// <summary>The share of each mint that goes to the heads' purse. Half.</summary>
    public static double HeadShare { get; set; } = 0.5;

    /// <summary>The most the heads' purse holds; what it cannot take goes to the treasury as before.</summary>
    public static int HeadCap { get; set; } = 10000;

    /// <summary>What is in the heads' purse now.</summary>
    public static int HeadPurse { get; private set; }

    /// <summary>A head's price lies between these, reckoned at the kind's strength over <see cref="HeadPerPower"/>.</summary>
    public static int HeadLeast { get; set; } = 20;

    public static int HeadMost { get; set; } = 60;

    public static double HeadPerPower { get; set; } = 25.0;

    /// <summary>How often the killers are read and the prices set.</summary>
    public static int HeadEveryMs { get; set; } = 600000;

    /// <summary>How many of ours a kind must have killed inside <see cref="BountyMs"/> to carry a price.</summary>
    public static int HeadKills { get; set; } = 2;

    /// <summary>How many kinds carry a price at once.</summary>
    public static int MostKinds { get; set; } = 6;

    /// <summary>The most one bot collects in heads in an hour: the farming guard.</summary>
    public static int HeadHourCap { get; set; } = 300;

    public static long KindsPosted { get; private set; }

    public static long KindsLapsed { get; private set; }

    public static long KindHeadsPaid { get; private set; }

    public static long KindGold { get; private set; }

    public static long KindsCapped { get; private set; }

    public static long KindsUnpaid { get; private set; }

    /// <summary>Kinds carrying a price now. For the store's line.</summary>
    public static int KindPrices => _kinds.Count;

    private static long _huntedTick;

    /// <summary>Most bounties on ground at once.</summary>
    public static int MostBounties { get; set; } = 6;

    /// <summary>Bounties on ground paid out, and what they came to.</summary>
    public static long BountiesPaid { get; private set; }

    public static long BountyGold { get; private set; }

    /// <summary>Bounties on heads paid out.</summary>
    public static long HeadsPaid { get; private set; }

    /// <summary>Murderers laid low that the city paid for, and what it came to.</summary>
    public static long BloodPaid { get; private set; }

    public static long BloodGold { get; private set; }

    /// <summary>Times the treasury could not cover the price of blood in full.</summary>
    public static long BloodShort { get; private set; }

    /// <summary>Bounties that lapsed unclaimed.</summary>
    public static long Lapsed { get; private set; }

    /// <summary>
    /// Prices on heads moved onto a red's new body after a boot.
    ///
    /// <para>
    /// <b>The same loss as the outlaws' cells, one store over (C6).</b> A price is written with the red's body and read back
    /// onto it; then <c>BotPopulation.PurgeSaved</c> deletes every bot the save brought back and raises the population
    /// again by name, and the first tick found the body gone and took the price down without a line — so every restart
    /// cleared every price on a head, as it had let every prisoner out until build 101d. Found on 17.09.2026 by reading the
    /// stores for anything kept by body after 101d's rebinding was seen to work ("1 records moved onto a bot's new body
    /// after a boot", 10:35). Moved by name once the population stands, and judged red on the next tick, after the
    /// outlaws have moved their own record.
    /// </para>
    /// </summary>
    public static long HeadsRebound { get; private set; }

    private static Clock _timer;

    /// <summary>How long a fair runs. An hour: Patrick's proposal of 16.09.2026, "an hour, the city takes everything on the stalls at 80%" — the shops by the bank since 01.10.2026.</summary>
    public static int FairMs { get; set; } = 3600000;

    /// <summary>The share of the asking price the city pays at a fair.</summary>
    public static double FairShare { get; set; } = 0.8;

    /// <summary>How many lots the fair takes from the open shops at each turn of the clock.</summary>
    public static int FairLots { get; set; } = 20;

    /// <summary>How often the city declares a fair on its own, or nought for only by hand. Six hours.</summary>
    public static int FairEveryMs { get; set; } = 21600000;

    /// <summary>The least in the treasury for a fair to be declared on its own.</summary>
    public static int FairFloor { get; set; } = 5000;

    /// <summary>
    /// What a fair leaves in the treasury, however long it runs: the board's errands, the bounties and the standing orders
    /// are paid from the same purse.
    ///
    /// <para>
    /// <b>A fair of an hour spent in a minute, and spent to nothing.</b> The first fair Hermes called, at 21:22 on
    /// 16.09.2026, took 932 things for 3899gp in its first minute and left a gold piece in the treasury; when a 5000gp claim
    /// tax came in four minutes later it took 942 things for 3812gp in the next one. Every stall that filled after the
    /// first minute met a fair with nothing to pay, and nothing could be posted on the board for the hour it took the mint
    /// to refill the purse. Spent evenly over the minutes left, above this reserve.
    /// </para>
    /// </summary>
    public static int FairReserve { get; set; } = 2000;

    private static long _fairUntil;

    private static long _fairDeclaredTick;

    private static long _bornTick;

    private static double _fairShare = 0.8;

    private static int _fairStalls;

    private static int _fairUnits;

    private static int _fairGold;

    /// <summary>Whether a fair is on now.</summary>
    public static bool Fairing => _fairUntil != 0 && _fairUntil - Core.TickCount > 0;

    /// <summary>Fairs declared, by hand or by the clock.</summary>
    public static long Fairs { get; private set; }

    /// <summary>
    /// Standing orders, bounties, prices on heads and fairs the city's verbs actually set or changed. A verb answers in a
    /// sentence whether it did anything or not; this is how a caller tells an accepted order from a refusal without
    /// reading the sentence. See <c>BotMarshal.Act</c>.
    /// </summary>
    public static long Changes { get; private set; }

    /// <summary>Lots (a kind at one shop), things and gold taken at fairs, all told.</summary>
    public static long FairStalls { get; private set; }

    public static long FairUnits { get; private set; }

    public static long FairGold { get; private set; }

    /// <summary>
    /// A fair: for so many minutes the city takes what the bots' shops by the bank hold at a share of the asking price, so many lots
    /// a minute, out of the treasury. The surplus's exit and the market's day out, in one event; the tournament and
    /// the revel are the other two under that roof (BotTourney, the debuggers' revel). Patrick's fifth City idea of
    /// 16.09.2026, taken that evening.
    /// </summary>
    public static string Fair(int minutes, double share, string who)
    {
        if (!Running)
        {
            return "the city is not buying anything at the moment.";
        }

        if (minutes <= 0 || minutes > 1440)
        {
            return "a fair runs between 1 and 1440 minutes.";
        }

        if (share <= 0.0 || share > 1.0)
        {
            return "the fair's share of the asking price is between 1 and 100 percent.";
        }

        var now = Core.TickCount;

        _fairUntil = now + minutes * 60000L;
        _fairDeclaredTick = now;
        _fairShare = share;
        _fairStalls = 0;
        _fairUnits = 0;
        _fairGold = 0;
        Fairs++;
        Changes++;

        logger.Information(
            "{Who} declared a fair: for {Minutes} minutes the city takes what the bots' shops by the bank hold at {Share:P0} of the asking price, {Lots} lots a minute, out of {Purse}gp",
            who,
            minutes,
            share,
            FairLots,
            Purse
        );

        return $"the fair is on: for {minutes} minutes the city takes what the bots' shops by the bank hold at {share:P0} of the asking price, {FairLots} lots a minute; {Purse}gp in the treasury.";
    }

    private static void Trade()
    {
        var now = Core.TickCount;

        if (_fairUntil != 0 && _fairUntil - now <= 0)
        {
            _fairUntil = 0;

            logger.Information(
                "The fair is over: {Stalls} lots and {Units} things taken at the bots' shops for {Gold}gp; {Purse}gp left in the treasury",
                _fairStalls,
                _fairUnits,
                _fairGold,
                Purse
            );
        }

        if (_fairUntil == 0)
        {
            var since = _fairDeclaredTick != 0 ? now - _fairDeclaredTick : now - _bornTick;

            if (FairEveryMs > 0 && Purse >= FairFloor && since >= FairEveryMs)
            {
                Fair(Math.Max(1, FairMs / 60000), FairShare, "the clock");
            }

            return;
        }

        // Spent evenly over what is left of the fair, above the reserve. See FairReserve.
        var left = Math.Max(1L, (_fairUntil - now + 59999) / 60000);
        var budget = (int)Math.Min(int.MaxValue, Math.Max(0L, Purse - FairReserve) / left);

        if (budget <= 0)
        {
            return;
        }

        var (stalls, units, paid) = BotShopkeep.CityBuy(null, int.MaxValue, 0, budget, _fairShare, FairLots, "the fair");

        if (stalls <= 0)
        {
            return;
        }

        Purse -= paid;
        Spent += paid;
        Lots += stalls;
        Units += units;
        _fairStalls += stalls;
        _fairUnits += units;
        _fairGold += paid;
        FairStalls += stalls;
        FairUnits += units;
        FairGold += paid;

        logger.Information(
            "The fair took {Units} things in {Stalls} lots from the bots' shops for {Paid}gp at {Share:P0}; {Minutes:F0} minutes left, {Purse}gp in the treasury",
            units,
            stalls,
            paid,
            _fairShare,
            (_fairUntil - now) / 60000.0,
            Purse
        );
    }

    private static string FairTell() =>
        Fairing
            ? $"a fair is on for another {(_fairUntil - Core.TickCount) / 60000.0:F0} minutes at {_fairShare:P0}"
            : "no fair on";

    /// <summary>Whether a save was read back at this boot, so <see cref="Start"/> does not put the opening sum over it.</summary>
    private static bool _restored;

    public static void Start()
    {
        Stop();

        // The opening sum only when nothing was read back: every restart used to empty a full treasury. See BotCityStore.
        if (!_restored)
        {
            Purse = Opening;
        }

        _bornTick = Core.TickCount;
        _timer = new Clock();
        _timer.Start();
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    private static void Tick()
    {
        if (!Running)
        {
            return;
        }

        Mint();
        Hunted();
        Serve();
        Trade();
        Lapse();
    }

    /// <summary>
    /// A bounty on ground: the city pays the company that clears the square, split evenly among whoever is in it when
    /// the ground is done. The Baron goes to bountied ground before the map's own choice — see BotHarrower — which is
    /// the whole point: a watcher that sees trouble at a place can send him there.
    /// </summary>
    public static string Bounty(Map map, int x, int y, int gold, string who)
    {
        if (!Running)
        {
            return "the city is not paying anything at the moment.";
        }

        if (map == null || map == Map.Internal)
        {
            return "the city has no map to put a bounty on.";
        }

        if (gold <= 0 || gold > Cap)
        {
            return $"a bounty is between 1 and {Cap}gp.";
        }

        if (!BotStep.Settle(map, x, y, out var z))
        {
            return $"nothing can stand at ({x}, {y}), so no company could clear it.";
        }

        var at = new Point3D(x, y, z);
        var key = BotQuad.Key(map, at);

        for (var i = 0; i < _bounties.Count; i++)
        {
            if (BotQuad.Key(_bounties[i].Map, _bounties[i].Where) == key)
            {
                _bounties[i].Gold = gold;
                _bounties[i].By = who;
                _bounties[i].SetTick = Core.TickCount;
                Changes++;

                return $"the bounty on the square at ({x}, {y}) is now {gold}gp; the Baron goes there before anywhere else.";
            }
        }

        if (_bounties.Count >= MostBounties)
        {
            return $"the city holds at most {MostBounties} bounties on ground at once.";
        }

        _bounties.Add(new Prize { Map = map, Where = at, Gold = gold, By = who, SetTick = Core.TickCount });
        Changes++;

        logger.Information("{Who} had the city put {Gold}gp on the square at ({X}, {Y}); the Baron is sent there first", who, gold, x, y);

        return $"the city puts {gold}gp on the square at ({x}, {y}); the Baron goes there before anywhere else, and the company that clears it is paid. {Purse}gp in the treasury.";
    }

    /// <summary>The nearest square the city has put a bounty on, settled on the tile named, or nothing.</summary>
    public static Point3D Bountied(Map map, Point3D from, int within)
    {
        Prize best = null;
        var closest = int.MaxValue;

        for (var i = 0; i < _bounties.Count; i++)
        {
            var bounty = _bounties[i];

            if (bounty.Map != map)
            {
                continue;
            }

            var away = Math.Max(Math.Abs(bounty.Where.X - from.X), Math.Abs(bounty.Where.Y - from.Y));

            if (away > within || away >= closest)
            {
                continue;
            }

            closest = away;
            best = bounty;
        }

        return best?.Where ?? Point3D.Zero;
    }

    /// <summary>The ground was cleared: the bounty on its square, if any, paid out evenly to the company. Returns what was paid.</summary>
    public static int Claim(Map map, Point3D where, IReadOnlyList<IBotSquadMember> company)
    {
        if (map == null || company == null)
        {
            return 0;
        }

        var key = BotQuad.Key(map, where);

        for (var i = 0; i < _bounties.Count; i++)
        {
            var bounty = _bounties[i];

            if (BotQuad.Key(bounty.Map, bounty.Where) != key)
            {
                continue;
            }

            _bounties.RemoveAt(i);

            var heads = 0;

            for (var j = 0; j < company.Count; j++)
            {
                if (company[j]?.Self is { Deleted: false, Alive: true })
                {
                    heads++;
                }
            }

            var pot = Math.Min(bounty.Gold, Purse);

            if (heads == 0 || pot <= 0)
            {
                logger.Information("The bounty of {Gold}gp on ({X}, {Y}) lapsed: {Why}", bounty.Gold, bounty.Where.X, bounty.Where.Y, heads == 0 ? "nobody left to pay" : "the treasury is empty");
                Lapsed++;

                return 0;
            }

            var share = pot / heads;
            var paid = 0;

            for (var j = 0; j < company.Count; j++)
            {
                if (company[j]?.Self is { Deleted: false, Alive: true } member && Pay(member, share))
                {
                    paid += share;
                }
            }

            Purse -= paid;
            Spent += paid;
            BountiesPaid++;
            BountyGold += paid;

            logger.Information(
                "The city paid its bounty on ({X}, {Y}): {Paid}gp to {Heads} of the company, {Share}gp each (put up by {By}); {Purse}gp left in the treasury",
                bounty.Where.X,
                bounty.Where.Y,
                paid,
                heads,
                share,
                bounty.By,
                Purse
            );

            return paid;
        }

        return 0;
    }

    /// <summary>One of ours fell to <paramref name="killer"/>. Read from <c>BotKept.Fell</c>; a bot, a pet or a summons is not a kind.</summary>
    public static void Fell(Mobile killer)
    {
        if (!Running || killer is not BaseCreature { Deleted: false, Controlled: false, Summoned: false } creature || killer is BotMobile)
        {
            return;
        }

        var kind = creature.GetType();

        if (!_killers.TryGetValue(kind, out var known))
        {
            known = new Killer();
            _killers[kind] = known;
        }

        known.Kills++;
        known.Power = BotThreat.Power(creature);
        known.LastTick = Core.TickCount;
    }

    private static KindPrice Find(Type kind)
    {
        for (var i = 0; i < _kinds.Count; i++)
        {
            if (_kinds[i].Kind == kind)
            {
                return _kinds[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Every <see cref="HeadEveryMs"/>: the kinds that killed most of ours inside the window, <see cref="HeadKills"/> at the least,
    /// carry a price between <see cref="HeadLeast"/> and <see cref="HeadMost"/> by their strength; a standing price is refreshed, a
    /// new one takes the place of the least-killing kind when all <see cref="MostKinds"/> are taken.
    /// </summary>
    private static void Hunted()
    {
        var now = Core.TickCount;

        if (_huntedTick != 0 && now - _huntedTick < HeadEveryMs)
        {
            return;
        }

        _huntedTick = now;

        List<Type> gone = null;

        foreach (var (kind, killer) in _killers)
        {
            if (now - killer.LastTick >= BountyMs)
            {
                (gone ??= []).Add(kind);
            }
        }

        for (var i = 0; gone != null && i < gone.Count; i++)
        {
            _killers.Remove(gone[i]);
        }

        List<KeyValuePair<Type, Killer>> worst = [];

        foreach (var pair in _killers)
        {
            if (pair.Value.Kills >= HeadKills)
            {
                worst.Add(pair);
            }
        }

        worst.Sort(static (a, b) => b.Value.Kills.CompareTo(a.Value.Kills));

        for (var i = 0; i < worst.Count && i < MostKinds; i++)
        {
            var kind = worst[i].Key;
            var killer = worst[i].Value;
            var gold = Math.Clamp((int)(killer.Power / Math.Max(1.0, HeadPerPower)), HeadLeast, HeadMost);
            var head = Find(kind);

            if (head != null)
            {
                head.Gold = gold;
                head.Kills = killer.Kills;
                head.SetTick = now;

                continue;
            }

            if (_kinds.Count >= MostKinds)
            {
                KindPrice least = null;

                for (var k = 0; k < _kinds.Count; k++)
                {
                    if (least == null || _kinds[k].Kills < least.Kills)
                    {
                        least = _kinds[k];
                    }
                }

                if (least == null || least.Kills >= killer.Kills)
                {
                    continue;
                }

                _kinds.Remove(least);
                KindsLapsed++;
            }

            _kinds.Add(new KindPrice { Kind = kind, Name = kind.Name, Gold = gold, Kills = killer.Kills, SetTick = now });
            KindsPosted++;

            logger.Information(
                "The city puts {Gold}gp on the head of every {Kind}: {Kills} of ours fell to them in the last {Hours} hours; {Purse}gp in the heads' purse",
                gold,
                kind.Name,
                killer.Kills,
                BountyMs / 3600000,
                HeadPurse
            );
        }
    }

    private static void LapseKinds(long now)
    {
        for (var i = _kinds.Count - 1; i >= 0; i--)
        {
            if (now - _kinds[i].SetTick < BountyMs)
            {
                continue;
            }

            logger.Information("The price of {Gold}gp on {Kind}'s head lapsed after {Hours} hours without another of ours falling to one; {Paid}gp was paid for {Heads} heads", _kinds[i].Gold, _kinds[i].Name, BountyMs / 3600000, _kinds[i].Paid, _kinds[i].Heads);
            _kinds.RemoveAt(i);
            KindsLapsed++;
        }
    }

    /// <summary>A lone hunter went through the corpse of one of <paramref name="kind"/>: the price on its head, if any, to the hunter.</summary>
    public static int Collect(Type kind, Mobile hand)
    {
        _hands.Clear();

        if (hand != null)
        {
            _hands.Add(hand);
        }

        return PayHeads(kind);
    }

    /// <summary>A company's corpse: the price split between the claimants like the corpse's own gold. See <c>BotSpoils.Share</c>.</summary>
    public static int Collect(Type kind, IReadOnlyList<IBotSquadMember> hands)
    {
        _hands.Clear();

        for (var i = 0; hands != null && i < hands.Count; i++)
        {
            if (hands[i]?.Self is { } self)
            {
                _hands.Add(self);
            }
        }

        return PayHeads(kind);
    }

    private static int PayHeads(Type kind)
    {
        if (!Running || kind == null || _hands.Count == 0)
        {
            return 0;
        }

        var head = Find(kind);

        if (head == null)
        {
            return 0;
        }

        var now = Core.TickCount;
        var heads = 0;

        for (var i = _hands.Count - 1; i >= 0; i--)
        {
            var hand = _hands[i];

            if (hand is not { Deleted: false, Alive: true })
            {
                _hands.RemoveAt(i);

                continue;
            }

            // The farming guard: a hand at its hour's cap takes no share, and the share goes to the others.
            if (_collected.TryGetValue(hand.Serial, out var took) && now - took.Since < 3600000L && took.Gold >= HeadHourCap)
            {
                KindsCapped++;
                _hands.RemoveAt(i);

                continue;
            }

            heads++;
        }

        if (heads == 0)
        {
            return 0;
        }

        var pot = Math.Min(head.Gold, HeadPurse);
        var share = pot / heads;

        if (share <= 0)
        {
            KindsUnpaid++;

            return 0;
        }

        var paid = 0;

        for (var i = 0; i < _hands.Count; i++)
        {
            var hand = _hands[i];

            if (!Pay(hand, share))
            {
                continue;
            }

            paid += share;

            var since = _collected.TryGetValue(hand.Serial, out var took) && now - took.Since < 3600000L ? took.Since : now;
            var gold = since == now ? 0 : took.Gold;

            _collected[hand.Serial] = (since, gold + share);
        }

        if (_collected.Count > 4096)
        {
            _collected.Clear();
        }

        HeadPurse -= paid;
        Spent += paid;
        KindHeadsPaid++;
        KindGold += paid;
        head.Paid += paid;
        head.Heads++;

        // The price counts towards what the kind pays, so the hunters that rank quarry by its gold see it. See BotQuarry.Rewarded.
        BotQuarry.Rewarded(kind, paid);

        logger.Information(
            "The city paid {Paid}gp for a {Kind}'s head, {Share}gp to each of {Heads}; {Purse}gp left in the heads' purse",
            paid,
            head.Name,
            share,
            heads,
            HeadPurse
        );

        return paid;
    }

    /// <summary>A bounty on a red's head, paid to whoever catches it. See BotOutlaw.Jail.</summary>
    public static string Head(BotMobile red, int gold, string who)
    {
        if (!Running)
        {
            return "the city is not paying anything at the moment.";
        }

        if (red is not { Deleted: false })
        {
            return "no such bot.";
        }

        // <b>Wanted is enough, and it has to be.</b> This asked for red until the night of 18.09.2026, when the price
        // Patrick set on an exposed fence — 5000gp the moment a witness tells — was refused here every time, because a
        // fence that has killed nobody is grey and not red. The caller's own line said the city had put the price on,
        // and the city had not; a log that says a thing happened when it did not is worse than no log.
        if (!BotOutlaw.Outlaw(red))
        {
            return $"{red.Name} is neither red nor wanted; the city puts a price only on an outlaw's head.";
        }

        if (gold <= 0 || gold > Cap)
        {
            return $"a bounty is between 1 and {Cap}gp.";
        }

        for (var i = 0; i < _heads.Count; i++)
        {
            if (_heads[i].Who == red.Serial)
            {
                _heads[i].Gold = gold;
                _heads[i].By = who;
                Changes++;

                return $"the price on {red.Name}'s head is now {gold}gp.";
            }
        }

        _heads.Add(new Price { Who = red.Serial, Name = red.Name, Gold = gold, By = who });
        Changes++;

        logger.Information("{Who} had the city put {Gold}gp on {Name}'s head", who, gold, red.Name);

        return $"the city puts {gold}gp on {red.Name}'s head, paid to whoever catches it. {Purse}gp in the treasury.";
    }

    /// <summary>A red was caught: the price on its head, if any, paid to the catcher.</summary>
    public static void Collect(BotMobile red, Mobile by)
    {
        if (red == null)
        {
            return;
        }

        for (var i = 0; i < _heads.Count; i++)
        {
            if (_heads[i].Who != red.Serial)
            {
                continue;
            }

            var head = _heads[i];

            _heads.RemoveAt(i);

            var pot = Math.Min(head.Gold, Purse);

            if (pot <= 0 || by is not { Deleted: false } || !Pay(by, pot))
            {
                logger.Information("The price of {Gold}gp on {Name}'s head lapsed: {Why}", head.Gold, head.Name, pot <= 0 ? "the treasury is empty" : "nobody to pay");
                Lapsed++;

                return;
            }

            Purse -= pot;
            Spent += pot;
            HeadsPaid++;
            BountyGold += pot;

            logger.Information("The city paid {By} {Gold}gp for {Name}'s head (put up by {Who}); {Purse}gp left in the treasury", by.Name, pot, head.Name, head.By, Purse);

            return;
        }
    }

    /// <summary>
    /// The price of blood: what the city pays for a murderer laid low, split evenly between the hands that did it.
    ///
    /// <para>
    /// <b>Patrick's order of 17.09.2026, night.</b> The price rises with the murders proved against the dead bot
    /// (<c>BotUnderworld.Price</c>); the Baron's own kill pays nobody, an ordinary bot's kill pays it the whole, and a
    /// band's kill is split between them. Paid at the moment of the kill out of the purse, like a bounty on ground and
    /// unlike an errand, which is held out of it at the posting; a treasury too thin pays what it has and says so.
    /// </para>
    /// </summary>
    public static int Blood(Mobile red, IReadOnlyList<Mobile> hands, int gold)
    {
        if (!Running || red == null || gold <= 0 || hands == null || hands.Count == 0)
        {
            return 0;
        }

        var heads = 0;

        for (var i = 0; i < hands.Count; i++)
        {
            if (hands[i] is { Deleted: false, Alive: true })
            {
                heads++;
            }
        }

        var pot = Math.Min(gold, Purse);

        if (heads == 0 || pot <= 0)
        {
            BloodShort++;

            logger.Information(
                "The price of {Gold}gp on {Name}'s blood went unpaid: {Why}",
                gold,
                red.Name,
                heads == 0 ? "nobody left to pay" : "the treasury is empty"
            );

            return 0;
        }

        if (pot < gold)
        {
            BloodShort++;
        }

        var share = pot / heads;
        var paid = 0;

        for (var i = 0; i < hands.Count; i++)
        {
            if (hands[i] is { Deleted: false, Alive: true } hand && Pay(hand, share))
            {
                paid += share;
            }
        }

        Purse -= paid;
        Spent += paid;
        BloodPaid++;
        BloodGold += paid;

        logger.Information(
            "The city paid {Paid}gp for {Name}'s blood, {Share}gp to each of {Heads} (the price was {Gold}); {Purse}gp left in the treasury",
            paid,
            red.Name,
            share,
            heads,
            gold,
            Purse
        );

        return paid;
    }

    /// <summary>Bounties off the towns' wanted lists paid out of the purse, and what they came to. See <see cref="Reward"/>.</summary>
    public static long Rewards { get; private set; }

    public static long RewardGold { get; private set; }

    /// <summary>
    /// A bounty off a town's wanted list, paid out of the purse into its captor's pack (07.10.2026, crime17): as much of
    /// <paramref name="gold"/> as the purse holds, booked as spent. Returns what was paid. See <c>BotCrime.Caught</c>.
    /// </summary>
    public static int Reward(Mobile to, int gold)
    {
        if (!Running || gold <= 0 || to is not { Deleted: false })
        {
            return 0;
        }

        var paid = Math.Min(gold, Purse);

        if (paid <= 0 || !Pay(to, paid))
        {
            return 0;
        }

        Purse -= paid;
        Spent += paid;
        Rewards++;
        RewardGold += paid;

        return paid;
    }

    private static bool Pay(Mobile to, int coin)
    {
        if (coin <= 0 || to?.Backpack is not { } pack)
        {
            return false;
        }

        pack.DropItem(new Gold(coin));

        return true;
    }

    /// <summary>The purse, the standing orders and the bounties, for <see cref="BotCityStore"/>.</summary>
    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;

        writer.WriteEncodedInt(Purse);

        writer.WriteEncodedInt(_wants.Count);

        for (var i = 0; i < _wants.Count; i++)
        {
            writer.Write(_wants[i].Name ?? "");
            writer.WriteEncodedInt(_wants[i].Amount);
            writer.WriteEncodedInt(_wants[i].Price);
            writer.Write(_wants[i].By ?? "");
        }

        writer.WriteEncodedInt(_bounties.Count);

        for (var i = 0; i < _bounties.Count; i++)
        {
            var bounty = _bounties[i];

            writer.Write(bounty.Map);
            writer.Write(bounty.Where);
            writer.WriteEncodedInt(bounty.Gold);
            writer.Write(bounty.By ?? "");
            writer.Write(Math.Max(0L, now - bounty.SetTick));
        }

        writer.WriteEncodedInt(_heads.Count);

        for (var i = 0; i < _heads.Count; i++)
        {
            writer.Write(World.FindMobile(_heads[i].Who));
            writer.Write(_heads[i].Name ?? "");
            writer.WriteEncodedInt(_heads[i].Gold);
            writer.Write(_heads[i].By ?? "");
        }

        // Shape 2 (02.10.2026, build 389): the heads' purse and the prices on kinds. See BotCityStore.
        writer.WriteEncodedInt(HeadPurse);
        writer.WriteEncodedInt(_kinds.Count);

        for (var i = 0; i < _kinds.Count; i++)
        {
            var head = _kinds[i];

            writer.Write(head.Name ?? "");
            writer.WriteEncodedInt(head.Gold);
            writer.WriteEncodedInt(head.Kills);
            writer.Write(Math.Max(0L, now - head.SetTick));
            writer.WriteEncodedInt(head.Paid);
            writer.WriteEncodedInt(head.Heads);
        }
    }

    /// <summary>
    /// The treasury read back. A standing order for a thing this build does not know is dropped; a bounty is set back by
    /// its age, so one older than <see cref="BountyMs"/> lapses at the first tick, with its own line; a price on a head
    /// whose red is gone is dropped.
    /// </summary>
    internal static (int Purse, int Orders, int Bounties, int Heads) Load(IGenericReader reader, int shape)
    {
        var now = Core.TickCount;

        Purse = Math.Max(0, reader.ReadEncodedInt());
        _restored = true;

        _wants.Clear();

        var orders = reader.ReadEncodedInt();

        for (var i = 0; i < orders; i++)
        {
            var name = reader.ReadString();
            var amount = reader.ReadEncodedInt();
            var price = reader.ReadEncodedInt();
            var by = reader.ReadString();
            var kind = string.IsNullOrEmpty(name) ? null : AssemblyHandler.FindTypeByName(name);

            if (kind == null || !typeof(Item).IsAssignableFrom(kind) || amount <= 0 || price <= 0)
            {
                continue;
            }

            _wants.Add(new Order { Kind = kind, Name = kind.Name, Amount = amount, Price = price, By = string.IsNullOrEmpty(by) ? null : by });
        }

        _bounties.Clear();

        var bounties = reader.ReadEncodedInt();

        for (var i = 0; i < bounties; i++)
        {
            var map = reader.ReadMap();
            var where = reader.ReadPoint3D();
            var gold = reader.ReadEncodedInt();
            var by = reader.ReadString();
            var age = reader.ReadLong();

            if (map == null || map == Map.Internal || gold <= 0)
            {
                continue;
            }

            _bounties.Add(new Prize { Map = map, Where = where, Gold = gold, By = string.IsNullOrEmpty(by) ? null : by, SetTick = now - Math.Max(0L, age) });
        }

        _heads.Clear();

        var heads = reader.ReadEncodedInt();

        for (var i = 0; i < heads; i++)
        {
            var red = reader.ReadEntity<BotMobile>();
            var name = reader.ReadString();
            var gold = reader.ReadEncodedInt();
            var by = reader.ReadString();

            if (red is not { Deleted: false } || gold <= 0)
            {
                continue;
            }

            _heads.Add(new Price { Who = red.Serial, Name = string.IsNullOrEmpty(name) ? red.Name : name, Gold = gold, By = string.IsNullOrEmpty(by) ? null : by });
        }

        _kinds.Clear();
        HeadPurse = 0;

        // A shape-1 save ends here; the heads' purse starts empty and fills from the next mint.
        if (shape >= 2)
        {
            HeadPurse = Math.Max(0, reader.ReadEncodedInt());

            var kinds = reader.ReadEncodedInt();

            for (var i = 0; i < kinds; i++)
            {
                var name = reader.ReadString();
                var gold = reader.ReadEncodedInt();
                var kills = reader.ReadEncodedInt();
                var age = reader.ReadLong();
                var paid = reader.ReadEncodedInt();
                var paidHeads = reader.ReadEncodedInt();
                var kind = string.IsNullOrEmpty(name) ? null : AssemblyHandler.FindTypeByName(name);

                if (kind == null || gold <= 0)
                {
                    continue;
                }

                _kinds.Add(new KindPrice { Kind = kind, Name = kind.Name, Gold = gold, Kills = kills, SetTick = now - Math.Max(0L, age), Paid = paid, Heads = paidHeads });
            }
        }

        return (Purse, _wants.Count, _bounties.Count, _heads.Count);
    }

    private static void Lapse()
    {
        var now = Core.TickCount;

        LapseKinds(now);

        for (var i = _bounties.Count - 1; i >= 0; i--)
        {
            if (now - _bounties[i].SetTick < BountyMs)
            {
                continue;
            }

            logger.Information("The bounty of {Gold}gp on ({X}, {Y}) lapsed after {Hours} hours unclaimed", _bounties[i].Gold, _bounties[i].Where.X, _bounties[i].Where.Y, BountyMs / 3600000);
            _bounties.RemoveAt(i);
            Lapsed++;
        }

        for (var i = _heads.Count - 1; i >= 0; i--)
        {
            var red = World.FindMobile(_heads[i].Who) as BotMobile;

            if (red is not { Deleted: false })
            {
                // The body went with the boot's purge: the same bot stands again under its name. See HeadsRebound.
                var again = Named(_heads[i].Name);

                if (again != null)
                {
                    _heads[i].Who = again.Serial;
                    HeadsRebound++;

                    continue;
                }

                // Not raised yet, so not gone yet.
                if (BotPopulation.Count == 0)
                {
                    continue;
                }
            }
            else if (BotOutlaw.IsRed(red))
            {
                continue;
            }

            // No longer red — the hour ran out, or it is gone — and nobody caught it. The price comes down.
            _heads.RemoveAt(i);
            Lapsed++;
        }
    }

    /// <summary>The living bot of the population with this name, or null.</summary>
    private static BotMobile Named(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && bot.Name == name)
            {
                return bot;
            }
        }

        return null;
    }

    /// <summary>The bounties standing, for the door and the dashboard.</summary>
    public static string Bounties()
    {
        if (_bounties.Count == 0 && _heads.Count == 0 && _kinds.Count == 0)
        {
            return "no bounties";
        }

        var say = ValueStringBuilder.Create(256);

        try
        {
            var shown = 0;

            for (var i = 0; i < _bounties.Count; i++)
            {
                if (shown++ > 0)
                {
                    say.Append("; ");
                }

                say.Append(_bounties[i].Gold);
                say.Append("gp on (");
                say.Append(_bounties[i].Where.X);
                say.Append(", ");
                say.Append(_bounties[i].Where.Y);
                say.Append(")");
            }

            for (var i = 0; i < _heads.Count; i++)
            {
                if (shown++ > 0)
                {
                    say.Append("; ");
                }

                say.Append(_heads[i].Gold);
                say.Append("gp on ");
                say.Append(_heads[i].Name);
                say.Append("'s head");
            }

            for (var i = 0; i < _kinds.Count; i++)
            {
                if (shown++ > 0)
                {
                    say.Append("; ");
                }

                say.Append(_kinds[i].Gold);
                say.Append("gp a head on ");
                say.Append(_kinds[i].Name);
                say.Append(" (");
                say.Append(_kinds[i].Kills);
                say.Append(" of ours fell to them, ");
                say.Append(_kinds[i].Heads);
                say.Append(" heads paid)");
            }

            return say.ToString();
        }
        finally
        {
            say.Dispose();
        }
    }

    private static void Mint()
    {
        var coin = (int)((long)MintPerHour * EveryMs / 3600000L);

        if (coin <= 0)
        {
            return;
        }

        // <b>Half of each mint to the heads' purse (02.10.2026, build 389; Patrick's option 3).</b> The same coin, minted at the
        // same rate; only where it goes changes: the fairs and the lots draw on the treasury, which now gets the other half. What
        // the heads' purse cannot take at its cap goes to the treasury as before.
        var heads = Math.Min((int)(coin * Math.Clamp(HeadShare, 0.0, 1.0)), Math.Max(0, HeadCap - HeadPurse));

        HeadPurse += heads;
        Minted += heads;

        var rest = coin - heads;

        if (rest <= 0 || Purse >= Cap)
        {
            return;
        }

        var added = Math.Min(rest, Cap - Purse);

        Purse += added;
        Minted += added;
    }

    /// <summary>The city buys whole lots at the bots' shops open by the bank, for what is in the treasury.</summary>
    public static (int Lots, int Units, int Paid) Buy(int lots, string who)
    {
        if (!Running || lots <= 0)
        {
            return (0, 0, 0);
        }

        if (Purse <= 0)
        {
            Refused++;

            return (0, 0, 0);
        }

        var (taken, units, paid) = BotShopkeep.CityBuy(null, int.MaxValue, 0, Purse, 1.0, lots, who);

        Purse -= paid;
        Spent += paid;
        Lots += taken;
        Units += units;

        if (taken > 0)
        {
            logger.Information(
                "{Who} had the city buy {Units} things in {Lots} lots at the bots' shops for {Paid}gp; {Purse}gp left in the treasury",
                who,
                units,
                taken,
                paid,
                Purse
            );
        }

        return (taken, units, paid);
    }

    /// <summary>A standing order: the city takes this thing at the bots' shops whenever it is on show at or under the price.</summary>
    public static string Want(string name, int amount, int price, string who)
    {
        if (!Running)
        {
            return "the city is not buying anything at the moment.";
        }

        var kind = AssemblyHandler.FindTypeByName(name);

        if (kind == null || !typeof(Item).IsAssignableFrom(kind))
        {
            return $"the city does not know a thing called {name}.";
        }

        if (amount <= 0 || price <= 0)
        {
            return "a standing order needs an amount and a price, both above nought.";
        }

        for (var i = 0; i < _wants.Count; i++)
        {
            if (_wants[i].Kind == kind)
            {
                _wants[i].Amount = amount;
                _wants[i].Price = price;
                _wants[i].By = who;
                Changes++;

                return $"the city's standing order for {kind.Name} is now {amount} at up to {price}gp each; {Purse}gp in the treasury.";
            }
        }

        if (_wants.Count >= MostWants)
        {
            return $"the city holds at most {MostWants} standing orders; forget one first.";
        }

        _wants.Add(new Order { Kind = kind, Name = kind.Name, Amount = amount, Price = price, By = who });
        Changes++;

        logger.Information(
            "{Who} had the city put out a standing order for {Amount} {Kind} at up to {Price}gp each",
            who,
            amount,
            kind.Name,
            price
        );

        return $"the city now wants {amount} {kind.Name} at up to {price}gp each, at the bots' shops by the bank as they show it; {Purse}gp in the treasury.";
    }

    /// <summary>A standing order withdrawn.</summary>
    public static string Forget(string name)
    {
        for (var i = 0; i < _wants.Count; i++)
        {
            if (_wants[i].Name.InsensitiveEquals(name))
            {
                _wants.RemoveAt(i);

                return $"the city's standing order for {name} is withdrawn.";
            }
        }

        return $"the city has no standing order for {name}.";
    }

    private static void Serve()
    {
        if (_wants.Count == 0 || Purse <= 0)
        {
            return;
        }

        for (var i = _wants.Count - 1; i >= 0; i--)
        {
            var want = _wants[i];
            var (_, units, paid) = BotShopkeep.CityBuy(want.Kind, want.Amount, want.Price, Purse, 1.0, int.MaxValue, "the city's standing order");

            if (units <= 0)
            {
                continue;
            }

            Purse -= paid;
            Spent += paid;
            Units += units;
            Served += units;
            want.Amount -= units;

            logger.Information(
                "The city took {Units} {Kind} at the bots' shops for {Paid}gp against its standing order; {Left} still wanted, {Purse}gp in the treasury",
                units,
                want.Name,
                paid,
                Math.Max(0, want.Amount),
                Purse
            );

            if (want.Amount <= 0)
            {
                _wants.RemoveAt(i);
                Filled++;
            }
        }
    }

    /// <summary>The standing orders, for the door and the dashboard.</summary>
    public static string Wants()
    {
        if (_wants.Count == 0)
        {
            return "no standing orders";
        }

        var say = ValueStringBuilder.Create(256);

        try
        {
            for (var i = 0; i < _wants.Count; i++)
            {
                if (i > 0)
                {
                    say.Append("; ");
                }

                say.Append(_wants[i].Amount);
                say.Append(" ");
                say.Append(_wants[i].Name);
                say.Append(" at up to ");
                say.Append(_wants[i].Price);
                say.Append("gp (");
                say.Append(_wants[i].By ?? "somebody");
                say.Append(")");
            }

            return say.ToString();
        }
        finally
        {
            say.Dispose();
        }
    }

    public static string Describe() =>
        !Running
            ? "the city is not buying"
            : $"the treasury holds {Purse}gp of {Cap} (minted {Minted}, taxed {Taxed} off the guilds' ground, {AboveCap} of it above the cap and kept, {Unclaimed} refunded for town ground ({UnclaimedShort} of it beyond the purse, the city's debt), spent {Spent} on {Lots} lots and {Units} things, {Served} of them against standing orders, {Filled} orders filled, {Refused} buys refused for want of coin; {BountiesPaid} bounties on ground and {HeadsPaid} on heads paid, {BountyGold}gp in all, {Lapsed} lapsed, {BloodPaid} murderers paid for at {BloodGold}gp and {BloodShort} prices the treasury could not meet, {HeadsRebound} prices on heads moved onto a new body after a boot); prices on kinds: {KindsPosted} posted and {KindsLapsed} lapsed, {KindHeadsPaid} heads paid for {KindGold}gp ({KindsCapped} hands at their hour's {HeadHourCap}gp, {KindsUnpaid} the heads' purse could not pay), {HeadPurse}gp in the heads' purse of {HeadCap} ({HeadShare:P0} of the mint); {CapitalPaid}gp paid on top of the capital's counters over {CapitalSales} sales; {Escrowed}gp held for the board's errands; {FairTell()}, {Fairs} fairs held and {FairGold}gp spent at them; standing orders: {Wants()}; bounties: {Bounties()}";

    public static void Forget()
    {
        _restored = false;
        _fairUntil = 0;
        _fairDeclaredTick = 0;
        Fairs = 0;
        FairStalls = 0;
        FairUnits = 0;
        FairGold = 0;
        _wants.Clear();
        _bounties.Clear();
        _heads.Clear();
        BountiesPaid = 0;
        HeadsPaid = 0;
        BountyGold = 0;
        Lapsed = 0;
        HeadsRebound = 0;
        _kinds.Clear();
        _killers.Clear();
        _collected.Clear();
        _huntedTick = 0;

        // <b>The heads' purse goes into the treasury, not nowhere (07.10.2026, reset26 review).</b> It is minted coin (Mint sends
        // its share there) kept by the store, and this zeroed it at every reset and every reload of the market: up to HeadCap of the
        // city's money destroyed without a word, against Patrick's rule that no coin is made or lost quietly. The prices on kinds
        // it paid for are forgotten above; the coin itself stays the city's. Added with a ceiling, so it cannot overflow the purse.
        Purse = (int)Math.Min(int.MaxValue, (long)Purse + Math.Max(0, HeadPurse));
        HeadPurse = 0;
        KindsPosted = 0;
        KindsLapsed = 0;
        KindHeadsPaid = 0;
        KindGold = 0;
        KindsCapped = 0;
        KindsUnpaid = 0;
        Taxed = 0;
        AboveCap = 0;
        Unclaimed = 0;
        UnclaimedShort = 0;
        Minted = 0;
        Spent = 0;
        Lots = 0;
        Units = 0;
        Served = 0;
        Filled = 0;
        Refused = 0;
        CapitalPaid = 0;
        CapitalSales = 0;
    }

    private sealed class Clock : Timer
    {
        public Clock() : base(TimeSpan.FromMilliseconds(Math.Max(1000, EveryMs)), TimeSpan.FromMilliseconds(Math.Max(1000, EveryMs)))
        {
        }

        protected override void OnTick() => Tick();
    }
}
