using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// A walk into the woods that comes back with herbs.
///
/// <para>
/// <b>The only thing on this shard that makes a reagent.</b> In this era herbs are shop goods and no skill
/// picks them, so every reagent in the world arrived across a counter — and a shard whose shopkeepers do not
/// stock sulphurous ash is a shard where casting ends, quietly, with one line at boot to say so. That is not
/// a hypothetical: it is in the logs. A sage who can walk out and gather is the population's own answer, and
/// the rationing is the whole of what keeps it an answer rather than a tap — see
/// <see cref="BotClass.HerbIntervalMs"/>.
/// </para>
///
/// <para>
/// <b>What it brings back is not chosen.</b> A gatherer that returned exactly what was short would be a
/// vending machine with a walk attached, and the shortage would stop being a fact the market has to solve.
/// It comes back with what the woods had: a random few kinds, a random amount of each. What is surplus goes
/// on the board like anything else, and what is still missing is still missing.
/// </para>
/// </summary>
public sealed class BotHerbs : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHerbs));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "herbs";

    /// <summary>What a trip to the woods is reckoned at per minute before experience corrects it.</summary>
    public static double Prior { get; set; } = 40.0;

    public static double WorkMinutes { get; set; } = 3.0;

    /// <summary>How near the ground the sage has to get before it counts as being in the woods.</summary>
    public static int ArriveWithin { get; set; } = 4;

    /// <summary>Fewest and most kinds of herb one trip may turn up.</summary>
    public static int LeastKinds { get; set; } = 2;

    public static int MostKinds { get; set; } = 5;

    /// <summary>What a herb is reckoned at before the shard has ever traded one. BotForage's figure.</summary>
    public static int Guess { get; set; } = 5;

    /// <summary>Fewest and most of any one kind.</summary>
    public static int LeastEach { get; set; } = 5;

    public static int MostEach { get; set; } = 20;

    /// <summary>
    /// How many of one reagent a picker keeps for itself before the rest goes to the population.
    ///
    /// Five, which is a caster's own working handful. A picker that sold everything and then bought the same
    /// reagent back off a counter would be paying twice to carry what it was already holding — the rule the
    /// archer's arrows and the cook's meat are both kept by.
    ///
    /// <para>
    /// And kept only by a bot that has some use for them: a spellbook to cast from or a mortar to brew with.
    /// Most pickers are gatherers and are neither, and a handful held back by somebody who will never spend it
    /// is a handful the population cannot reach.
    /// </para>
    /// </summary>
    public static int Keeps { get; set; } = 5;

    /// <summary>
    /// The four a heal, a greater heal and a cure spend: garlic, ginseng, spider's silk and mandrake root. What a
    /// healer walks out for, by Patrick's order of 15.09.2026 — so that it heals by spell in a fight and never finds
    /// the book full and the pack empty.
    /// </summary>
    public static readonly Type[] Healing = [typeof(Garlic), typeof(Ginseng), typeof(SpidersSilk), typeof(MandrakeRoot)];

    /// <summary>
    /// How many of each healing herb a healer keeps before the rest goes to the population. Twenty-five: a greater
    /// heal spends one of each, so this is twenty-five casts of the best it has, and a trip brings back up to forty.
    /// </summary>
    public static int HealerKeeps { get; set; } = 25;

    /// <summary>Trips into the woods taken by healers for their own herbs. For the summary.</summary>
    public static long ForHealing { get; private set; }

    /// <summary>Whether this bot is a healer, whose trip is for its own book. See <see cref="Healing"/>.</summary>
    private static bool Medic(Mobile bot) => bot is BotMobile { Class.Role: BotRole.Medic };

    /// <summary>
    /// How much of this kind this bot keeps: a healer keeps <see cref="HealerKeeps"/> of each healing herb, anybody
    /// with a book or a mortar keeps the handful, and everybody else keeps nothing.
    /// </summary>
    private static int KeptBy(Mobile bot, Type kind)
    {
        if (Medic(bot) && Array.IndexOf(Healing, kind) >= 0)
        {
            return HealerKeeps;
        }

        return BotGrimoire.Book(bot) != null || BotFlask.Kit(bot) != null ? Keeps : 0;
    }

    /// <summary>Of these kinds, the one this pack holds least of; ties to chance.</summary>
    private static Type Fewest(Container pack, Type[] kinds)
    {
        Type worst = null;
        var least = int.MaxValue;
        var seen = 0;

        for (var i = 0; i < kinds.Length; i++)
        {
            var held = pack?.GetAmount(kinds[i]) ?? 0;

            if (held > least)
            {
                continue;
            }

            if (held < least)
            {
                least = held;
                seen = 1;
                worst = kinds[i];

                continue;
            }

            seen++;

            if (Utility.Random(seen) == 0)
            {
                worst = kinds[i];
            }
        }

        return worst ?? kinds[Utility.Random(kinds.Length)];
    }

    /// <summary>Reagents put into somebody's standing order. For the summary.</summary>
    public static long Ordered { get; private set; }

    /// <summary>The same, onto a stall. For the summary.</summary>
    public static long Listed { get; private set; }

    /// <summary>The eight, in the order the world lists them. What the woods may have is any of these.</summary>
    private static readonly Type[] Kinds =
    [
        typeof(SulfurousAsh), typeof(BlackPearl), typeof(Garlic), typeof(Ginseng),
        typeof(SpidersSilk), typeof(Nightshade), typeof(Bloodmoss), typeof(MandrakeRoot)
    ];

    private readonly Map _map;

    private readonly Point3D _where;

    private int _found;

    /// <summary>What the haul is worth at the market's own price. See <see cref="Made"/>.</summary>
    private int _worth;

    public BotHerbs(Map map, Point3D where)
    {
        _map = map;
        _where = where;
    }

    public override string Kind => Trade;

    /// <summary>
    /// A walk into the woods for herbs is seen through. See <see cref="BotDeed.Steadfast"/>. The picking clock
    /// belongs to the class rather than the trip, so nothing is put right on resuming.
    /// </summary>
    public override bool Steadfast => true;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing. Walking into a wood teaches nobody anything, whatever comes back in the bag.</summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>
    /// Counted as coin without producing any, for the reason <c>BotBolt</c> spells out: work that pays in
    /// anything but money is discounted by how badly a bot needs money, and a discount that reaches nought is
    /// a veto. A sage with an empty purse is exactly the sage that should be out picking herbs.
    /// </summary>
    public override double Coin => 1.0;

    /// <summary>
    /// What came back out of the woods, priced the way the shard prices it.
    ///
    /// <para>
    /// <b>This declared nought, and declaring nought taught the whole shard that gathering is worthless.</b>
    /// It is the identical defect <see cref="BotOrder.Made"/> records paying for, on the trade the shard can
    /// least afford it: over the four hours to 09:26 on 04.09.2026 the population spent 25,770gp of its
    /// 48,685gp at counters on reagents — fifty-three pence in every pound — and in the same four hours
    /// sixty-five trips into the woods were taken, each one settling in the ledger as "0 in 1.0 min (0/min)".
    /// A trade that reports nothing per minute is a trade the auction stops offering, so the only reagent
    /// route the population had was the one that takes money out of the world.
    /// </para>
    ///
    /// <para>
    /// Goods and not coin, exactly as <see cref="BotForage"/>, <c>BotDig</c> and <c>BotChop</c> reckon what
    /// they bring back — every other gathering trade on the shard already prices its haul this way and this
    /// one was simply never given the line. The <see cref="Coin"/> factor above is left as it was: what a
    /// trip is worth and whether a bot short of money should take it are two questions, and only the first
    /// was being answered wrongly.
    /// </para>
    /// </summary>
    public override int Made => _worth;

    public override string Stage =>
        _found > 0 ? $"back from the woods with {_found} herbs" : $"out to the woods near {_where}";

    /// <summary>
    /// The way to that patch of woods does not exist: remember it, so this bot stops choosing it.
    ///
    /// <para>
    /// <b>Written at 02:20 on 08.09.2026, on a shard that had been running two hours.</b> Herb gathering had
    /// taken on 2959 errands and failed 2910 of them — 98% — every one on "no way through", each one costing
    /// a full path search, each one lasting a fifth of a minute before the bot chose the same kind of place
    /// again. It was the single largest source of failure on the shard and the reason the finished share of
    /// work had fallen from 69% to 17% over one night.
    /// </para>
    ///
    /// <para>
    /// <b>The identical defect was found and fixed in <see cref="BotForage"/> on 02.09.2026</b> — "the only
    /// trade of them all that did not call Ledger.Beware", 281 refusals against one square — and this file
    /// was not looked at then. Same shape, same cure, copied deliberately: the ledger bands places 64 tiles
    /// wide, so one refusal teaches the bot about the whole patch rather than about one tile of it.
    /// </para>
    ///
    /// <para>
    /// Returns false for <see cref="BotForage"/>'s reason: the proposer chose this patch and choosing again
    /// from here would choose it again. The errand ends, and the next decision is made by a bot that knows
    /// something it did not know before.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        bot?.Resolve?.Ledger?.Beware(Trade, _map, _where);

        return false;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body is not BotMobile sage || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (!body.InRange(_where, ArriveWithin))
        {
            return BotDoing.Walk(_map, _where, BotArrival.Within(ArriveWithin), "out to the woods for herbs");
        }

        var pack = body.Backpack;

        if (pack == null)
        {
            return BotDoing.Failed("nothing to carry them in");
        }

        // Stamped before anything is picked, so a trip that ends badly still costs the half hour. Otherwise
        // the cheapest way to gather would be to fail on purpose.
        sage.Herbed = true;
        sage.HerbTick = Core.TickCount;

        var klass = sage.Class;

        // <b>A class may name its own handful, and one did with nothing reading it.</b> BotGatherer sets
        // ForageIntervalMs, ForageYieldMin and ForageYieldMax, and its own documentation calls the forage
        // "the point of this class existing — this is the tap. A handful of one kind every quarter of an
        // hour, deliberately less than the fifteen a caster orders at a time, so the gatherer becomes a
        // supplier rather than a one-off answer." On 02.09.2026 all three of those numbers were assigned,
        // bound to configuration, and read by nothing anywhere on the shard: the tap had never been
        // plumbed in, which is half of why a caster out of reagents had only a counter to go to.
        //
        // One kind, in the amount the class asks for. A class that names no amount gets the Sage's trip,
        // which is what this file was written for and is left exactly as it was.
        // Once, before anything is priced. Shelf asks which counters are known and does not sweep for them.
        BotShops.Survey(body.Map, body.Location);

        // A healer picks two of the four its heals spend, whichever its own pack holds least of; the sage picks what
        // the market has least of; a gatherer picks one kind in its own handful.
        var medic = Medic(body);
        var handful = klass is { ForageYieldMax: > 0 };
        var kinds = handful ? 1 : medic ? 2 : Utility.RandomMinMax(LeastKinds, MostKinds);
        var picked = 0;

        if (medic)
        {
            ForHealing++;
        }

        for (var i = 0; i < kinds; i++)
        {
            var kind = medic ? Fewest(pack, Healing) : Scarcest();

            var amount = handful
                ? Utility.RandomMinMax(Math.Max(1, klass.ForageYieldMin), klass.ForageYieldMax)
                : Utility.RandomMinMax(LeastEach, MostEach);
            var herb = kind.CreateInstance<Item>();

            if (herb == null)
            {
                continue;
            }

            herb.Amount = amount;

            if (!pack.TryDropItem(body, herb, false))
            {
                herb.Delete();

                break;
            }

            picked += amount;

            // Priced as it is picked and at the market's own price, so a reagent the population is bidding
            // hard for makes the trip that fetched it worth what it really was.
            //
            // <b>Valued at the same number it will be sold at, and it has to be the same call.</b> The
            // fallback here was Guess while Store came to open at the shopkeeper's shelf price — five against
            // three for garlic — so the trip reported takings it could not get and the ledger would have
            // learned to over-price this trade by two thirds. Both ends ask Shelf now, which reaches Guess
            // only where no shopkeeper within reach stocks the thing at all.
            _worth += amount * BotMarket.Worth(kind, Shelf(bot, kind));

            // Ground that paid while a bot stood still on it. See BotQuad.Harvested.
            BotQuad.Harvested(body.Map, body.Location);
        }

        _found = picked;

        if (picked <= 0)
        {
            return BotDoing.Failed("the woods had nothing, or the pack was full");
        }

        // <b>Picking had no ending, and that is the whole reason the alchemist had nothing to work with.</b>
        // "herbs" was the second commonest thing on this shard — 1982 rounds of it — while the brewer read
        // "470 had the glass but no herbs, 633 had neither". Neither trade was broken; there was no edge
        // between them, counted: what a want waits for and what is kept to sell (01.10.2026).
        var (ordered, listed) = Store(bot);

        logger.Information(
            "{Name} came back from the woods with {Count} herbs worth about {Worth}gp, {Ordered} of them for somebody's want, to be carried to it, and {Listed} kept to sell",
            body.Name,
            picked,
            _worth,
            ordered,
            listed
        );

        return BotDoing.Done(
            $"{picked} herbs out of the woods worth about {_worth}gp, {ordered} for a want and {listed} kept to sell"
        );
    }

    /// <summary>
    /// Counts the picked reagents: what a want within a carrier's reach waits for, and the rest, kept to sell. Nothing moves — the
    /// goods ride in the pack to the want or to the bank box (01.10.2026; until then a want was filled and the rest put on a stall).
    ///
    /// Only the kinds this trade picks, so a caster's own spellbook reagents bought over a counter are not
    /// swept out with them, and only what is above <see cref="Keeps"/>.
    /// </summary>
    private static (int Ordered, int Listed) Store(IBotWilful bot)
    {
        var body = bot?.Self;
        var pack = body?.Backpack;

        if (pack == null)
        {
            return (0, 0);
        }

        BotShops.Survey(body.Map, body.Location);

        var ordered = 0;
        var listed = 0;

        // A snapshot: offering a stack moves it out of the pack.
        List<Item> carried = [.. pack.Items];

        for (var i = 0; i < carried.Count; i++)
        {
            var stack = carried[i];

            if (stack is not { Deleted: false, Movable: true } || Array.IndexOf(Kinds, stack.GetType()) < 0)
            {
                continue;
            }

            var held = Math.Max(1, stack.Amount);
            var spare = held - KeptBy(body, stack.GetType());

            if (spare <= 0)
            {
                continue;
            }

            var went = BotMarket.Answering(bot, stack.GetType(), spare);

            ordered += went;
            listed += spare - went;
        }

        Ordered += ordered;
        Listed += listed;

        return (ordered, listed);
    }

    /// <summary>
    /// What one of these opens at: the shopkeeper's own asking price, and only then a guess.
    ///
    /// <para>
    /// <b>Opening above the shelf is opening at a price nobody on this island can rationally pay.</b>
    /// <c>BotShopper</c> takes whichever of stall and counter is cheaper and gives a tie to one of ours, so a
    /// reagent listed at five when a herbalist sells garlic at three is a reagent that will never move: 1986
    /// of them went onto stalls in one window and every caster that wanted one walked to a shopkeeper and
    /// paid the world instead of paying a bot. The same fault the fletcher already documents about arrows,
    /// on the trade that produces the most goods per hour of anything here.
    /// </para>
    ///
    /// <para>
    /// Measured rather than declared, like the loot floor in <c>BotSlay.Rifle</c>: the engine knows what a
    /// shopkeeper charges and there is no table here to go stale. The guess is only ever reached where no
    /// shopkeeper within reach stocks the thing at all — which for the deeper reagents is most of them, and
    /// is exactly where a bot's stall is the only supply there is.
    /// </para>
    /// </summary>
    /// <param name="bot">Whose reach decides which counters count. The survey is the caller's to do once —
    /// see the two call sites, both of which sweep before their loop rather than inside it.</param>
    private static int Shelf(IBotWilful bot, Type kind) => BotShops.Shelf(bot, kind, Guess);

    /// <summary>
    /// Which reagent to pick: the one the island has least of on show (the shops by the bank since 01.10.2026; the stalls before).
    ///
    /// <para>
    /// <b>Uniform picking against skewed demand is a supply that cannot be spent.</b> This was
    /// <c>Kinds[Utility.Random(Kinds.Length)]</c> — an eighth of each — while what the shard actually wanted
    /// over one run was sulfurous ash 1320, bloodmoss 393, nightshade 314, ginseng 243 and almost nothing of
    /// the rest. So the ash ran out at once and was bought over a counter, and the other seven piled up: 2661
    /// reagents listed, 1661 of them sold, a thousand standing unsold, and the population meeting 41% of its
    /// own reagent demand while the remainder left the world as coin over a shopkeeper's counter.
    /// </para>
    ///
    /// <para>
    /// <b>Read off the stalls rather than off a tally of shortages, and the difference matters.</b>
    /// <c>BotShopper</c> keeps a count of what bots have been short of, but it is cumulative for the life of
    /// the shard: a reagent that ran dry once an hour ago still reads as the scarcest thing on the island.
    /// Stock on the stalls is a fact about now, and it is self-correcting — the moment the pickers have
    /// covered a kind, it stops being the scarcest and they move to the next.
    /// </para>
    ///
    /// <para>
    /// Ties go to chance, so eight empty kinds at the start of a shard do not send every picker after the
    /// same one.
    /// </para>
    /// </summary>
    private static Type Scarcest()
    {
        Type worst = null;
        var least = int.MaxValue;
        var seen = 0;

        for (var i = 0; i < Kinds.Length; i++)
        {
            // The open shops' stock (29.09.2026; alone since the stalls went on 01.10.2026). See BotShopkeep.Showing.
            var held = BotShopkeep.Showing(Kinds[i]);

            if (held > least)
            {
                continue;
            }

            // Reservoir sampling over the ties, which costs one random draw and no list.
            if (held < least)
            {
                least = held;
                seen = 1;
                worst = Kinds[i];

                continue;
            }

            seen++;

            if (Utility.Random(seen) == 0)
            {
                worst = Kinds[i];
            }
        }

        return worst ?? Kinds[Utility.Random(Kinds.Length)];
    }

    /// <summary>Forgotten with the world.</summary>
    public static void ForgetTrade()
    {
        Ordered = 0;
        Listed = 0;
        ForHealing = 0;
    }
}

/// <summary>
/// Offers the woods to whoever may walk into them, which on this shard is one bot.
///
/// <para>
/// Refuses more often than it offers and every refusal is named, for the reason the patrol's proposer states
/// at length: an unnamed nought is the failure mode this shard has paid for more than any other.
/// </para>
/// </summary>
public sealed class BotHerbalist : IBotProposer
{
    /// <summary>How far out the woods may be looked for.</summary>
    public static int Range { get; set; } = 200;

    /// <summary>How many places to try before giving up on finding a wood at all.</summary>
    public static int Samples { get; set; } = 6;

    public static long Asked { get; private set; }

    /// <summary>Patches passed over because this bot has already been refused the road to them.</summary>
    public static long Refused { get; private set; }

    /// <summary>Woods passed over for asking more strength than the picker brings. See Wood.</summary>
    public static long Perilous { get; private set; }

    /// <summary>Trips sampled round where the picker stood rather than round its home. See <see cref="Wood"/> (build 386).</summary>
    public static long FromHere { get; private set; }

    /// <summary>Asked of a bot whose class has no such trip. Not a refusal — nearly every answer is this.</summary>
    public static long NotAGatherer { get; private set; }

    public static long TooSoon { get; private set; }

    public static long NoWood { get; private set; }

    public static long Offered { get; private set; }

    public string Name => "Herbalist";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        if (body is not BotMobile { Class.HerbIntervalMs: > 0 } sage)
        {
            NotAGatherer++;

            return null;
        }

        Asked++;

        if (sage.Herbed && Core.TickCount - sage.HerbTick < sage.Class.HerbIntervalMs)
        {
            TooSoon++;

            return null;
        }

        var where = Wood(body, map, bot?.Resolve?.Ledger);

        if (where == Point3D.Zero)
        {
            NoWood++;

            return null;
        }

        Offered++;

        return new BotHerbs(map, where);
    }

    /// <summary>
    /// Somewhere out of town a body can stand.
    ///
    /// <para>
    /// Sampled rather than searched, and the test is the one this project already uses for ground: it is not
    /// a town, and feet go there. "Woods" is what the population's own range happens to be made of once the
    /// town is excluded — asking the world for tree tiles would be a spatial sweep per beat for one bot, and
    /// the proposer contract says in as many words that the question may be real but must not be expensive.
    /// </para>
    /// </summary>
    private static Point3D Wood(Mobile body, Map map, BotLedger ledger)
    {
        // Round where this bot lives, not the population's home (29.09.2026): a sage living in Vesper gathers in Vesper's woods.
        var home = BotPopulation.HomeOf(body);
        var roam = Math.Min(Range, BotPopulation.Leash(body));

        // <b>The woods near the picker, not the woods near its bed (01.10.2026, build 386).</b> The patch was drawn within Range of
        // home whatever the bot's distance from it, so a sage at the far end of its leash was sent seven hundred tiles back
        // for a handful of herbs worth twelve gold: eighteen of the twenty-three outbid herb trips on build 386 were dropped at
        // the hold's eight minutes still walking, 149 bot-minutes. Drawn round where it stands while that is within its
        // leash; every sample is still held to the leash from home (BotPopulation.Within below).
        var centre = BotPopulation.Within(map, body.Location, body) ? body.Location : home;

        if (centre != home)
        {
            FromHere++;
        }

        // <b>The nearest of the samples, by the road, rather than the first.</b> The samples are drawn round home and the
        // first that would do was taken wherever the picker stood, so a sage living by The Needle's hall at (1426, 1855)
        // was sent to woods 190 to 600 tiles off and gave up on ten of them on 24–25.09.2026 (Ilsa; 51 herb trips failed
        // in those two days, 1314 finished). Every sample is weighed, and the one nearest by distance plus how much further
        // round from home it lies (BotRoads.Behind, as the prowl, the lairs, the fires and the seams are chosen) goes.
        var best = Point3D.Zero;
        var bestAway = double.MaxValue;

        for (var tries = 0; tries < Samples; tries++)
        {
            var x = centre.X + Utility.RandomMinMax(-roam, roam);
            var y = centre.Y + Utility.RandomMinMax(-roam, roam);

            if (!BotStep.Settle(map, x, y, out var z))
            {
                continue;
            }

            var where = new Point3D(x, y, z);

            if (Region.Find(where, map)?.IsPartOf<TownRegion>() == true)
            {
                continue;
            }

            // See BotPopulation.Reachable.
            if (!BotPopulation.Within(map, where, body) || !BotPopulation.Reachable(map, body.Location, where))
            {
                continue;
            }

            if (BotReach.Ask(map, body.Location, where, BotArrival.Within(BotHerbs.ArriveWithin))
                == BotReachVerdict.Sealed)
            {
                continue;
            }

            // <b>Not into a deadly zone (build 386).</b> Eight pickers died at their patch and thirty-two more while their trip was
            // put down for a flight or a call; the quadrant test below reads what has killed, this reads what the zones know is
            // there now. The same read the hunt makes of its ground (BotHunter.ReadsZones).
            if (BotZones.LevelAt(map, where.X, where.Y) == BotZoneLevel.Deadly)
            {
                Perilous++;

                continue;
            }

            // <b>What this bot has already learned about that ground, which nothing here asked until now.</b>
            // BotHerbs.Bend was given a Beware at 02:16 on 08.09.2026 and the failures came back within the
            // hour: the mark was being written and this sampler, the only thing that chooses where to go,
            // never read it. Third time in one night that a note was filed and not read — the hunt's baulks
            // and the stall watch's silence were the other two.
            //
            // Refusals are per-bot, so this is per-bot too. The ledger bands ground 64 tiles wide, so one
            // refusal covers the patch rather than the tile, and the caution lapses on its own clock.
            // <b>Cautious, not Expect, and the difference cost an hour.</b> Expect answers "what is this
            // ground worth", which for a patch nobody has worked is the claim itself - a positive number,
            // always, so the test never fired once in thirty-five minutes. Caution is a separate question
            // with a separate method, and it is the one BotGround, BotMiner and BotShops all ask.
            // What the whole population has learned about this ground, before what this bot has learned:
            // one bot's caution is a private opinion, a shard-wide refusal is a fact about the island.
            if (BotRefused.Refusing(map, where))
            {
                Refused++;

                continue;
            }

            if (ledger != null && ledger.Cautious(BotHerbs.Trade, map, where))
            {
                Refused++;

                continue;
            }

            // <b>Nor woods the picker cannot live in (29.09.2026, build 335).</b> The seams have asked this since 29.09 morning
            // (BotGround.MineOdds); the woods never did. Lone pickers with a few hundred of strength walked into squares the
            // quadrant record rated five to a hundred times that, and died there two and three times each on 332–333 (Sigra,
            // Norna, Osric, Piran; `night-r6-producers.md`: deaths were 44 % of the producing trades' non-finished endings).
            if (BotGround.MineOdds > 0.0 && BotQuad.Muscle(map, where) > BotThreat.Power(body) * BotGround.MineOdds)
            {
                Perilous++;

                continue;
            }

            var away = body.GetDistanceToSqrt(where) + Math.Max(0, BotRoads.Behind(map, body.Location, where));

            if (away < bestAway)
            {
                best = where;
                bestAway = away;
            }
        }

        return best;
    }

    public static string Describe() =>
        Asked == 0
            ? $"nobody on this shard may go looking for herbs ({NotAGatherer} answers went to bots that may not)"
            : $"{Asked} looks at the woods: {Offered} trips offered ({FromHere} of them drawn round where the picker stood rather than its home), {TooSoon} came round too soon, {NoWood} found nowhere out of town to go, {Refused} patches passed over as already refused, {Perilous} as asking more than the picker brings or lying in a deadly zone; "
              + $"{BotHerbs.Ordered} reagents kept for somebody's want, to be carried to it, and {BotHerbs.Listed} kept to sell, above the {BotHerbs.Keeps} of each kind a picker that can cast or brew keeps back; "
              + $"{BotHerbs.ForHealing} of the trips were healers' own, for the four herbs a heal spends, of which each keeps {BotHerbs.HealerKeeps}";

    public static void Forget()
    {
        Asked = 0;
        NotAGatherer = 0;
        TooSoon = 0;
        NoWood = 0;
        Refused = 0;
        Offered = 0;
        FromHere = 0;
        BotHerbs.ForgetTrade();
    }
}
