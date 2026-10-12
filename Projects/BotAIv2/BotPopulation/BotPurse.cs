using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// What a bot keeps in its pocket, and what it puts away the moment it is standing somewhere it can.
///
/// <para>
/// <b>Carrying a fortune is two costs, and a bot had no way of noticing either.</b> Coin weighs — a thousand
/// pieces is twenty stones against a mage's ceiling of a hundred and twenty-seven — and coin is not bound, so
/// every piece of it drops into the corpse when something finally wins. A hunter that has been paid by forty
/// skeletons was walking around wearing its whole career.
/// </para>
///
/// <para>
/// <b>Banked as a side effect rather than as an errand, and that is deliberate.</b> Everything a bot chooses
/// is weighed in takings per minute, and moving coin from a pocket to an account produces nothing by that
/// measure — purse and account are both counted as wealth, so the trip would score zero and never be chosen,
/// however sensible it is. What it is instead is something a bot does while it happens to be at a counter,
/// which it already is several times an hour: the shops, the forge and the bank are the same few streets.
/// </para>
///
/// <para>
/// The float is what it keeps for its own errands — cloth, paper, herbs, bandages, a replacement blade — so
/// banking never leaves a bot unable to afford the work it was about to do.
/// </para>
/// </summary>
public static class BotPurse
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPurse));

    /// <summary>
    /// Coin a bot keeps on it. Enough for the dearest ordinary errand — twenty blank scrolls and a fresh
    /// tool — and not so much that a corpse is worth robbing.
    /// </summary>
    /// <remarks>
    /// <b>Lowered to a hundred on 24.08.2026, by order, and it had to move together with the reason for
    /// going.</b> A bot now walks to a counter because it is carrying coin — see <see cref="BotUnload.Purse"/>
    /// — and if what it keeps back were larger than what sends it, every such trip would bank nothing and be
    /// made again on the next beat. The two numbers are one decision written in two places: go at two
    /// hundred and fifty, come away with a hundred.
    /// </remarks>
    public static int Float { get; set; } = 100;

    /// <summary>
    /// Coin a bot keeps back rather than spend on what it buys: materials, bullion, restocking, gear upkeep, a horse.
    ///
    /// <para>
    /// <b>One fact, which five proposers each kept a number for.</b> Stores and bullion kept 150, restocking 100, upkeep and
    /// the stable 200 — five reserves for "never spend past your own footing", drifted apart the way two numbers for one
    /// fact always do, and the dearest of them priced the poorest crafters out of their own materials. One number since
    /// 15.09.2026, a hundred, which is the shopper's and the float's; each proposer's <c>Reserve</c> reads this unless it is
    /// dialled apart. The armourer keeps its own four hundred on purpose: it keeps back the price of the piece up to that,
    /// so a cheap sleeve still asks for little (see <c>BotArmourer.Keeps</c>). Fable's option, taken on Patrick's word.
    /// </para>
    /// </summary>
    public static int KeepBack { get; set; } = 100;

    /// <summary>
    /// What this bot keeps on it rather than banking: the float, or the price of a horse it is saving for.
    ///
    /// <para>
    /// <b>One place answers this, and it had to be one place.</b> Two different subsystems bank a bot's
    /// purse — this one, as a side effect of standing at a counter, and the unloading errand, which walks to
    /// a counter on purpose — and a rule written into either of them alone is a rule the other one undoes.
    /// That is the shape this project keeps paying for: <c>Rearm</c> was taught not to arm a bot with a
    /// butcher's knife and <c>Draw</c> was not, and a captain spent a morning holding one.
    /// </para>
    ///
    /// <para>
    /// <b>Why a saver keeps it in its pocket at all.</b> A shopkeeper is paid out of the pack, so a bot that
    /// banks everything above a hundred can never be carrying the seven hundred a horse costs — and the two
    /// rules would otherwise fight over the same coins at the same counter on the same beat, one drawing them
    /// out and the other putting them back. The coin is exposed to death while it is carried, and that is the
    /// real price of the horse being bought at all: seven hundred, once, and never again for that bot.
    /// </para>
    /// </summary>
    public static int Keeps(Mobile bot) =>
        BotFence.Is(bot)
            ? Math.Max(Float, BotFence.Most)
            : bot is BotMobile rider && BotStable.Wants(rider)
                ? Math.Max(Float, BotSteed.Price + BotStable.Reserve)
                : Float;

    /// <summary>How near a counter the bot has to be. The same reach the miner banks its takings from.</summary>
    public static int Reach { get; set; } = 3;

    // ---- Banking before a trip (02.10.2026, build next; Patrick's option 3: "bank before a trip") ---------------------------------

    /// <summary>Whether a bot leaving town for work out of it puts its pocket over <see cref="TripFloat"/> in the bank first.</summary>
    public static bool BanksBeforeTrips { get; set; } = true;

    /// <summary>
    /// What a bot keeps in its pocket when it sets out of town: a small working purse. Fifty: every counter and every meeting on
    /// this shard draws on the account (<c>BotShops.Buy</c>, <c>BotMarket.Charge</c>), so the pocket buys nothing a trip needs
    /// that the bank does not; what it holds is what a death takes.
    /// </summary>
    public static int TripFloat { get; set; } = 50;

    /// <summary>The least surplus worth a deposit as the bot leaves. Twenty.</summary>
    public static int TripLeast { get; set; } = 20;

    /// <summary>How near a banker a bot in a town must stand for its surplus to go in as it leaves: within the town's own streets.</summary>
    public static int TripBankTiles { get; set; } = 40;

    /// <summary>Deposits made as a bot set out of town, and their gold.</summary>
    public static long TripBanks { get; private set; }

    public static long TripGold { get; private set; }

    /// <summary>Trips out of town begun by a bot already out of every town with a surplus in its pocket, and that surplus: what this cannot reach.</summary>
    public static long TripAfield { get; private set; }

    public static long TripAfieldGold { get; private set; }

    /// <summary>Trips begun in a town with no banker within <see cref="TripBankTiles"/>, the surplus kept.</summary>
    public static long TripNoBank { get; private set; }

    /// <summary>
    /// <b>Bank before a trip.</b> Called when a bot takes on a piece of work, by the auction or pressed (<c>BotWill</c>): when the
    /// work lies outside every town or goes where the fighting is (<see cref="BotDeed.Braves"/>), and the bot is standing in a town
    /// with a banker within <see cref="TripBankTiles"/>, everything in its pocket over <see cref="TripFloat"/> — or over the work's own
    /// outlay, when that is more — goes into its account. Returns what went in.
    ///
    /// <para>
    /// <b>Measured before it (builds 390–392, 14:21–16:12, the died-ending coin of 266 deaths):</b> 10,288gp of pocket lost, 39gp a
    /// death; the 19 deaths that lost 150gp or more (7 %) were 53 % of it — Godfrey 556 on his way to a counter, Falk 439 prowling,
    /// Faelar 400. The float banked only at a counter within three tiles (<see cref="Bank"/>, 2 to 27 deposits a session) and the
    /// porter set out at 250 (<c>BotUnload.Purse</c>), so a bot paid in town walked out of it with the whole of its pay.
    /// </para>
    ///
    /// <para>
    /// The reservation for the bot's wants on the board (build 391) reads pack and account together (<see cref="BotYield.Wealth"/>)
    /// and is paid at the meeting out of the account when the pack is short (<c>BotMarket.Charge</c>), so it stays payable. Not
    /// for a bot on a stipend (its steward draws it into the pocket), a fence (its trade is paid from the pocket) or a trader
    /// (<see cref="BotTrading.PutAway"/> already keeps its float). The coin leaves the pack before the account is credited, as in
    /// <see cref="Bank"/>.
    /// </para>
    /// </summary>
    public static int BeforeTrip(IBotWilful bot, BotDeed deed)
    {
        if (!BanksBeforeTrips || deed == null || bot?.Self is not BotMobile { Deleted: false, Alive: true } body
            || body.Map is not { } map || map == Map.Internal || body.Backpack is not { } pack)
        {
            return 0;
        }

        if (body.Class is { Stipend: > 0 } || BotFence.Is(body) || BotTrading.Trader(body) || deed.Paperwork || deed.Standing)
        {
            return 0;
        }

        var keep = Math.Max(TripFloat, deed.Outlay);
        var excess = pack.GetAmount(typeof(Gold)) - keep;

        if (excess < Math.Max(1, TripLeast))
        {
            return 0;
        }

        // A company's work goes where its fights are, wherever it was raised: 112 of the 266 deaths measured were enlisted.
        var where = deed.Where;
        var leaving = deed.Braves || deed.Kind == BotEnlist.Trade || where != Point3D.Zero && BotTowns.Of(where) == null;

        if (!leaving)
        {
            return 0;
        }

        if (BotTowns.Of(body.Location) == null)
        {
            TripAfield++;
            TripAfieldGold += excess;

            return 0;
        }

        var banker = false;

        foreach (var near in map.GetMobilesInRange<Banker>(body.Location, Math.Max(1, TripBankTiles)))
        {
            if (near is { Deleted: false })
            {
                banker = true;

                break;
            }
        }

        if (!banker)
        {
            TripNoBank++;

            return 0;
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

        // Counted apart from the counter's own deposits above, so the line's first figure keeps meaning what it meant.
        TripBanks++;
        TripGold += excess;

        logger.Information(
            "{Name} put {Gold}gp in the bank before setting out on {Kind}, keeping {Keep}",
            body.Name,
            excess,
            deed.Kind,
            keep
        );

        return excess;
    }

    /// <summary>The trip deposits, for the Money line.</summary>
    public static string DescribeTrips() =>
        !BanksBeforeTrips
            ? "nothing is banked before a trip"
            : $"{TripBanks} deposits worth {TripGold}gp made by bots setting out of town (keeping {TripFloat}gp), {TripAfield} trips begun out of town with {TripAfieldGold}gp over the purse, {TripNoBank} begun in a town with no banker within {TripBankTiles} tiles";

    /// <summary>Coin put away, all told. For the summary.</summary>
    public static long Banked { get; private set; }

    public static long Deposits { get; private set; }

    public static void Reset()
    {
        Banked = 0;
        Deposits = 0;
        TripBanks = 0;
        TripGold = 0;
        TripAfield = 0;
        TripAfieldGold = 0;
        TripNoBank = 0;
    }

    public static string Describe() => $"{Deposits} deposits worth {Banked}gp; {Wealthy()}; {BotYield.DescribeAside()}; {BotYield.DescribeDeaths()}; {BotUndertaker.Describe()}; before a trip: {DescribeTrips()}";

    /// <summary>
    /// What the whole population is worth, at this moment, read off the bots themselves.
    ///
    /// <para>
    /// <b>Every price on this shard was being refused and not one line said how much money there was.</b>
    /// In seventeen minutes of the afternoon of 27.08.2026: 849 of 849 riders could not afford a horse,
    /// 1436 of 2558 could not afford a lesson, 337 could not afford a piece of armour. Three subsystems
    /// idle for the same stated reason, and the only way to tell "these three prices are too high" from
    /// "this population has no money" was to guess — because <see cref="Deposits"/> counts the coin that
    /// moves and nothing counted the coin that sits.
    /// </para>
    ///
    /// <para>
    /// The median rather than the mean, because one bot that has been paid by forty skeletons drags an
    /// average somewhere no bot actually stands. The pocket and the account apart, because a population
    /// rich in the bank and empty in the pocket is a banking problem and not a poverty one, and those two
    /// have already been mistaken for each other once on this project.
    /// </para>
    ///
    /// <para>
    /// <b>And two sorts of bot are left out of it, by order: whoever draws a stipend, and whoever thinks.</b>
    /// Poverty is a question about what work pays, and both of those have a second tap. A Baron is handed
    /// ten thousand gold he did not earn — he was the 10400gp that stood as the fattest purse on this shard
    /// the first time this line was printed, which says nothing whatever about whether mining pays. A minded
    /// bot decides differently from the other thirteen and would quietly move the middle of a measurement
    /// that is supposed to be about the trades. They are counted and named on the end of the line instead,
    /// because a bot dropped silently from a measurement is how a measurement starts lying.
    /// </para>
    /// </summary>
    public static string Wealthy()
    {
        var bots = BotPopulation.Bots;

        if (bots.Count == 0)
        {
            return "nobody alive to have any money";
        }

        // Every bot, once, on a clock measured in minutes — a walk of the population is what this question
        // is, and there is no spatial query for "how much money exists".
        var purses = new int[bots.Count];
        var counted = 0;

        // <b>The fattest purse is named, because a number that large is a question and a name is its
        // answer.</b> 98242gp stood here on the evening of 04.09.2026 — sixty-eight per cent of every coin
        // 50 bots held between them — and the line could say only that somebody had it. This file argues two
        // paragraphs above that a bot dropped silently from a measurement is how a measurement starts lying;
        // an unnamed maximum is the same fault wearing a number.
        Mobile richest = null;
        var kept = 0;
        var minded = 0;
        long pack = 0;
        long bank = 0;

        foreach (var bot in bots)
        {
            if (bot == null || bot.Deleted || !bot.Alive)
            {
                continue;
            }

            // <b>Anybody the crown keeps, whether by purse or by pack.</b> A Baron is handed ten thousand
            // gold he did not earn; a King's Ranger is handed no gold at all and never earns any, because he
            // has no trade and is provisioned instead. Both are outside the question this line answers, and
            // the second is the more dangerous of the two to leave in: five bots holding nought would drag
            // the median of a thirty-bot population down by a step and read as the economy getting worse.
            if (bot.Class is { Stipend: > 0 } or { Provisioned: true })
            {
                kept++;

                continue;
            }

            if (bot.Minded)
            {
                minded++;

                continue;
            }

            var pocket = bot.Backpack?.GetAmount(typeof(Gold)) ?? 0;
            var account = Banker.GetBalance(bot);

            pack += pocket;
            bank += account;
            if (richest == null || pocket + account > (richest.Backpack?.GetAmount(typeof(Gold)) ?? 0) + Banker.GetBalance(richest))
            {
                richest = bot;
            }

            purses[counted++] = pocket + account;
        }

        var apart = kept + minded == 0
            ? ""
            : $"; {kept} kept by the crown and {minded} that think were left out of it";

        if (counted == 0)
        {
            return $"nobody who earns their own living is alive to have any money{apart}";
        }

        Array.Sort(purses, 0, counted);

        var poorest = purses[0];
        var middle = purses[counted / 2];
        var richest2 = purses[counted - 1];

        var name = richest == null
            ? "nobody"
            : $"{richest.Name} the {(richest as BotMobile)?.Class?.Name ?? "bot"}";

        return $"{counted} purses that were earned: poorest {poorest}gp, middling {middle}gp, fattest {richest2}gp held by {name}, "
               + $"{pack + bank}gp between them with {pack}gp of it in pockets and {bank}gp in accounts{apart}";
    }

    /// <summary>
    /// Puts away everything above the float, if the bot is standing at a counter. Returns what went in.
    ///
    /// <para>
    /// <b>The coin leaves the pack before the account is credited</b>, and if the deposit is refused it is
    /// handed straight back. The engine's deposit adds to an account without touching what the depositor is
    /// carrying, so the other order — or any early return between the two — is a way to make gold out of
    /// nothing. It is the same order <see cref="BotDig"/> banks with, for the same reason.
    /// </para>
    /// </summary>
    public static int Bank(Mobile bot)
    {
        var pack = bot?.Backpack;
        var map = bot?.Map;

        if (pack == null || map == null || map == Map.Internal || !bot.Alive)
        {
            return 0;
        }

        // <b>A bot on a stipend never banks its pocket, and this is not a nicety.</b> What it carries is
        // drawn from its own account by BotStipend — no counter, no trip — so a rule that put the excess back
        // the moment it walked past a bank would be two subsystems moving the same coin in opposite
        // directions for the rest of the session: withdraw four hundred, deposit four hundred, and a log line
        // each time. The steward owns that purse.
        if (bot is BotMobile { Class.Stipend: > 0 })
        {
            return 0;
        }

        var floor = Keeps(bot);
        var purse = pack.GetAmount(typeof(Gold));
        var excess = purse - floor;

        if (excess <= 0)
        {
            return 0;
        }

        var counter = BotGround.Counter(map, bot.Location);

        if (counter == Point3D.Zero || !bot.InRange(counter, Reach))
        {
            return 0;
        }

        if (!pack.ConsumeTotal(typeof(Gold), excess))
        {
            return 0;
        }

        if (!Banker.Deposit(bot, excess))
        {
            pack.DropItem(new Gold(excess));

            return 0;
        }

        Banked += excess;
        Deposits++;

        logger.Information("{Name} put {Gold}gp away and kept {Float}", bot.Name, excess, floor);

        return excess;
    }
}
