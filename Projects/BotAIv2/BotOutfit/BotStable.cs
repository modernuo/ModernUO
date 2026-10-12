using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Buying a horse, and calling it up.
///
/// <para>
/// <b>Why a miner and not a warrior.</b> A gatherer's whole day is the walk: out to the rock, back to the
/// forge, back out again, with a pack heavy enough that stamina is the thing it actually runs out of. It is
/// the one trade on this shard whose takings are limited by distance rather than by skill or by what it
/// meets — which is exactly the bot a horse is worth five hundred gold to. Everything else here is written
/// so that the next class to be given one needs no code at all: see <see cref="BotClass.Rides"/>.
/// </para>
///
/// <para>
/// <b>Not an errand, and it was one for an hour.</b> Buying a horse was written as a piece of work with a
/// price on it, and a piece of work is measured: every ending, including being outbid halfway, writes what
/// it came to per minute into the bot's ledger. What this comes to per minute is <em>nought</em> — always,
/// even when it succeeds, because it does not earn five hundred gold, it spends it. So the forecast decayed
/// towards <c>prior × 2 / (2 + tries)</c> and the errand poisoned itself: Bryn took it at 115 a minute on
/// 27.08.2026, was outbid by his own copper seam half a minute later, and thereafter it was offered to him
/// at 51, then 36, then 33, against mining at 260. It could never win again.
/// </para>
///
/// <para>
/// A number that is both a forecast and a bid, on a thing the measure cannot see: the horse pays back inside
/// every <em>other</em> errand the bot ever runs, and there is nowhere in takings-per-minute to say that.
/// This project already has the answer written down, in <c>BotPurse</c>, about the identical shape — moving
/// coin to a bank "produces nothing by that measure, so the trip would score zero and never be chosen,
/// however sensible it is. What it is instead is something a bot does <em>while it happens to be at a
/// counter</em>." A horse is bought the same way: on the beat, out of trips the bot was making anyway.
/// </para>
///
/// <para>
/// <b>The five hundred gold is destroyed, and that is worth saying out loud.</b> This shard has one faucet —
/// a monster's purse — and everything else moves coin about rather than making or unmaking it. A stablemaster
/// is an ordinary shopkeeper as far as the world is concerned, so paying one is a sink, and a sink is the
/// half of an economy this population has almost none of. It is the same shape as the crown's stipend read
/// backwards, and it is the direction that needs no argument.
/// </para>
/// </summary>
public static class BotStable
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotStable));

    /// <summary>
    /// How near the stablemaster a bot has to be to be sold a horse.
    ///
    /// <para>
    /// Eight, which is a shopkeeper's own selling range rather than arm's length. Britain's stables are a
    /// walled yard with the keeper inside it, and a bot that had to stand next to her could never buy
    /// anything at all — "no way through to Abira", which is what the errand version of this spent its
    /// short life saying. Trade over a fence is what a stable counter is for.
    /// </para>
    /// </summary>
    public static int Reach { get; set; } = 8;

    /// <summary>Coin a bot keeps back rather than spending on a horse: <see cref="BotPurse.KeepBack"/> unless dialled apart. It was 200.</summary>
    public static int Reserve
    {
        get => _reserve ?? BotPurse.KeepBack;
        set => _reserve = value;
    }

    private static int? _reserve;

    /// <summary>
    /// How often a mounted bot is looked at, and how often one on foot is asked whether to call its horse.
    ///
    /// Two seconds, which is exactly the length of the summon. Faster would be a bot re-casting over its own
    /// cast; slower would be a bot walking half a field before it noticed it could ride.
    /// </summary>
    public static int EveryMs { get; set; } = 2000;

    public static long Bought { get; private set; }

    public static long Paid { get; private set; }

    public static long Summons { get; private set; }

    /// <summary>Times a horse was put away because something started hitting its rider.</summary>
    public static long Thrown { get; private set; }

    /// <summary>
    /// The horse in this bot's pack, or null.
    ///
    /// Asked of the pack rather than remembered on the bot, so that a horse which was sold, lost or handed
    /// over stops existing for every reader at once — there is no second record to go stale.
    /// </summary>
    public static BotSteed Of(Mobile bot) =>
        bot?.Backpack?.FindItemByType<BotChampionSteed>() ?? bot?.Backpack?.FindItemByType<BotSteed>();

    /// <summary>Whether this bot may have a horse at all, and has not got one.</summary>
    public static bool Wants(BotMobile bot) =>
        bot?.Class is { Rides: true } && Of(bot) == null && !bot.Mounted;

    /// <summary>
    /// Sells one, if the bot is standing at a stablemaster with the money.
    ///
    /// <para>
    /// The coin leaves the pack before the horse is handed over, and the horse is destroyed if the pack will
    /// not take it — the same order every payment on this shard uses, and for the same reason: any other
    /// ordering, or any early return between the two, is a way to make something out of nothing.
    /// </para>
    /// </summary>
    public static bool Buy(BotMobile bot, Mobile keeper)
    {
        var pack = bot?.Backpack;

        if (pack == null || keeper is not { Deleted: false } || !bot.InRange(keeper.Location, Reach))
        {
            return false;
        }

        if (BotYield.Spendable(bot) - BotSteed.Price < Reserve)
        {
            return false;
        }

        // <b>Out of everything it owns, and the pocket first.</b> A shopkeeper is paid out of the pack, and
        // that fact took three files to find on 26.08.2026 — but nothing about it is true here, because no
        // shopkeeper is involved in the payment: the horse is made rather than bought off a shelf, and what
        // the stablemaster is for is that a bot has to stand somewhere real to get one. So the money may
        // come from the account, and it must, because the bot this was written for never goes to a bank.
        var carried = pack.GetAmount(typeof(Gold));
        var fromPack = Math.Min(carried, BotSteed.Price);
        var fromBank = BotSteed.Price - fromPack;

        if (fromPack > 0 && !pack.ConsumeTotal(typeof(Gold), fromPack))
        {
            return false;
        }

        if (fromBank > 0 && !Banker.Withdraw(bot, fromBank))
        {
            // Put back what was already taken. Any other ordering here is a way to lose a bot's money.
            if (fromPack > 0)
            {
                pack.DropItem(new Gold(fromPack));
            }

            return false;
        }

        var steed = new BotSteed();

        if (!pack.TryDropItem(bot, steed, false))
        {
            steed.Delete();

            if (fromPack > 0)
            {
                pack.DropItem(new Gold(fromPack));
            }

            if (fromBank > 0)
            {
                Banker.Deposit(bot, fromBank);
            }

            return false;
        }

        // Bound like every other thing the world hands a bot: weightless, and death does not take it. A horse
        // that a bad afternoon can cost five hundred gold is a horse nobody buys twice.
        BotBinding.Bind(steed, bot.Bond);

        Bought++;
        Paid += BotSteed.Price;

        // Bought by a reflex on top of whatever errand the bot is in, and not that errand's cost. See BotYield.Aside.
        BotYield.Aside(bot, BotSteed.Price);

        logger.Information(
            "{Name} bought a horse from {Keeper} for {Price}gp, {Pack} of it out of its pocket and {Bank} out of its account",
            bot.Name,
            keeper.Name,
            BotSteed.Price,
            fromPack,
            fromBank
        );

        return true;
    }

    /// <summary>
    /// Draws the price of a horse out of the bot's own account and into its pocket.
    ///
    /// <para>
    /// Not minted — moved. The account is debited before the coin exists and the coin is handed straight back
    /// if the pack will not take it, which is the same order every payment on this shard uses and for the
    /// same reason: any other ordering is a way to make gold out of nothing.
    /// </para>
    /// </summary>
    public static bool Draw(BotMobile bot)
    {
        var pack = bot?.Backpack;

        if (pack == null)
        {
            return false;
        }

        var have = pack.GetAmount(typeof(Gold));
        var want = BotSteed.Price + Reserve - have;

        if (want <= 0)
        {
            return true;
        }

        if (Banker.GetBalance(bot) < want || !Banker.Withdraw(bot, want))
        {
            return false;
        }

        var coins = new Gold(want);

        if (!pack.TryDropItem(bot, coins, false))
        {
            coins.Delete();
            Banker.Deposit(bot, want);

            return false;
        }

        Drawn += want;

        return true;
    }

    /// <summary>Coin moved out of a bot's own account to pay for a horse. Not minted, and counted apart.</summary>
    public static long Drawn { get; private set; }

    /// <summary>Stablemasters passed over because there was no way through to where they stand.</summary>
    public static long Fenced { get; private set; }

    // ---- Every gate, counted apart. There is no bucket called "other". ---------------------------
    //
    // <b>These went away with the errand and had to come back.</b> Buying a horse used to be a proposer, and
    // a proposer on this shard names every one of its refusals — that is the standard, and it is why "nobody
    // bought a horse" was ever answerable. Turning it into a reflex threw the counters out with it, and for
    // one window the summary could say only that no horse had been bought, which is the unnamed nought this
    // project has paid for more than any other single thing.

    /// <summary>Riders without a horse, looked at.</summary>
    public static long Asked { get; private set; }

    /// <summary>Cannot afford one out of pocket and account together.</summary>
    public static long Poor { get; private set; }

    /// <summary>
    /// The fattest purse among those turned away as too poor.
    ///
    /// <para>
    /// <b>A refusal that does not say how short it fell is not a measurement.</b> "849 could not afford
    /// 500gp and keep 200" reads the same whether the population is carrying 690gp each — a bar set a
    /// tenth too high — or 40gp each, which is a broken economy and nothing to do with this price at all.
    /// One number tells the two apart, and without it the only way to choose between them is to guess.
    /// </para>
    /// </summary>
    public static long Richest { get; private set; }

    /// <summary>No stablemaster has been surveyed anywhere the population has walked.</summary>
    public static long Nowhere { get; private set; }

    /// <summary>Carrying the price, with no stablemaster near enough to hand it to.</summary>
    public static long Away { get; private set; }

    /// <summary>
    /// How far off a stablemaster a bot with the price in hand will walk to reach one.
    ///
    /// <para>
    /// <b>The reflex was sound and had no legs, and the counter said so once it was asked the right
    /// question.</b> Buying a horse became a reflex rather than an errand because as an errand it poisoned
    /// itself in three tries — but a reflex only ever fires where the bot already is, and a bot is never
    /// already at the stables: 282 riders carried the price and were refused for distance on the afternoon
    /// of 27.08.2026, against nought who were near enough, and the closest of them stood twenty-nine tiles
    /// off. Twenty-nine is not a miner at a cave two hundred and forty tiles out. It is a bot standing in
    /// the town it banks in with the stables four streets over, which a selling reach of eight will never
    /// bridge and a short walk closes in seconds.
    /// </para>
    ///
    /// <para>
    /// Sixty, which is "the same town" and not "somewhere on this map". It is deliberately far short of the
    /// distances a gatherer works at: this is not permission to cross the island for a horse — that is the
    /// errand that failed — it is permission to finish a walk the bot had already very nearly made.
    /// </para>
    /// </summary>
    public static int Fetch { get; set; } = 60;

    /// <summary>Bots sent the last few streets to a stablemaster they were carrying the price for.</summary>
    public static long Fetched { get; private set; }

    /// <summary>Walks to a stablemaster taken off the road because the horse was bought, or the bot stood in reach and could not buy.</summary>
    public static long Unfetched { get; private set; }

    /// <summary>
    /// Takes a walk to a stablemaster off the top of the road once it has done its job or can no longer do it.
    ///
    /// <para>
    /// <b>The fetch is pushed on top of the road as an interruption, and nothing else was ever going to take it off.</b>
    /// This reflex stops looking at a bot the moment it has a horse, and an interruption that follows a stablemaster
    /// never lapses on its own - she does not die. So a bot that bought its horse, or arrived and could not afford one,
    /// stood on the fetch: within eight tiles of her and so arrived on every beat, with its own walk underneath never
    /// resumed. Aric stood four minutes that way on 14.09.2026, mounted, with "taking 1 Raw Bird to Iman" underneath.
    /// </para>
    /// </summary>
    private static void Unfetch(BotMobile bot)
    {
        var journey = bot?.Journey;

        if (journey?.Current is not { Interruption: true, Reason: "a horse" } errand)
        {
            return;
        }

        var keeper = errand.Follow;

        // Still wanted and still on its way: the walk is doing its job.
        if (Wants(bot) && keeper is { Deleted: false } && !bot.InRange(keeper.Location, Reach))
        {
            return;
        }

        // In reach and still without a horse is a purchase this beat or a bot too poor to make one; either way the walk
        // has nothing left to do, and the purchase happens from where the bot stands.
        if (Wants(bot) && keeper is { Deleted: false } && BotYield.Spendable(bot) - BotSteed.Price >= Reserve)
        {
            return;
        }

        journey.Complete();
        Unfetched++;
    }

    /// <summary>
    /// The closest any of those ever came to a stablemaster, in tiles.
    ///
    /// <para>
    /// <b>"Too far" is two completely different faults and read as one.</b> 193 riders carried the price and
    /// were turned away for distance on the afternoon of 27.08.2026, with not one turned away for want of a
    /// keeper — and whether that means forty tiles (a bot standing in the town it banks in, with the stables
    /// four streets over, which a reach of eight will never bridge) or two hundred and forty (a miner at the
    /// cave, which no reach could bridge and which needs a walk) decides the whole shape of the fix. The
    /// counter would not say, so this one does.
    /// </para>
    /// </summary>
    public static int Nearest { get; private set; } = int.MaxValue;

    /// <summary>Carrying the price with a stablemaster in reach. What should end in a horse.</summary>
    public static long Ready { get; private set; }

    /// <summary>
    /// The nearest stablemaster the shard has surveyed, or nothing.
    ///
    /// Read off the shopkeepers the population has already found rather than by a sweep of its own: the
    /// survey happens wherever bots go and is the one place on this shard that knows where anybody sells
    /// anything.
    /// </summary>
    /// <param name="except">One already tried and found unreachable, or null.</param>
    public static Mobile Keeper(Mobile bot, Mobile except = null)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        var shops = BotShops.Shops;
        Mobile best = null;
        var closest = int.MaxValue;

        for (var i = 0; i < shops.Count; i++)
        {
            var shop = shops[i];

            if (shop is not AnimalTrainer || shop.Deleted || shop.Map != map || ReferenceEquals(shop, except))
            {
                continue;
            }

            // <b>A stablemaster behind a fence is a stablemaster nobody buys from.</b> Britain's stables are
            // a walled yard and Abira stands inside it; a bot that walked at her from the street on
            // 27.08.2026 got "no way through to Abira" and gave the errand up. The reach ledger already knows
            // which pockets are closed, from searches that have already failed, so this costs a dictionary
            // lookup — the same question the patrol, the harrowing, the lesson and the seam all ask before
            // they offer anywhere.
            if (BotReach.Ask(map, bot.Location, shop.Location, BotArrival.Within(Reach)) == BotReachVerdict.Sealed)
            {
                Fenced++;

                continue;
            }

            var away = System.Math.Max(
                System.Math.Abs(shop.X - bot.X),
                System.Math.Abs(shop.Y - bot.Y)
            );

            if (away >= closest)
            {
                continue;
            }

            closest = away;
            best = shop;
        }

        return best;
    }

    /// <summary>
    /// When each rider was last looked at. Per bot, never one shared stamp — see <c>BotStipend</c> for the
    /// version of this that starves every bot but the first the moment there are two of them.
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<Serial, long> _looked = [];

    /// <summary>
    /// Buys a horse out of a trip the bot was making anyway: draws the price while it is at a counter, and
    /// pays for the horse while it is near a stablemaster.
    ///
    /// <para>
    /// Two halves because the money and the horse are in two places, and neither half is ever a journey of
    /// its own. A bot that banks its takings is at a counter several times an hour by its own arithmetic, and
    /// the stables are the same few streets — so the horse costs the population no walking at all, which is
    /// the whole reason this is a reflex rather than an errand.
    /// </para>
    ///
    /// <para>
    /// Cheap enough for the population's beat: a class flag rules out everybody who does not ride before
    /// anything is looked at, and the two bots left are throttled to one look every <see cref="EveryMs"/>.
    /// </para>
    /// </summary>
    public static void Keep(BotMobile bot)
    {
        Unfetch(bot);

        if (bot?.Class is not { Rides: true } || bot.Deleted || !bot.Alive || !Wants(bot))
        {
            return;
        }

        var pack = bot.Backpack;

        if (pack == null)
        {
            return;
        }

        var now = Core.TickCount;

        // Compared by subtraction against a stamp that was itself a real tick, never against a zero this
        // field never held.
        if (_looked.TryGetValue(bot.Serial, out var last) && now - last < EveryMs)
        {
            return;
        }

        _looked[bot.Serial] = now;

        Asked++;

        // <b>Everything it owns, because the bot this was written for does not go to banks.</b> Two versions
        // of this tried to shepherd coin into the pocket first — one at three tiles from a counter, one at
        // six — and both reported the same thing 565 and 572 times: short, and nowhere near a counter. They
        // were right. A gatherer now mines at a cave two hundred and forty tiles out, took the trip to the
        // counter twice in twelve minutes and was outbid by its own seam both times before arriving. The
        // further a miner works, the less it visits town and the more it needs a horse — the two things I
        // built today pull against each other, and the premise that a bot "is at a counter several times an
        // hour anyway" is false for precisely the class this is for.
        var wealth = BotYield.Spendable(bot);

        if (wealth - BotSteed.Price < Reserve)
        {
            Poor++;

            if (wealth > Richest)
            {
                Richest = wealth;
            }

            return;
        }

        var keeper = Keeper(bot);

        if (keeper == null)
        {
            Nowhere++;

            return;
        }

        if (!bot.InRange(keeper.Location, Reach))
        {
            Away++;

            var gap = System.Math.Max(
                System.Math.Abs(keeper.X - bot.X),
                System.Math.Abs(keeper.Y - bot.Y)
            );

            if (gap < Nearest)
            {
                Nearest = gap;
            }

            // <b>Close the last few streets, and only if it is a few streets.</b> See Fetch: the walk this
            // gives is the tail of a trip the bot has already made, never a journey across the island.
            if (gap > Fetch)
            {
                return;
            }

            // Asked before it is given, because this reflex fires every two seconds and Push does not
            // deduplicate: told twice, a bot would be carrying two orders to the same keeper, and told thirty
            // times it would carry nothing else. The errand already under way is the same order — leave it
            // alone and let it arrive. This project has paid for the other version of this once already: a
            // walking order that is rewritten every beat is a route that restarts every beat.
            if (!ReferenceEquals(bot.Journey.Current?.Follow, keeper))
            {
                bot.Journey.Interrupt(bot.Map, keeper, BotArrival.Within(Reach), "a horse");
                Fetched++;
            }

            return;
        }

        Ready++;

        Buy(bot, keeper);
    }

    /// <summary>
    /// Calls the horse up when the bot has one, is on foot and nothing is hitting it.
    ///
    /// <para>
    /// <b>The summon itself is the engine's, disturbance and all.</b> Double-clicking an ethereal casts a
    /// two-second spell on this era; being hurt disturbs it like any other spell, so a bot set upon while
    /// calling its horse simply does not get it. Nothing here has to know that — which is the whole reason
    /// the horse is one of the engine's mounts rather than something of ours.
    /// </para>
    ///
    /// <para>
    /// Refused while a spell is already going up, because that is what a second cast would interrupt — and a
    /// caster interrupted on a two-second clock is a caster that never lands anything slow. The same sentence
    /// is already in <c>BotMobile.Rearm</c> and in <c>BotWalk</c>, for the same reason.
    /// </para>
    /// </summary>
    public static void Ride(BotMobile bot)
    {
        if (bot is not { Deleted: false, Alive: true } || bot.Mounted || bot.Spell != null)
        {
            return;
        }

        var steed = Of(bot);

        if (steed == null || steed.Rider != null)
        {
            return;
        }

        // Not while the bot is at work no rider may do: it was put on foot for it, and a horse called back mid-swing is a
        // two-second cast and a refused dig on every beat. See BotDeed.Afoot.
        if (bot.Resolve?.Deed is { Afoot: true })
        {
            HeldBack++;

            return;
        }

        // Nothing is calling a horse in the middle of a fight. The rung says it plainly: something is hitting
        // this bot and it has no business doing anything but answering that.
        if (BotLadder.Standing(bot) < BotStanding.Busy)
        {
            return;
        }

        Summons++;

        steed.OnDoubleClick(bot);
    }

    /// <summary>
    /// Puts the horse away because something is hitting its rider.
    ///
    /// <para>
    /// By order, and it is also how the game itself reads: a rider that is set upon is a rider on the ground.
    /// What matters for this shard is the second half of the sentence — <em>and the bot can fight</em> — so
    /// this is called from the damage hook rather than from any decision, and it happens whether or not
    /// anything is thinking.
    /// </para>
    /// </summary>
    public static void Throw(Mobile bot)
    {
        if (bot?.Mount == null)
        {
            return;
        }

        EtherealMount.Dismount(bot);
        Thrown++;
    }

    /// <summary>Puts a rider on foot for work the engine will not let it do from the saddle. See <see cref="BotDeed.Afoot"/>.</summary>
    public static void Alight(Mobile bot)
    {
        if (bot?.Mount == null)
        {
            return;
        }

        EtherealMount.Dismount(bot);
        Alighted++;
    }

    /// <summary>Riders put on foot to work. See <see cref="Alight"/>.</summary>
    public static long Alighted { get; private set; }

    /// <summary>Horses not called up because the rider was at work done on foot. See <see cref="Ride"/>.</summary>
    public static long HeldBack { get; private set; }

    public static string Describe() =>
        Asked == 0
            ? "no class that rides has been looked at"
            : $"{Asked} looks at a rider with no horse: {Ready} had the price and a stablemaster in reach, {Away} had the price and were too far from one (the closest of them stood {(Nearest == int.MaxValue ? 0 : Nearest)} tiles off, against a reach of {Reach}), {Nowhere} had no stablemaster surveyed at all, {Poor} could not afford {BotSteed.Price}gp and keep {Reserve} (the fattest purse among them held {Richest}gp); "
              + $"{Fetched} sent the last streets to one and {Unfetched} of those walks taken off the road once done; {Bought} horses bought for {Paid}gp out of {Drawn}gp drawn, {Fenced} stablemasters found behind a fence; {Summons} called up, {Thrown} riders put on the ground by a blow, {Alighted} put on foot to mine and {HeldBack} calls held back while they worked";

    public static void Forget()
    {
        Bought = 0;
        Paid = 0;
        Summons = 0;
        Thrown = 0;
        Alighted = 0;
        HeldBack = 0;
        Drawn = 0;
        Fenced = 0;
        Asked = 0;
        Poor = 0;
        Richest = 0;
        Nowhere = 0;
        Away = 0;
        Nearest = int.MaxValue;
        Fetched = 0;
        Ready = 0;
        _looked.Clear();
    }
}
