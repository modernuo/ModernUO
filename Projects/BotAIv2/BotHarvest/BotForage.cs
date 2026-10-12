using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Picking up the reagents the world leaves lying about, and putting them on the board.
///
/// <para>
/// <b>Free money the population has been walking past since the day it was raised.</b> The world spawns
/// nightshade, sulphurous ash, garlic and the rest on the ground and refills them on their own clock; every
/// bot on this shard has stepped over them for a fortnight. It is the cheapest income there is — no fight,
/// no tool, no material, no skill — and unlike a vein or a shopkeeper it costs nobody anything to take.
/// </para>
///
/// <para>
/// <b>Asked of the engine as a family, not written out as a list of eight herbs.</b> Everything the world
/// calls a reagent derives from <c>BaseReagent</c>, so one type check covers all of them and covers any that
/// are ever added — which is the same reason the armoury reads the craft systems instead of keeping a table
/// of hauberks. A list of names here would be right today and quietly wrong the first time somebody adds a
/// herb.
/// </para>
///
/// <para>
/// <b>And it ends on the board rather than in a pocket.</b> Two of these are worth pennies to the bot that
/// picked them up and a great deal to the mage two fields away who cannot cast without them — this shard's
/// scribes and casters buy reagents constantly. A gathering errand that ended at the backpack would be a bot
/// hoarding somebody else's spellbook.
/// </para>
/// </summary>
public sealed class BotForage : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotForage));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "forage";

    /// <summary>
    /// What a gathering trip is reckoned at per minute before experience corrects it.
    ///
    /// Modest, and it is meant to be beaten by real work. Reagents are pennies a piece; what makes this worth
    /// anybody's time is that the walk is short and there is nothing to buy, not that the pile is rich. A
    /// number that made this compete with hunting would have the whole population combing fields.
    /// </summary>
    public static double Prior { get; set; } = 20.0;

    /// <summary>How long a gathering trip is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 1.0;

    /// <summary>How far a bot will look, and go, for something lying about.</summary>
    public static int Reach { get; set; } = 18;

    /// <summary>How near it has to be standing to pick a thing up.</summary>
    public static int Touch { get; set; } = 2;

    /// <summary>How full a pack may be before a bot stops stooping for pennies.</summary>
    public static double FillFraction { get; set; } = 0.8;

    /// <summary>What one reagent is asked for on the board, before the market has an opinion.</summary>
    public static int Guess { get; set; } = 5;

    /// <summary>
    /// Reagents passed over because the ground they lie on is already known to be shut off.
    ///
    /// Kept on this class rather than on the proposer because Nearest is here and a counter belongs beside
    /// the line that moves it. Read and reset by BotForager, the way BotScoutmaster reads BotScout.Baulked.
    /// </summary>
    public static long Unreachable { get; private set; }

    /// <summary>Forgets what only the summary was keeping. Called by the proposer's own Forget.</summary>
    public static void ForgetGround() => Unreachable = 0;

    private readonly Map _map;

    private readonly Point3D _where;

    private readonly List<Item> _taken = [];

    /// <summary>Beats of approach without coming any nearer before a pile is given up. Twenty is about four seconds.</summary>
    public static int ApproachBeats { get; set; } = 20;

    /// <summary>How long a pile nobody could get nearer to is passed over by every forager.</summary>
    public static int ShunMs { get; set; } = 600000;

    /// <summary>Piles given up as unreachable and shunned.</summary>
    public static long Shunned { get; private set; }

    /// <summary>
    /// <b>A pile given up twice is given up for longer, and the trip goes on to the next pile (30.09.2026, build 369).</b> The
    /// shun lapsed after ten minutes and forgot how often the pile had been given up, so the same unreachable pile failed
    /// the next forager that came by: (1405, 1693) and (1397, 1676) failed Ingram at 16:12 on 362, Kael at 16:42 and Norbert
    /// at 17:31 on 363, each trip over on the first pile. The rest doubles with each giving-up, up to
    /// <c>ShunMs</c> × 2^<see cref="MostDoublings"/>, and a trip shuns up to <see cref="MostPiles"/> piles before it fails.
    /// </summary>
    public static int MostDoublings { get; set; } = 4;

    /// <summary>Piles one trip gives up and goes on from before the trip itself is given up. See <see cref="MostDoublings"/>.</summary>
    public static int MostPiles { get; set; } = 2;

    /// <summary>Trips that went on to another pile after giving one up.</summary>
    public static long WentOn { get; private set; }

    private static readonly Dictionary<Serial, (long Since, int Times)> _shunned = [];

    private int _piles;

    private int _approach;

    private int _nearestSeen = int.MaxValue;

    /// <summary>Whether that pile was given up on lately, by anybody.</summary>
    private static bool IsShunned(Item item)
    {
        if (item == null || !_shunned.TryGetValue(item.Serial, out var shun))
        {
            return false;
        }

        // Kept after it lapses, so the next giving-up doubles the rest. See MostDoublings.
        return Core.TickCount - shun.Since < (long)ShunMs << Math.Min(Math.Max(0, shun.Times - 1), MostDoublings);
    }

    private int _gathered;

    private int _worth;

    /// <summary>The pile this trip is walking to, held until it is picked up or lost. See the note in Advance.</summary>
    private Item _lying;

    public BotForage(Map map, Point3D where)
    {
        _map = map;
        _where = where;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing. Stooping teaches a bot nothing, and claiming otherwise would be a lie to the ledger.</summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>
    /// Goods rather than coin, exactly as digging is.
    ///
    /// A bot that is short of money should go and earn some instead of filling its pack with herbs it still
    /// has to sell — which is what this factor is for, and it is the same answer the miner gives.
    /// </summary>
    public override double Coin => 0.0;

    public override int Made => _worth;

    public override string Stage =>
        _gathered > 0 ? $"gathered {_gathered} reagents" : "after reagents lying about";

    /// <summary>
    /// The way to the herbs turned out not to exist. Written down — and for want of this one line the whole
    /// population spent its mornings walking at a balcony.
    ///
    /// <para>
    /// <b>Every other trade that walks somewhere already does this and foraging was the one that did not.</b>
    /// <see cref="BotDig"/> marks a seam, <c>BotForge</c> marks a smithy, <c>BotSew</c> marks a shop; the
    /// auction reads that mark for any deed at all — <c>BotAppraisal</c> multiplies an offer by
    /// <c>Suspicion</c> when the ledger is cautious about its <c>Kind</c>, <c>Map</c> and <c>Where</c>. So
    /// the machinery to break this loop was already built, already general, and simply never called from
    /// here.
    /// </para>
    ///
    /// <para>
    /// <b>Measured, 02.09.2026:</b> 281 of the 317 roads refused across a ninety-minute session were one
    /// tile — reagents lying at (1456, 1641, 20), which is a floor above the ground and has no way up. The
    /// errand failed in twelve seconds, the proposer scored the same nearest pile again, and it was offered
    /// again on the next beat: Yarrow failed at it eight times in thirteen seconds. Nine bots at once were
    /// walking somewhere they had not got a tile nearer to in eleven minutes.
    /// </para>
    ///
    /// <para>
    /// <b>The mark goes on <see cref="Where"/> and nowhere else, which is the part that is easy to get
    /// wrong.</b> The tempting key is the herb's own tile — the place that actually refused — but the
    /// appraisal reads <c>deed.Where</c>, so a mark written against the herb would be a note nobody ever
    /// reads, and the loop would run on with a tidy-looking fix in the file. The ledger bands places 64
    /// tiles wide, so the patch is the right grain in any case.
    /// </para>
    ///
    /// <para>
    /// Returns false: there is nowhere else for this errand to bend to — the proposer chose the patch and
    /// picking again from here would pick the same one. The errand ends, and the next decision is made by a
    /// bot that now knows something.
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

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        var pack = body.Backpack;

        if (pack == null)
        {
            return BotDoing.Failed("nowhere to put it");
        }

        // Stops stooping when the pack is nearly full, so that a handful of herbs never costs a bot the room
        // for what it is actually out here to carry.
        if (BotLadder.Load(body) >= BotLadder.Ceiling(body) * FillFraction)
        {
            return Finish(bot, body, "the pack is too full to stoop");
        }

        // <b>Held for the length of the approach, and not held was why this errand could stall in silence.</b>
        // The nearest pile was chosen afresh on every beat, so two piles at much the same distance swapped
        // places as the bot moved a tile and each swap was a different walk order. A journey replaced is a
        // journey whose counters start again — MaxEmptyPlans, MaxPlansWithoutCloser and StallAttempts all of
        // them — so the three backstops the movement layer keeps for exactly this could never reach their
        // limits. On the night of 02-03.09.2026 that showed as three different bots stuck at one place:
        // Lysa the Warrior held forage for 922 seconds at 1424,1632, Nessa for 360 and Edda 2 for 228, none
        // of them arriving, none failing, and not a line in the log about any of it.
        //
        // Re-chosen only when the held one is gone — picked up by somebody else, or now out of reach.
        if (_lying is not { Deleted: false, Movable: true } || !body.InRange(_lying.GetWorldLocation(), Reach))
        {
            _lying = Nearest(body, Reach);
        }

        var lying = _lying;

        if (lying == null)
        {
            return Finish(bot, body, "nothing left lying about");
        }

        if (!body.InRange(lying.GetWorldLocation(), Touch))
        {
            // <b>An approach that stops closing is given up, and the pile is shunned for a while.</b> A bunch
            // of reagents three tiles away with no road to it — on a ledge, behind a fence, inside a wall —
            // was walked at every beat: Jorunn asked the road to (1519, 1453) 292 times in one minute on build
            // 271, every other search at the whole sixty-millisecond ceiling, a seventh of the loop for one
            // bot, and the errand ended only when a prowl outbid it. The walker's own backstops never fired
            // because the pile is chosen again each beat and a re-chosen destination starts their counters
            // over. So the deed keeps its own: the nearest the bot has come, and how many beats since that
            // improved.
            var at = lying.GetWorldLocation();
            var away = Math.Max(Math.Abs(body.X - at.X), Math.Abs(body.Y - at.Y));

            if (away < _nearestSeen)
            {
                _nearestSeen = away;
                _approach = 0;
            }
            else if (++_approach >= ApproachBeats)
            {
                if (_shunned.Count > 2048)
                {
                    _shunned.Clear();
                }

                var times = _shunned.TryGetValue(lying.Serial, out var before) ? before.Times + 1 : 1;

                _shunned[lying.Serial] = (Core.TickCount, times);
                Shunned++;
                BotRefused.Refuse(_map, at);
                _lying = null;
                _approach = 0;
                _nearestSeen = int.MaxValue;

                if (++_piles <= MostPiles)
                {
                    WentOn++;

                    return BotDoing.Work($"giving up the reagents at ({at.X}, {at.Y}), {away} tiles off, for another pile");
                }

                return BotDoing.Failed($"could not get nearer than {away} tiles to the reagents at ({at.X}, {at.Y}) in {ApproachBeats} beats; the pile is shunned for {ShunMs / 60000} minutes");
            }

            return BotDoing.Walk(_map, at, BotArrival.Within(Touch), "after reagents");
        }

        _approach = 0;
        _nearestSeen = int.MaxValue;

        _lying = null;

        var amount = Math.Max(1, lying.Amount);

        // <b>Counted before it is picked up, and the order matters.</b> Dropping a stack into a pack merges
        // it with whatever is already in there, and the object may cease to exist in the same instruction —
        // so anything wanted about it has to be read first. This is the same shape as a craft that reports
        // attempts as output.
        var kind = lying.GetType();

        if (!pack.TryDropItem(body, lying, false))
        {
            return Finish(bot, body, "the pack would not take it");
        }

        _gathered += amount;

        // Ground that paid while a bot stooped on it. See BotQuad.Harvested.
        BotQuad.Harvested(body.Map, body.Location);
        _worth += amount * BotMarket.Worth(kind, Guess);

        // Held so the errand can put its takings out at the end. The stack may have merged into another, in
        // which case the object is deleted and is skipped when the time comes.
        _taken.Add(lying);

        return BotDoing.Work($"gathered {_gathered}");
    }

    /// <summary>
    /// The errand is over: what was gathered goes on the board.
    ///
    /// <para>
    /// Listed rather than carried, and the failure to list is not a failure of the errand — a bot that could
    /// not put its herbs out still has them, which is worth something, and reporting the whole afternoon as
    /// failed over it would teach the ledger to avoid a field that was perfectly good.
    /// </para>
    /// </summary>
    private BotDoing Finish(IBotWilful bot, Mobile body, string why)
    {
        // A trip that gave up a pile it could not reach and then found nothing is the failure it would have been before it
        // went on (build 369), not a finish.
        if (_gathered == 0 && _piles > 0)
        {
            return BotDoing.Failed($"{why}, after giving up {_piles} pile(s) nobody could get to");
        }

        if (_gathered == 0)
        {
            return BotDoing.Done(why);
        }

        // Nothing is put out from the ground any more (01.10.2026): the reagents ride in the pack — to a want by a carrier's walk,
        // or to the bank box by the porter, and from there to a shop by the bank or a shopkeeper.
        _taken.Clear();

        logger.Information(
            "{Name} gathered {Count} reagents worth about {Worth}gp",
            body.Name,
            _gathered,
            _worth
        );

        return BotDoing.Done($"{_gathered} reagents gathered — {why}");
    }

    public override void Drop(IBotWilful bot) => _taken.Clear();

    /// <summary>
    /// The nearest reagent lying loose on the ground, or null.
    ///
    /// <para>
    /// <c>Parent == null</c> is what makes it "on the ground" rather than in a pack or a corpse, and it is
    /// also what keeps a bot out of another bot's goods: nothing in a pack or a bank box is ever standing on a tile to be found.
    /// </para>
    /// </summary>
    public static Item Nearest(Mobile bot, int range)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        Item best = null;
        var bestAway = double.MaxValue;

        foreach (var item in map.GetItemsInRange<BaseReagent>(bot.Location, range))
        {
            if (item.Deleted || item.Parent != null || !item.Movable || IsShunned(item))
            {
                continue;
            }

            // <b>Reagents lie where they lie, and some of them lie on a roof.</b> This picks the nearest by
            // the crow's flight and nothing here asked whether a body could walk to it, so a bunch of garlic
            // one storey up is the nearest thing on offer for as long as it sits there. The reach ledger had
            // already filed that ground and was already refusing the road for nothing — 23 forage errands
            // ended "no way through to (1456, 1641, 20)" in ninety minutes on 03.09.2026, against a pocket of
            // 77 tiles filed at that exact point — but the answer was being read one step too late, by the
            // walker rather than by whatever chose the destination.
            //
            // The same free question BotGround.Nearest asks of every forge and counter it considers.
            if (BotReach.Ask(map, bot.Location, item.Location, BotArrival.Beside) == BotReachVerdict.Sealed)
            {
                Unreachable++;

                continue;
            }

            var away = bot.GetDistanceToSqrt(item.Location);

            if (away < bestAway)
            {
                bestAway = away;
                best = item;
            }
        }

        return best;
    }
}

/// <summary>
/// Offers any bot the reagents lying within sight of it.
///
/// <para>
/// <b>Offered to everybody, because stooping is not a trade.</b> Mining wants a pickaxe, forging wants an
/// anvil, and scribing wants a book — this wants a free hand, so restricting it to one class would be
/// inventing a guild for picking things up. The auction settles whether it is worth a given bot's minute,
/// which is exactly the sort of question it exists to answer.
/// </para>
/// </summary>
public sealed class BotForager : IBotProposer
{
    public string Name => "Forager";

    public BotStanding Rung => BotStanding.Free;

    public static long Asked { get; private set; }

    public static long Laden { get; private set; }

    public static long Bare { get; private set; }

    public static long Sent { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        Asked++;

        if (BotLadder.Load(body) >= BotLadder.Ceiling(body) * BotForage.FillFraction)
        {
            Laden++;

            return null;
        }

        var lying = BotForage.Nearest(body, BotForage.Reach);

        if (lying == null)
        {
            Bare++;

            return null;
        }

        Sent++;

        return new BotForage(map, lying.GetWorldLocation());
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been offered anything lying about"
            : $"{Asked} asked: {Sent} sent after reagents on the ground, {Bare} had none in sight, {Laden} were carrying too much to stoop, {BotForage.Unreachable} were lying somewhere already known to be shut off, {BotForage.Shunned} piles given up as unreachable and shunned ({BotForage.WentOn} trips went on to another pile)";

    public static void Forget()
    {
        Asked = 0;
        BotForage.ForgetGround();
        Laden = 0;
        Bare = 0;
        Sent = 0;
    }
}
