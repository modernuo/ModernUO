using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>How a piece of work ended. Four endings, and each is treated differently on purpose.</summary>
public enum BotEnding
{
    /// <summary>Finished, by its own definition of finished. The only ending that credits skill.</summary>
    Done,

    /// <summary>It will not happen — nowhere to walk, nothing there, the order withdrawn. The place is
    /// treated with suspicion afterwards.</summary>
    Failed,

    /// <summary>Something better came along, or it was set aside so long its reason went stale. No blame
    /// attaches to the place: the takings are recorded, and that is all.</summary>
    Dropped,

    /// <summary>The bot died doing it. Failure, plus the cost of the walk back.</summary>
    Died
}

/// <summary>Where a bot stood when it took work on. Everything the takings are measured against.</summary>
public readonly struct BotStake
{
    public BotStake(long tick, double skill, int wealth, int made, long aside)
    {
        Tick = tick;
        Skill = skill;
        Wealth = wealth;
        Made = made;
        Aside = aside;
    }

    public long Tick { get; }

    /// <summary>The one skill this work claims to train, at the moment it started. Zero when it trains none.</summary>
    public double Skill { get; }

    /// <summary>Purse and account together.</summary>
    public int Wealth { get; }

    public int Made { get; }

    /// <summary>The bot's aside ledger at the moment it started. Only the difference from it counts; see <see cref="BotYield.Aside"/>.</summary>
    public long Aside { get; }
}

/// <summary>What a finished piece of work came to.</summary>
public readonly struct BotTakings
{
    public BotTakings(double minutes, double worth, double perMinute, double skill, int coin, int made, int toll = 0)
    {
        Minutes = minutes;
        Worth = worth;
        PerMinute = perMinute;
        Skill = skill;
        Coin = coin;
        Made = made;
        Toll = toll;
    }

    /// <summary>What the death charged on top of the coin: the supplies the next door will ask for. See <see cref="BotYield.Settle"/>.</summary>
    public int Toll { get; }

    public double Minutes { get; }

    /// <summary>Everything it came to, in gold-equivalent.</summary>
    public double Worth { get; }

    /// <summary>The figure the ledger keeps: gold-equivalent per minute of the bot's life.</summary>
    public double PerMinute { get; }

    /// <summary>Points of the trained skill gained.</summary>
    public double Skill { get; }

    /// <summary>Change in purse and account. Negative when the work cost more than it brought in.</summary>
    public int Coin { get; }

    /// <summary>Declared value of goods produced.</summary>
    public int Made { get; }

    public override string ToString() =>
        $"{Worth:F0} in {Minutes:F1} min ({PerMinute:F0}/min): {Coin} coin, {Made} made, {Skill:F1} skill{(Toll > 0 ? $", {Toll} toll" : "")}";
}

/// <summary>
/// What a piece of work was worth. One currency, and the exchange rate between the two things this
/// population is for.
///
/// <para>
/// <b>Points are given for change, never for state.</b> Having money is worth nothing; putting money in the
/// bank is worth what was put in. Having skill is worth nothing; gaining a tenth of a point is worth a
/// tenth of <see cref="GoldPerSkillPoint"/>. That is not a stylistic preference — it is the one condition
/// under which added-on rewards provably cannot change what the best behaviour is (Ng, Harada and Russell,
/// 1999: a shaping term has to be a difference of a potential), and the classic result of ignoring it is an
/// agent that discovers standing still in the right place scores well.
/// </para>
///
/// <para>
/// <b>Death has to be made expensive here, because the rest of this project made it cheap.</b> A bot's kit
/// is bound: it survives death, it is restored on resurrection, and it is not merchandise. That is right
/// for the kit and it leaves dying almost free — so if takings were skill and coin alone, the best way for
/// a young fighter to gain skill would be to attack something far too strong, over and over, dying every
/// time. So dying costs <see cref="DeathMinutes"/> of the divisor and marks the place, which is the honest
/// version of the same fact: what death actually costs a bot is the walk back.
/// </para>
/// </summary>
public static class BotYield
{
    /// <summary>
    /// What one full point of skill is worth in gold.
    ///
    /// <para>
    /// <b>The most consequential number in this folder.</b> It is the exchange rate between the two things
    /// the population is for — getting better and getting paid — and every comparison between a smith's
    /// afternoon and a miner's passes through it. Five hundred means a tenth of a point, which is what a
    /// successful check gains, is worth fifty gold: a little more than an ore trip, a little less than a
    /// good corpse. It wants retuning once the shard's money supply is not running at a loss, and it should
    /// be retuned by watching what the population does, not by argument.
    /// </para>
    /// </summary>
    public static double GoldPerSkillPoint { get; set; } = 500.0;

    /// <summary>What dying costs, expressed as minutes of the bot's life. Roughly the walk back.</summary>
    public static double DeathMinutes { get; set; } = 3.0;

    /// <summary>
    /// How much of the bill a death leaves — the bandages, bottles and reagents the next door will ask for, which lie on the
    /// corpse — is charged to the work that died. All of it (02.10.2026, build 389; Patrick's option 4). The three minutes above
    /// priced a death at the walk back while the pack was bound; the consumables are not, and on build 386 a death cost about
    /// 16gp of pocket and some 40gp of re-provisioning before the next company would take the bot, 2.85 times a bot-hour. Nought
    /// restores the walk-back price alone. Charged to the work in the place it died, so quarry on ground that kills loses the
    /// auction there; hunting as a kind is not charged, and a flight is charged like anything else it was doing.
    /// </summary>
    public static double DeathTollShare { get; set; } = 1.0;

    /// <summary>Deaths that were charged a toll, and the gold of it. For the Money line.</summary>
    public static long DeathTolls { get; private set; }

    public static long DeathTollGold { get; private set; }

    /// <summary>For the Money line.</summary>
    public static string DescribeDeaths() =>
        $"{DeathTolls} deaths charged the work that died {DeathTollGold}gp of supplies left on the corpse ({DeathTollShare:P0} of the bill)";

    /// <summary>
    /// What a point of skill is worth when it is <b>not</b> one this bot's class is for.
    ///
    /// <para>
    /// Three tenths. Without it the two things this project measures quietly become one: a warrior who spends
    /// the night mining gains Mining at the same rate a gatherer does, earns the same takings for it, and the
    /// ledger reports a thriving bot whose own trade has not moved a point. The dashboard's vector column
    /// would say one thing and the brain's arithmetic another.
    /// </para>
    ///
    /// <para>
    /// Not zero, and that matters: a warrior who learns to mine <em>has</em> learned something, and work that
    /// pays is still work. It simply loses to work that also makes the bot what it is trying to become.
    /// </para>
    /// </summary>
    public static double StrayFactor { get; set; } = 0.3;

    /// <summary>
    /// The shortest a piece of work is allowed to have taken, in minutes.
    ///
    /// A floor rather than an accuracy measure: dividing by a few milliseconds turns any trivial success
    /// into an enormous rate, and a proposer that declares its work finished the instant it starts would
    /// otherwise be the most attractive thing on the shard.
    /// </summary>
    public static double LeastMinutes { get; set; } = 0.25;

    /// <summary>
    /// The most, and the least, one settlement may claim per minute. The second guard on the same hole: no
    /// single freak measurement gets to dominate a row of the ledger, and smoothing handles the rest.
    /// </summary>
    public static double MostPerMinute { get; set; } = 2000.0;

    /// <summary>
    /// Everything the bot could pay with: coin in the pack and the balance behind it.
    ///
    /// Both halves, which the first version got wrong in the deciding half only. It weighed every want
    /// against the coin in the pack alone, so a bot with four thousand banked and a hundred in its pocket
    /// judged itself too poor for a chest plate and went back to the graveyard — while the shopkeeper, all
    /// along, would have taken the difference out of its account.
    /// </summary>
    public static int Wealth(Mobile bot)
    {
        if (bot == null)
        {
            return 0;
        }

        var purse = bot.Backpack?.GetAmount(typeof(Gold)) ?? 0;

        return purse + Banker.GetBalance(bot);
    }

    /// <summary>
    /// What the bot may spend on anything but its own wants: <see cref="Wealth"/> less the gold its open wants on the board hold
    /// (<see cref="BotMarket.Reserved"/>). Never below nought.
    ///
    /// <para>
    /// <b>Patrick, 02.10.2026: "money for the purchase must be reserved when the want is made", and "if there is no money,
    /// hunting naturally prevails".</b> Every decision to spend reads this rather than <see cref="Wealth"/>: what a deed may cost
    /// before it is taken (<see cref="BotAppraisal"/>), the purse the urges weigh need against, the poor hunter's rules, dues and a
    /// guild's call on its members, lessons, supplies, a horse, a house, a bed. So a bot whose money is promised to its wants is,
    /// for everything else, as poor as what it has left — and a poor bot is sent to work that pays. <see cref="Wealth"/> stays the
    /// measure of a bot's takings, which a want neither adds to nor takes from.
    /// </para>
    /// </summary>
    public static int Spendable(Mobile bot)
    {
        if (bot == null)
        {
            return 0;
        }

        var wealth = Wealth(bot);
        var held = BotMarket.Reserved(bot);

        return held <= 0 ? wealth : Math.Max(0, wealth - held);
    }

    /// <summary>
    /// Whether the pack could take a coin now: a pile of gold on top to add to, or a free slot for a new one.
    ///
    /// <para>
    /// The engine's own test, asked before anything is drawn. <c>Container.TryDropItem</c> joins a pile first and asks
    /// only about weight when it does; a coin with no pile to join needs a slot, and a backpack has
    /// <c>Container.GlobalMaxItems</c> of them. Goods put into a pack outside <c>TryDropItem</c> - loot, a share of the
    /// spoils, a delivery off the board - can take it past that count, and from then on every coin the bot pays or is
    /// paid over a counter bounces. See <see cref="BotDeed.AtCounter"/>.
    /// </para>
    /// </summary>
    public static bool Pocket(Mobile bot)
    {
        var pack = bot?.Backpack;

        if (pack == null)
        {
            return false;
        }

        var most = pack.MaxItems;

        if (most <= 0 || pack.TotalItems < most)
        {
            return true;
        }

        var items = pack.Items;

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is Gold { Deleted: false })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The named skill, or zero when the work names none. Never a total: see <see cref="BotDeed.Trains"/>.</summary>
    /// <summary>
    /// What a bot is worth for the purpose of judging its work: pack and bank. It added what the bot had down on the auction's board,
    /// so that money moved into escrow did not read as money lost to the work in hand; a want holds no gold since 01.10.2026, and a
    /// purchase paid at a carrier's meeting in the middle of other work is booked aside by the meeting itself (<see cref="Aside"/>,
    /// <c>BotHandover.Exchange</c>).
    /// </summary>
    public static int Standing(Mobile bot) => bot == null ? 0 : Wealth(bot);

    private static readonly Dictionary<Serial, long> _aside = [];

    /// <summary>Coin taken out of bots by decisions outside their work since the world loaded. See <see cref="Aside"/>.</summary>
    public static long AsideTaken { get; private set; }

    /// <summary>Coin handed to bots by decisions outside their work since the world loaded. See <see cref="Aside"/>.</summary>
    public static long AsideGiven { get; private set; }

    /// <summary>
    /// Books coin that a decision outside the bot's work moved out of it (positive) or into it (negative), so that what
    /// the work in hand came to leaves it out.
    ///
    /// <para>
    /// <b>The takings are the difference between the bot now and the bot when it began, and that difference is not the
    /// work's alone.</b> On 15.09.2026 Lysa, the richest bot on the island, was levied for her guild's claim on a square
    /// at 00:49:06 in the middle of a mining trip; the trip settled at −2,902 coin, clamped to −2,000 a minute, and that
    /// is what her ledger learned mining pays. A horse bought on the way did the same the trip before (−517), and the
    /// other direction inflates just as quietly: a courier who carried its guild's till in from the counter was credited
    /// with the till — 380 coin, 522 a minute, for one scribe's pen. A levy, a guild's draw on its members, a counter's
    /// wages and takings and a stable's price are decided outside the errand the bot happens to be in; the stake keeps
    /// this ledger as it stood, and <see cref="Settle"/> puts back whatever went through it since.
    /// </para>
    ///
    /// <para>
    /// Booked by whoever moves the money, and only there: a bot's own purchase for its own errand is that errand's cost,
    /// and a member raising a hall keeps its own share in the errand, which credits it as made. See the callers of this.
    /// </para>
    /// </summary>
    public static void Aside(Mobile bot, long amount)
    {
        if (bot == null || amount == 0)
        {
            return;
        }

        _aside[bot.Serial] = _aside.GetValueOrDefault(bot.Serial) + amount;

        if (amount > 0)
        {
            AsideTaken += amount;
        }
        else
        {
            AsideGiven -= amount;
        }
    }

    /// <summary>Everything booked aside for this bot, net. A running figure: only a difference of two readings means anything.</summary>
    public static long AsideOf(Mobile bot) => bot == null ? 0 : _aside.GetValueOrDefault(bot.Serial);

    /// <summary>The bot is gone for good.</summary>
    public static void Forget(Mobile bot)
    {
        if (bot != null)
        {
            _aside.Remove(bot.Serial);
        }
    }

    /// <summary>For the money line.</summary>
    public static string DescribeAside() =>
        $"{AsideTaken}gp taken out of bots and {AsideGiven}gp handed to them by decisions outside their work, left out of what that work came to";

    /// <summary>A new world: no stake taken in the old one can be settled.</summary>
    public static void Reset()
    {
        _aside.Clear();
        AsideTaken = 0;
        AsideGiven = 0;
        DeathTolls = 0;
        DeathTollGold = 0;
    }

    public static double SkillOf(Mobile bot, SkillName? which) =>
        bot == null || which == null ? 0.0 : bot.Skills[which.Value].Base;

    /// <summary>Where the bot stands now. Taken once, when work is taken on.</summary>
    public static BotStake Take(IBotWilful bot, BotDeed deed)
    {
        var body = bot?.Self;

        return new BotStake(
            Core.TickCount,
            SkillOf(body, deed?.Trains),
            Standing(body),
            deed?.Made ?? 0,
            AsideOf(body)
        );
    }

    /// <summary>
    /// What the work came to, and this is the whole of the measure.
    ///
    /// <para>
    /// Skill counts only when the work finished, which is the guard against the metric's most obvious
    /// exploit. Reward the rate of skill gain by itself and the strongest available behaviour is the
    /// cheapest twitch that gains skill — a training dummy, a spell cast at nothing, two bots sparring in a
    /// field until the shard restarts. Requiring the work to have finished means the gain has to arrive
    /// beside ore, a corpse or a delivery.
    /// </para>
    /// </summary>
    public static BotTakings Settle(IBotWilful bot, BotDeed deed, BotStake stake, BotEnding ending)
    {
        var body = bot?.Self;

        var minutes = (Core.TickCount - stake.Tick) / 60000.0;

        if (ending == BotEnding.Died)
        {
            minutes += DeathMinutes;
        }

        if (minutes < LeastMinutes)
        {
            minutes = LeastMinutes;
        }

        // Money moved through the bot by decisions outside this work is put back out of it. See Aside.
        var coin = body == null ? 0 : (int)(Standing(body) - stake.Wealth + (AsideOf(body) - stake.Aside));
        var made = Math.Max(0, (deed?.Made ?? 0) - stake.Made);

        var skill = 0.0;

        if (ending == BotEnding.Done)
        {
            skill = SkillOf(body, deed?.Trains) - stake.Skill;

            // A loss is neither a reward nor a punishment. Some shards take skill on death, and a negative
            // gain here would be a bot punishing itself for the one thing it is meant to be doing.
            if (skill < 0.0)
            {
                skill = 0.0;
            }

            // Off its own trade, the same tenth of a point is worth less. See StrayFactor.
            if (skill > 0.0 && deed?.Trains != null && bot?.Class?.Wants(deed.Trains.Value) == false)
            {
                skill *= StrayFactor;
            }
        }

        // <b>A death is charged what the next door will demand (02.10.2026, build 389; Patrick's option 4).</b> The pack is on
        // the corpse by the time this is asked, so BotProvision.Bill reads as the supplies the bot must buy again. See DeathTollShare.
        var toll = 0;

        if (ending == BotEnding.Died && body != null && DeathTollShare > 0.0)
        {
            toll = (int)(BotProvision.Bill(body) * DeathTollShare);

            if (toll > 0)
            {
                DeathTolls++;
                DeathTollGold += toll;
            }
        }

        var worth = coin + made + skill * GoldPerSkillPoint - toll;
        var perMinute = Math.Clamp(worth / minutes, -MostPerMinute, MostPerMinute);

        return new BotTakings(minutes, worth, perMinute, skill, coin, made, toll);
    }
}
