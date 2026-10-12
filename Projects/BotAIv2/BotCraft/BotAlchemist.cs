using System;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers the mortar to anybody carrying one, and offers the board's orders first.
///
/// <para>
/// <b>Every gate here is a skipped candidate and never a failed errand.</b> The same rule the fletcher's
/// proposer states at length, and for the same reason: a brewer with no herbs is passed over, not sent out
/// to discover that at the mortar. This shard has paid for the other arrangement twice.
/// </para>
///
/// <para>
/// <b>Herbs are checked before glass, because they are the half nobody hands back.</b> Glass is five gold a
/// hundred on the alchemist's shelf and comes back off every potion anybody drinks, so a brewer short of
/// bottles is a brewer with a short errand. A brewer short of reagents is waiting on a gatherer, and
/// telling those two apart is what makes the counters below worth reading.
/// </para>
/// </summary>
public sealed class BotAlchemist : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotAlchemist));

    private static bool _saidNoSystem;

    private static bool _saidNoGlass;

    private static bool _said;

    /// <summary>Every gate apart, with the denominator. There is no bucket called "other".</summary>
    public static long Asked { get; private set; }

    public static long NoKit { get; private set; }

    public static long NoHerbs { get; private set; }

    /// <summary>
    /// Of those, the ones whose Alchemy will not carry a single draught yet.
    ///
    /// <para>
    /// <b>One bucket was holding three answers, and the shard said so for hours before anybody could read
    /// it.</b> "Had the glass but no herbs" is everything <c>BotFlask.Choose</c> refuses for, and it refuses
    /// on three grounds that want opposite responses: no skill, no reagent, or everything already at its
    /// cap. Measured across eight windows the share of new asks landing there settled at 85%, which is not a
    /// supply figure at all — no shortage is that uniform. See <c>BotFlask.Refusal</c>.
    /// </para>
    /// </summary>
    public static long Unskilled { get; private set; }

    /// <summary>Of those, the ones with the skill and not the reagent. The only one an errand can answer.</summary>
    public static long Reagentless { get; private set; }

    /// <summary>Of those, the ones whose every draught is at its cap or resting.</summary>
    public static long Stocked { get; private set; }

    public static long NoGlass { get; private set; }

    /// <summary>
    /// Bots holding neither herb nor glass, which used to be counted as holding herbs.
    ///
    /// <para>
    /// <b>The biggest number in the whole economy summary was measuring something else.</b> The gate above
    /// asks for glass first and answers "no glass" whenever the recipe cannot be filled, so a bot carrying
    /// nothing at all was reported as a brewer standing over its reagents waiting for a bottle. At 20:09 on
    /// 04.09.2026 the line read "846 had the herbs but no glass" — the largest single shortage on the board
    /// — and there was no way to tell from it how many of the eight hundred were a glass problem at all.
    /// A bucket that swallows a second cause is worse than no bucket: it names a cure and points it at the
    /// wrong half of the shard.
    /// </para>
    /// </summary>
    public static long Bare { get; private set; }

    /// <summary>Bots holding the cap of every draught they can make. Not the same as holding nothing.</summary>
    public static long AtCap { get; private set; }

    /// <summary>Of those genuinely short of glass, how many were sent to buy some.</summary>
    public static long Sent { get; private set; }

    /// <summary>How many found no counter within their own reach that sells a bottle.</summary>
    public static long NoShop { get; private set; }

    /// <summary>How many found a counter that named no price for one.</summary>
    public static long NoPrice { get; private set; }

    /// <summary>
    /// Brewers whose purse would not carry even the glass their own herbs could fill, and the fattest purse among them.
    /// See <see cref="Glassware"/>.
    /// </summary>
    public static long Unaffordable { get; private set; }

    public static long Richest { get; private set; }

    /// <summary>Bottles the glass trips offered were for, between them: the average is what one trip now buys.</summary>
    public static long SentBottles { get; private set; }

    public static long ToOrder { get; private set; }

    public static long OnSpec { get; private set; }

    public string Name => "Alchemist";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        if (BotFlask.Kit(body) == null)
        {
            NoKit++;

            return null;
        }

        Asked++;

        if (BotFlask.System == null)
        {
            if (!_saidNoSystem)
            {
                _saidNoSystem = true;

                logger.Error("The alchemy system does not exist yet, so nobody can brew");
            }

            return null;
        }

        var recipe = BotFlask.Choose(bot, body, out var potion);

        if (recipe == null)
        {
            // <b>A draught somebody has asked for, its glass and herbs paid out of the want's own reservation (03.10.2026, econ13;
            // Patrick: "a potion want's reserve pays the alchemist's bottles — do it").</b> Before the pack's own shortages are
            // counted: a brewer short of glass or herbs, poor or not, brews the order on the buyer's advance and settles it at the
            // hand-over. See BotAdvance.
            var funded = Funded(bot, body, map);

            if (funded != null)
            {
                return funded;
            }

            // Choose only ever answers with a recipe both halves are paid for, so a null here is one of two
            // shortages. Named apart: which one it is decides whether the answer is a gatherer or an errand.
            // Which of the two halves is missing is settled inside Glassware, where the answer is actually
            // known: a bot with no bottle may still have no herb either, and counting it here made the two
            // indistinguishable. See BotAlchemist.Bare.
            if (BotFlask.Bottles(body) <= 0)
            {
                return Glassware(bot, body, map);
            }

            // <b>A bot with five of everything is not a bot short of herbs, and calling it one cost most of a
            // day.</b> The split above was written to find out which of three grounds held 85% of every ask,
            // window after window — and the answer was that every single one of them was at its cap. There
            // was never a shortage there at all: it is the settled state of a trade that has made what the
            // population will hold, working exactly as the cap was ordered to. A steady share is the tell —
            // no real shortage sits at 85% for eight windows running.
            //
            // So it is counted where it belongs rather than under a name that sent two people hunting supply.
            switch (BotFlask.Why)
            {
                case BotFlask.Refusal.Unskilled:
                    Unskilled++;

                    break;

                case BotFlask.Refusal.Full:
                    Stocked++;

                    break;

                default:
                    NoHerbs++;
                    Reagentless++;

                    break;
            }

            return null;
        }

        var order = Order(bot, potion);

        Once(body, potion);

        if (order != null)
        {
            ToOrder++;
            order.Offered();

            return new BotBrew(map, body.Location, potion, null, 0, 0, order);
        }

        // A draught nobody ordered needs a place in the pack: they do not stack in this era (06.10.2026, work17). See BotRoom.SpecShut.
        if (BotRoom.SpecShut(body))
        {
            return null;
        }

        OnSpec++;
        BotGlut.Spec(potion);

        return new BotBrew(map, body.Location, potion, null, 0, 0);
    }

    /// <summary>
    /// A trip for empty bottles, when that is the only thing standing between the bot and its trade.
    ///
    /// <para>
    /// Offered only when the pack still holds herbs to brew with, which <see cref="Propose"/> has already
    /// established: buying glass to stand next to no reagents is money spent on nothing.
    /// </para>
    /// </summary>
    private BotDeed Glassware(IBotWilful bot, Mobile body, Map map)
    {
        // Which draught the herbs in the pack are for, settled before the walk rather than after it.
        var potion = BotFlask.Likeliest(bot, body);

        if (potion == null)
        {
            // <b>Full is not empty, and telling them apart is the whole of what this bucket is for.</b> A bot
            // holding five of every draught it can make answers null from Likeliest exactly as a bot holding
            // nothing does. See BotFlask.Cap, and see Bare below for the first time this file was corrected
            // for a bucket that swallowed a second cause.
            if (BotFlask.AtCap(bot, body))
            {
                AtCap++;

                return null;
            }

            // No glass and no herbs either. Not this trade's errand — the shopper and the gatherer both
            // answer for herbs — and counted apart, because a bot with nothing in its pack is not a brewer
            // waiting on a bottle and a cure aimed at the glass will never reach it.
            Bare++;

            return null;
        }

        NoGlass++;

        BotShops.Survey(map, body.Location);

        var shop = BotShops.Nearest(bot, typeof(Bottle));
        var price = shop == null ? 0 : BotShops.Price(shop, typeof(Bottle));

        // (The population's own glass off the stalls counted here until the auction went on 01.10.2026: glass now reaches a brewer
        // over a counter, where the peddle takes the empties nobody brews with.)
        if (shop == null)
        {
            NoShop++;

            if (!_saidNoGlass)
            {
                _saidNoGlass = true;

                // Said of this bot and not of the shard — BotShops.Nearest searches from ONE bot's position.
                // Same correction as its sisters in BotTailor and BotShopper.
                logger.Error(
                    "{Name} at {Where} on {Map} found no counter with an empty bottle in stock within its own reach; BotShops.Sells refuses a shelf whose Amount has run to nought, so this is as often a drained shelf as a missing shopkeeper",
                    body.Name,
                    body.Location,
                    map
                );
            }

            return null;
        }

        if (price <= 0)
        {
            NoPrice++;

            return null;
        }

        // <b>As much glass as the herbs in the pack can fill, and not twenty bottles whatever they hold (30.09.2026).</b> One round
        // brews one draught, at most up to its cap of five between pack and stall, one herb and one bottle a potion for the heal and
        // the cure; and the glass leg bought BotFlask.Batch, twenty, every time — a hundred gold at the alchemist's five. So the
        // appraisal refused the trip for what it cost: "brew costs 100gp and it has 8gp", 65, 74, 90 and 93gp on build 336, and 1136
        // of 2630 asks there were brewers holding their herbs with no glass. Five bottles are twenty-five gold, and a failed attempt
        // gives its bottle back (DefAlchemy.PlayEndingEffect), so five fill five. A brewer whose purse carries less than its herbs
        // need buys what it can pay for, and one that cannot pay for a single draught's glass is counted and left alone.
        var fill = BotFlask.GlassFor(bot, body, potion);
        var take = Math.Clamp(Math.Max(fill, BotFlask.LeastBottles) - BotFlask.Bottles(body), 1, Math.Max(1, BotFlask.Batch));
        var least = Math.Max(1, Math.Min(fill, BotFlask.LeastBottles) - BotFlask.Bottles(body));
        var wealth = BotYield.Spendable(body);

        if (wealth < take * price)
        {
            take = wealth / price;
        }

        if (take < least)
        {
            Unaffordable++;

            if (wealth > Richest)
            {
                Richest = wealth;
            }

            return null;
        }

        Sent++;
        SentBottles += take;

        return new BotBrew(map, body.Location, potion, shop, price, take);
    }

    /// <summary>
    /// A round brewed for the dearest draught want this bot could answer, its missing glass and herbs bought at a counter on the
    /// buyer's money (03.10.2026, econ13). See <see cref="BotAdvance"/>.
    ///
    /// <para>
    /// <b>Gated on the brewer, the want and the counter.</b> The brewer: Alchemy enough (the mortar's own margin) and the engine's
    /// chance of the draught at <see cref="BotAdvance.LeastChance"/> or better, under the draught's cap, no loan standing, no want of
    /// its own for the kind (its carrier would not carry it), and wants of its own the purse still covers (the counter reads what
    /// it has free). The want: open, answerable by a carrier from here (<see cref="BotCourier.Answerable"/>), no advance on it
    /// already, and a buyer whose money carries the draughts at its offer. The counter: one in reach selling everything missing,
    /// and the missing glass and herbs at its prices no more than the draughts' offer — the hand-over pays the advance back.
    /// </para>
    /// </summary>
    private static BotDeed Funded(IBotWilful bot, Mobile body, Map map)
    {
        if (!BotAdvance.Running || BotAdvance.Of(body) != null || BotMarket.Reserved(body) > BotYield.Wealth(body))
        {
            return null;
        }

        // Once a LookMs a brewer (the review of this build): the walk over the board below is not asked on every review, and the
        // passes it counts are counted once a look, not once a review.
        if (!BotAdvance.Due(body))
        {
            return null;
        }

        var wants = BotMarket.Wants;

        BotWant best = null;
        var bestWorth = 0;
        var units = 0;
        var glass = 0;
        var herbs = 0;
        System.Type reagent = null;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (!want.IsOpen || want.Worth <= bestWorth || !BotAdvance.Draught(want.Kind) || ReferenceEquals(want.Buyer, bot)
                || BotAdvance.On(want) > 0 || BotMarket.Wanted(bot, want.Kind) != null)
            {
                continue;
            }

            // A buyer no carrier from here could reach is passed over without a word: this look is asked of every mortar on every
            // review, and writing a pass on the want for each would name the line's failure after brewers that never could have come.
            if (want.Buyer?.Self is not { Deleted: false, Alive: true } asker || asker.Map != map
                || !Utility.InRange(body.Location, asker.Location, BotCourier.Reach) || BotFlask.Resting(body, want.Kind))
            {
                continue;
            }

            var room = BotFlask.Cap - BotFlask.Held(bot, body, want.Kind);

            if (room <= 0)
            {
                continue;
            }

            var able = BotAdvance.Able(body, want.Kind, out var chance);

            if (able == null)
            {
                continue;
            }

            if (!BotCourier.Answerable(bot, want) || BotCourier.Holding(bot, want))
            {
                continue;
            }

            if (chance < BotAdvance.LeastChance)
            {
                BotAdvance.PassedUnlikely();

                continue;
            }

            // A carrier holding the draughts goes first: it is quicker than a counter and a mortar.
            if (Core.TickCount - want.PostedTick < BotAdvance.WaitMs)
            {
                BotAdvance.PassedYoung();

                continue;
            }

            var (herb, per, bottle) = BotFlask.Costs(able);
            var n = Math.Min(Math.Min(want.Payable, Math.Max(1, BotAdvance.MostUnits)), room);
            var lackGlass = Math.Max(0, n * bottle - BotFlask.Bottles(body));
            var lackHerbs = Math.Max(0, n * per - BotFlask.Amount(body, herb));

            // Everything in the pack already: the mortar's own look offers that round, and nothing need be lent.
            if (herb == null || n <= 0 || lackGlass + lackHerbs <= 0)
            {
                continue;
            }

            best = want;
            bestWorth = want.Worth;
            units = n;
            glass = lackGlass;
            herbs = lackHerbs;
            reagent = herb;
        }

        if (best == null)
        {
            return null;
        }

        var kind = best.Kind;
        var buyer = best.Buyer?.Self;

        BotShops.Survey(map, body.Location);

        // One counter for both halves: the alchemist sells the glass and the herbs alike.
        var shop = BotShops.Nearest(bot, glass > 0 ? typeof(Bottle) : reagent);

        if (shop != null && glass > 0 && herbs > 0 && !BotShops.Sells(shop, reagent, out _))
        {
            shop = BotShops.Nearest(bot, reagent);

            if (shop != null && !BotShops.Sells(shop, typeof(Bottle), out _))
            {
                shop = null;
            }
        }

        if (shop == null)
        {
            BotAdvance.PassedNoCounter();

            return null;
        }

        // At the buyer's guild's price in the town, which is what the counter will take. See BotBurgh.Factor.
        var factor = Math.Max(1.0, BotBurgh.Factor(body, shop));
        var cost = (glass > 0 ? glass * (int)Math.Ceiling(BotShops.Price(shop, typeof(Bottle)) * factor) : 0)
                   + (herbs > 0 ? herbs * (int)Math.Ceiling(BotShops.Price(shop, reagent) * factor) : 0);

        if (cost <= 0 || cost > units * best.Offer)
        {
            BotAdvance.PassedUnderpaid();

            return null;
        }

        if (buyer == null || BotMarket.Affords(best, best.Offer) < units
            || (long)BotYield.Spendable(buyer) + BotMarket.ReservedFor(buyer, kind) < cost)
        {
            BotAdvance.PassedUnfunded();

            return null;
        }

        BotAdvance.Looked(units);
        best.Offered();

        return new BotBrew(map, body.Location, kind, shop, best, units, glass, herbs, reagent, cost);
    }

    /// <summary>The most valuable standing order for this potion, or null. Worth rather than nearness.</summary>
    private static BotWant Order(IBotWilful bot, System.Type potion)
    {
        var wants = BotMarket.Wants;

        BotWant best = null;
        var bestWorth = 0;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (!want.IsOpen || want.Kind != potion || want.Worth <= bestWorth)
            {
                continue;
            }

            // Only an order a carrier from here could deliver, and not one already brewed and waiting in the pack for the walk
            // (01.10.2026, build 384 review). See BotCourier.Answerable and BotCourier.Holding.
            if (!BotCourier.Answerable(bot, want) || BotCourier.Holding(bot, want))
            {
                continue;
            }

            best = want;
            bestWorth = want.Worth;
        }

        return best;
    }

    /// <summary>Said once. The first potion this shard ever brewed is worth a line and the thousandth is not.</summary>
    private static void Once(Mobile body, System.Type potion)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} is the first bot on this shard ever to brew: a {Potion}, out of herbs and a bottle it had already, and until now every draught on the island came off a shelf at 15gp",
            body.Name,
            potion?.Name
        );
    }

    public static string Describe() =>
        Asked == 0
            ? $"nobody has been offered the mortar ({NoKit} answers went to bots with no pestle)"
            : $"{Asked} asked to brew: {ToOrder} took an order off the board, {OnSpec} brewed on spec, "
              + $"{NoHerbs} had the glass and not the reagent, {Stocked} had both and every draught already at its cap, {Unskilled} could not carry a single recipe yet, {Bare} had neither, {AtCap} were at the cap of {BotFlask.Cap} on everything they can make ({BotFlask.Capped} draughts passed over for it, {BotFlask.Rests} stood off for {BotFlask.RestMs / 60000} minutes), "
              + $"{NoGlass} had the herbs but no glass ({Sent} sent to buy some, {SentBottles} bottles between them, {Unaffordable} could not pay for the glass their herbs could fill (the fattest purse among them held {Richest}gp), {NoShop} found no counter with one in stock "
              + $"within reach, {NoPrice} found a counter that named no price); {BotAdvance.Describe()}";

    public static void Forget()
    {
        _saidNoSystem = false;
        _saidNoGlass = false;
        _said = false;
        Asked = 0;
        NoKit = 0;
        NoHerbs = 0;
        Unskilled = 0;
        Reagentless = 0;
        Stocked = 0;
        NoGlass = 0;
        Bare = 0;
        AtCap = 0;
        Sent = 0;
        NoShop = 0;
        NoPrice = 0;
        Unaffordable = 0;
        Richest = 0;
        SentBottles = 0;
        ToOrder = 0;
        OnSpec = 0;
    }
}
