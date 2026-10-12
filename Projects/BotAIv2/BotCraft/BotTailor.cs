using System;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers the needle to anybody carrying a sewing kit.
///
/// <para>
/// <b>The tool decides, as it does with the pickaxe.</b> A crafter is born with a kit; anybody who buys one
/// is a tailor while they hold it. No list of permitted classes, so adding a class cannot silently exclude
/// it.
/// </para>
///
/// <para>
/// The precondition belongs here rather than in the work: without a shop selling cloth the chain would end
/// with a bot standing in a shop it cannot buy from, and the ledger would learn that <em>sewing</em> is
/// worthless for a reason that has nothing to do with sewing.
/// </para>
/// </summary>
public sealed class BotTailor : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTailor));

    private static bool _saidNoCloth;

    /// <summary>Sewing stints begun on cloth already in the pack, the weaver's. See BotWeave.</summary>
    public static long OwnCloth { get; private set; }

    /// <summary>
    /// <b>Whether cloth may be bought over a counter to sew on spec (29.09.2026).</b> Off. In two hours after the wipe the
    /// tailors bought 2080 cloth for 4160gp and sewed oilcloth nobody wanted at a loss of 23–35gp a stint, with 286
    /// things on the stalls and 570 wants finding them too dear — the biggest single outflow of the population's money
    /// to the shopkeepers while nobody could afford bandages. Spec work is for the pack's own cloth (the weaver's,
    /// see BotWeave) and for hide, which is free off a corpse.
    /// </summary>
    public static bool BuysCloth { get; set; }

    /// <summary>Spec sewing refused because it would have bought cloth off a counter. See <see cref="BuysCloth"/>.</summary>
    public static long NoBoughtCloth { get; private set; }

    private static bool _saidNoSystem;

    private static bool _said;

    // ---- Every gate, counted apart. This proposer had none at all. ------------------------------
    //
    // <b>The half of the crafting trade nobody could see.</b> The smith names eight refusals in the shard's
    // own summary; the tailor named none, so "the tailor is not taking orders" and "the tailor is taking
    // orders and you have not been watching" were the same log — and the first turned out to be true, for
    // the life of the project, because this proposer never read the board at all.

    public static long Asked { get; private set; }

    /// <summary>Asked of a bot with no sewing kit. Not a refusal — most answers are this.</summary>
    public static long NoKit { get; private set; }

    /// <summary>Orders taken off the needs board.</summary>
    public static long ToOrder { get; private set; }

    /// <summary>Orders this bot could make and had not the leather for.</summary>
    public static long ShortOfLeather { get; private set; }

    /// <summary>Sewing chosen on the bot's own judgement, with no order behind it.</summary>
    public static long OnSpec { get; private set; }

    /// <summary>Neither an order nor leather anywhere: the cloth route, or nothing.</summary>
    public static long NoLeather { get; private set; }

    /// <summary>
    /// Answers where the bot could sew nothing at all out of hide — not one recipe within its skill that the
    /// pack and the cheapest stall together could pay for.
    ///
    /// <para>
    /// <b>Split out because the one counter beside it was answering for both and named only one of them.</b>
    /// "Found no leather anywhere" carried every tailor that got as far as looking, whatever stopped it, and
    /// for four hours it read 2099 while leather sat on stalls — the refusal was the recipe, not the hide.
    /// A counter that names one cause and catches all of them is this shard's most-repeated defect.
    /// </para>
    /// </summary>
    public static long NoRecipe { get; private set; }

    public string Name => "Tailor";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        if (BotThread.Kit(body) == null)
        {
            NoKit++;

            return null;
        }

        Asked++;

        if (BotThread.System == null)
        {
            // Content initialisation builds the craft systems, and anything that asks before that gets null.
            // Said once: a crafter that never works is otherwise indistinguishable from a lazy one.
            if (!_saidNoSystem)
            {
                _saidNoSystem = true;

                logger.Error("The tailoring system does not exist yet, so nobody can sew");
            }

            return null;
        }

        BotShops.Survey(map, body.Location);

        // <b>Leather is offered first, and the order is the point of it.</b> Cloth is the trade that waits on
        // nobody — it is on a shelf, it will be there tomorrow, and a bot sewing it is a bot converting town
        // money into skill. Leather is the other kind entirely: nothing on this shard sells it, so every
        // piece of it in the world came off something a bot killed and skinned. Reaching for it first is what
        // makes a hunter's afternoon worth money to somebody, and it is the only thing here that turns two
        // trades into an economy rather than two hobbies.
        var leather = Leatherwork(bot, body);

        if (leather != null)
        {
            return leather;
        }

        // Leatherwork has already filed which of the two refusals it was — no recipe, or no hide to be had —
        // so nothing is counted a second time here.
        // <b>The cloth in the pack first (29.09.2026).</b> A weaver's own bolt, cut this hour; the counter only when the
        // pack is bare. See BotWeave.
        if (BotThread.Amount(body, typeof(Cloth)) >= BotSew.Bolt && !BotRoom.SpecShut(body))
        {
            var own = BotThread.Choose(body, typeof(Cloth), BotSew.Bolt);

            if (own != null)
            {
                OwnCloth++;
                BotGlut.Spec(own.ItemType);

                return new BotSew(map, body.Location, BotThread.Units(own), own.ItemType);
            }
        }

        if (!BuysCloth)
        {
            NoBoughtCloth++;

            return null;
        }

        var shop = BotShops.Nearest(bot, typeof(Cloth));

        if (shop == null)
        {
            if (!_saidNoCloth)
            {
                _saidNoCloth = true;

                // <b>Said of this bot, not of the shard.</b> BotShops.Nearest searches from ONE bot's
                // position within ITS reach, and a null from it means that bot has nowhere to buy — not that
                // the map has no cloth. Measured 02.09.2026: this line fired four times in a session during
                // which 36 sewing jobs finished and cloth was bought from Melina and from Phyllis. It is an
                // error-level sentence that was simply untrue, and it is the sort a debugger reads and
                // believes. Same correction as its sister in BotShopper.Missing.
                logger.Error(
                    "{Name} at {Where} on {Map} found no shopkeeper selling cloth within its own reach; it cannot sew from bought cloth here",
                    body?.Name ?? "a bot",
                    body?.Location ?? Point3D.Zero,
                    map
                );
            }

            return null;
        }

        // <b>The same question the leatherwork path asks, and this branch never asked it.</b> It checked that
        // somebody sells cloth and that the price is a number — and not whether this bot can make anything
        // out of cloth at all. A tailor whose skill reaches no cloth recipe was therefore offered "go and buy
        // some", walked, bought, and failed with "nothing it knows how to make from Cloth" — whereupon the
        // same offer was made again, because nothing about the bot had changed.
        //
        // On 09.09.2026 that loop ran 1,240 times in nine minutes across four bots and took the whole shard's
        // completion band down to 25%, which is how the alarm found it. It had been latent for as long as the
        // branch existed: it needs a bot with money, and until the guilds began standing for their members
        // these four never had any. Two thresholds on one shelf, with the right one already written twelve
        // lines below.
        var recipe = BotThread.Choose(body, typeof(Cloth), BotSew.Bolt);

        if (recipe == null)
        {
            NoRecipe++;

            return null;
        }

        var price = BotShops.Price(shop, typeof(Cloth));

        // The recipe chosen here decides how much cloth the errand must have before it starts sewing. Handing
        // it over rather than letting the errand assume one is the whole of the fix for the loop described on
        // the constructor: the question asked here and the question asked there are now the same question.
        if (price <= 0)
        {
            return null;
        }

        // The kind it means to sew goes with the offer, so the claim can read its glut (01.10.2026). See BotGlut.
        BotGlut.Spec(recipe.ItemType);

        return new BotSew(shop, price, BotThread.Units(recipe), recipe.ItemType);
    }

    /// <summary>
    /// A leather chain if there is leather to be had, otherwise nothing.
    ///
    /// <para>
    /// Two sources and they are checked in the order that costs least. What the bot is already carrying is
    /// free and would otherwise sit in a pack or go out to a stall for somebody else to sew — a bot that
    /// skinned a bear an hour ago should be wearing the result, not buying the same thing back. Failing that,
    /// the population's own market, which is the whole point: it is where a hunter's leather went, and buying
    /// it is what pays the hunter for having gone out.
    /// </para>
    ///
    /// <para>
    /// No complaint is logged when there is none. Unlike a missing cloth merchant — which is a shard that
    /// cannot support the trade at all and wants saying once — an empty leather market is the ordinary state
    /// of a morning before anybody has killed anything, and it cures itself.
    /// </para>
    /// </summary>
    private static BotDeed Leatherwork(IBotWilful bot, Mobile body)
    {
        // <b>What the trade can lay hands on is worked out before the recipe, not after it.</b> The stall
        // used to be read four lines below the chooser, and the chooser refused any recipe the pack alone
        // could not pay for — so the purchase that is the entire point of the leather trade could never run.
        // Tailors do not hunt; their packs hold no hide; the answer was always null. See BotThread.Choose.
        // (Leather off the cheapest stall was the second source until the auction went on 01.10.2026. A hunter's leather reaches a
        // tailor now by a carrier's walk to the want its trade says aloud — BotStores — or out of its own bank box at a bank.)
        if (BotThread.Amount(body, typeof(Leather)) < BotSew.Bolt)
        {
            BotMarket.FromBank(bot, typeof(Leather), BotSew.Bolt);
        }

        var carried = BotThread.Amount(body, typeof(Leather));

        // <b>The board first, exactly as the smith does it.</b> An order is money already in escrow and a
        // piece somebody is waiting for; sewing on spec is a guess about what might sell. Asked before the
        // bot's own judgement so that the piece made is the piece wanted.
        var order = Order(bot, body);
        var recipe = order == null
            ? BotThread.Choose(body, typeof(Leather), carried)
            : BotThread.Recipe(body, typeof(Leather), order.Kind);

        if (recipe == null)
        {
            NoRecipe++;

            return null;
        }

        var need = BotThread.Units(recipe);

        // <b>A piece nobody ordered needs a place in the pack (06.10.2026, work17).</b> Argus at 12:52 on build 410: Hrolf carried 100
        // leather caps, Varek 70, Jorunna 41, Adela 33, each a place of the pack's 125; at the count those packs refused every
        // carrier, trader and counter (the pack-full family, 115 endings on b410). Spec sewing waits until the pack has
        // BotRoom.SpecPlaces free; the peddler sells the caps meanwhile (BotRoom.Packed). An order is still sewn. See BotRoom.SpecShut.
        if (order == null && BotRoom.SpecShut(body))
        {
            return null;
        }

        // Free, and it would otherwise go out to a stall for somebody else to sew.
        if (carried >= need)
        {
            Took(body, order, recipe.ItemType);

            return new BotSew(body.Map, body.Location, need, order, recipe.ItemType);
        }

        // Not enough to finish a piece: the want the trade says aloud (BotStores) is what brings more.
        NoLeather++;

        return null;
    }

    /// <summary>Which of the two counters this deed belongs to, raised once the deed is certain.</summary>
    private static void Took(Mobile body, BotWant order, Type kind)
    {
        if (order == null)
        {
            OnSpec++;
            BotGlut.Spec(kind);

            return;
        }

        ToOrder++;
        Once(body, order);
    }

    /// <summary>
    /// The most valuable standing order this bot could sew, or null.
    ///
    /// <para>
    /// Worth rather than nearness, like the smith's: everything on this board is within a few minutes' walk
    /// of everything else, and what separates two orders is what they pay. And, like the smith's, the
    /// material is checked here rather than left for the work to discover — <b>skill enough and leather
    /// enough are two different questions</b>, and taking the dearest order on the board is reliably taking
    /// the one that needs the most hide. The smith paid for that lesson with fifty-six failed hauberks in a
    /// night; it is not worth learning twice.
    /// </para>
    /// </summary>
    private static BotWant Order(IBotWilful bot, Mobile body)
    {
        var wants = BotMarket.Wants;

        BotWant best = null;
        var bestWorth = 0;

        // <b>Raised once for the bot and not once for every want it walked past.</b> This counts against
        // Asked, which is a count of bots, so a board holding six leather orders turned one short tailor into
        // six — 567 against a denominator of 264 on 04.09.2026, a figure that cannot be read as a share of
        // anything. Whether this bot was short of hide is one fact about this bot.
        var starved = false;
        var starvedNeed = 0;
        var starvedWorth = 0;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (!want.IsOpen || ReferenceEquals(want.Buyer, bot))
            {
                continue;
            }

            var recipe = BotThread.Recipe(body, typeof(Leather), want.Kind);

            if (want.Worth <= bestWorth || recipe == null)
            {
                continue;
            }

            // Only an order a carrier from here could deliver, and not one already sewn and waiting in the pack for the walk
            // (01.10.2026, build 384 review). See BotCourier.Answerable and BotCourier.Holding.
            if (!BotCourier.Answerable(bot, want) || BotCourier.Holding(bot, want))
            {
                continue;
            }

            var need = BotThread.Units(recipe);
            var carried = BotThread.Amount(body, typeof(Leather));

            if (carried < need)
            {
                // Short of hide for this one: counted apart, so "no order it could sew" and "no leather to sew it with" stay apart.
                starved = true;
                want.Passed(BotPass.Material);

                // The dearest order it could not sew: the leather that one takes is what it fetches from its own box (econ13).
                if (want.Worth > starvedWorth)
                {
                    starvedWorth = want.Worth;
                    starvedNeed = need;
                }

                continue;
            }

            best = want;
            bestWorth = want.Worth;
        }

        if (starved && best == null)
        {
            ShortOfLeather++;

            // Remembered for the walk to its own bank box before it digs (03.10.2026, econ13). See BotCraftFirst.
            BotCraftFirst.Short(body, typeof(Leather), starvedNeed);
        }

        return best;
    }

    private static void Once(Mobile body, BotWant order)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} is the first tailor to take an order off the board: {Buyer} wants {Item} and has {Worth}gp down",
            body.Name,
            order.Buyer?.Self?.Name ?? "somebody",
            order.Label,
            order.Worth
        );
    }

    public static string Describe() =>
        Asked == 0
            ? $"nobody has been offered sewing ({NoKit} answers went to bots with no kit)"
            : $"{Asked} asked: {ToOrder} took an order off the board, {ShortOfLeather} passed one over for want of hide, "
              + $"{OnSpec} sewed on spec, {NoRecipe} had nothing they could sew out of hide, {NoLeather} found no leather anywhere and fell back on cloth, "
              + $"{NoBoughtCloth} refused cloth off a counter for spec work ({OwnCloth} sewed the pack's own)";

    /// <summary>Lets the complaints be made again after a world reload.</summary>
    public static void Forget()
    {
        _saidNoCloth = false;
        _saidNoSystem = false;
        _said = false;
        Asked = 0;
        NoKit = 0;
        ToOrder = 0;
        ShortOfLeather = 0;
        OnSpec = 0;
        NoLeather = 0;
        NoRecipe = 0;
        NoBoughtCloth = 0;
    }
}
