using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// One turn at the skillet: raw meat in the pack becomes suppers, and what the cook does not keep goes out
/// to the market.
///
/// <para>
/// <b>A walk to a fire, which this errand was written without and could not work without.</b> It opened with
/// a swing on the strength of <c>DefCooking.CanCraft</c> asking for the tool and nothing else — but the heat
/// is required by each recipe rather than by the system, and a recipe refused for want of heat sends its
/// complaint to a screen the bot does not have. What that looked like on 05.09.2026 was eight swings, three
/// lamb legs still in the pack, and silence. See <c>BotOven.AtAHearth</c> for the test, which is asked of the
/// engine.
/// </para>
///
/// <para>
/// <b>Crafting is asynchronous and the count is taken before the next swing, never after this one.</b>
/// <c>CraftItem.Craft</c> ends by starting a timer; the meal appears a second or so later. A leg that swung
/// and then counted would see no change every single time and give the round up — the exact fault that had
/// alchemy reading "0 finished against 34 failed" while it was in fact brewing. See <c>BotBrew.Brewing</c>,
/// which carries the measurement.
/// </para>
/// </summary>
public sealed class BotBake : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBake));

    /// <summary>How many suppers a cook keeps on itself rather than selling. One, and one is enough: the
    /// eater's rule is one meal per ten minutes, so a second is a stall's worth of stock in a pocket.</summary>
    public static int Keeps { get; set; } = 1;

    /// <summary>The ledger's key.</summary>
    public const string Trade = "cook";

    /// <summary>What a turn at the skillet is reckoned at before the ledger knows better.</summary>
    public static double Prior { get; set; } = 70.0;

    /// <summary>How long one is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 2.0;

    /// <summary>How often the skillet comes round.</summary>
    public static int SwingMs { get; set; } = 3000;

    /// <summary>How long the meat may sit unchanged before the round is given up. Eight swings' worth.</summary>
    public static int StallMs { get; set; } = SwingMs * 8;

    /// <summary>
    /// How long to wait for the pan after the last of the meat goes in.
    ///
    /// <para>
    /// <b>The last swing takes the meat before it gives back the meal.</b> <c>CraftItem.Craft</c> ends by
    /// starting a timer, so there is a second in which the pack holds neither — and a round that finishes the
    /// moment the meat runs out finishes inside exactly that second. It then reports, truthfully as far as it
    /// can see, that nothing came of it. On 05.09.2026 that was 37 rounds ending "the meat is gone" against
    /// 26 that cooked something, and the 26 were only the ones where an earlier swing had already landed.
    /// </para>
    ///
    /// <para>
    /// The same fault the alchemist had, one step along: there it was the count taken after the swing instead
    /// of before the next one, here it is the <em>exit</em> taken between the two. One swing's worth of
    /// patience is enough, because that is what the engine's own timer is measured against.
    /// </para>
    /// </summary>
    public static int SettleMs { get; set; } = SwingMs;

    private enum Leg
    {
        Walk,
        Work
    }

    private readonly Map _map;

    private Point3D _where;

    /// <summary>Fires this cook has gone on to after finding one cold or roadless. See <see cref="MostHops"/>.</summary>
    private int _hops;

    /// <summary>
    /// <b>A cold fire sends the cook to the next one, twice (30.09.2026, build 367).</b> Standing beside a hearth the engine
    /// would not cook over failed the trip, and the next trip found the next fire: 12 of 363's 160 failures, Quelindor three
    /// in 40 seconds at (1487, 1351), (1422, 1472) and (1382, 1520), and two cooks each at (1477, 1369), (1329, 1402) and
    /// (1410, 1470) within half a minute of each other. The cold hearth is still written off for everybody (<see cref="Refuse"/>);
    /// the trip goes on to the nearest other one.
    /// </summary>
    public static int MostHops { get; set; } = 2;

    /// <summary>Cooks sent on to another fire. See <see cref="MostHops"/>.</summary>
    public static long Hopped { get; private set; }

    private Leg _leg;

    private readonly Type _raw;

    private readonly CraftItem _recipe;

    private readonly Type _meal;

    private int _swings;

    private int _cooked;

    private int _had;

    private bool _counting;

    private bool _swung;

    private long _swungTick;

    /// <summary>Meat left at the last look. Minus one, which no amount can equal — see BotSew.</summary>
    private int _lastLeft = -1;

    private long _stirTick;

    private bool _served;

    public BotBake(Map map, Point3D where, Type raw, CraftItem recipe, Type meal)
    {
        _map = map;
        _where = where;
        _raw = raw;
        _recipe = recipe;
        _meal = meal;
    }

    public override string Kind => Trade;

    /// <summary>A turn at the skillet is seen through. See <see cref="BotDeed.Steadfast"/>.</summary>
    public override bool Steadfast => true;

    /// <summary>
    /// Back at the fire after an interruption. The stall clock measures time since the meat last went down, so
    /// left alone it would read the interruption as a skillet that stopped; the count of meals is seeded again
    /// because some may have been eaten or sold in the meantime.
    /// </summary>
    public override void Resumed(IBotWilful bot)
    {
        _swung = false;
        _stirTick = Core.TickCount;
        _lastLeft = -1;
        _counting = false;
    }

    public override Map Map => _map;

    public override Point3D Where => _where;

    /// <summary>Marked down by the glut of the meal it means to cook (01.10.2026): a larder full of ribs nobody asks for is a pile. See <see cref="BotGlut.Claim"/>.</summary>
    public override double Expects => BotGlut.Claim(_meal, Prior);

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => BotOven.Skill;

    /// <summary>The meat was already in the pack. Nothing is bought to do this.</summary>
    public override int Outlay => 0;

    /// <summary>Nothing here is coin. Suppers are goods, and what one is worth is what the market pays.</summary>
    public override double Coin => 0.0;

    /// <summary>The island's price for what somebody asks for, the surplus price for the rest. See <see cref="BotGlut.Yield"/>.</summary>
    public override int Made => BotGlut.Yield(_meal, _cooked, BotOven.Worth);

    public override string Stage =>
        _served
            ? $"cooked {_cooked} of {_meal?.Name}"
            : _leg == Leg.Walk
                ? $"off to a fire to cook {_meal?.Name}"
                : $"at the skillet ({_swings} swings, {_cooked} cooked)";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal || !body.Alive)
        {
            return BotDoing.Failed("no body");
        }

        if (_leg == Leg.Walk)
        {
            return Walking(bot, body);
        }

        var kit = BotOven.Kit(body);

        if (kit == null || BotOven.System == null || _recipe == null)
        {
            return Finish(bot, body, "no skillet");
        }

        if (!_counting)
        {
            // Seeded rather than started from nought: a cook may already be carrying supper, and counting
            // that as made would price the round at what it did not do.
            _counting = true;
            _had = BotCraftwork.Made(body, _meal);
        }

        // What the last swing produced, counted before the next one is made. See the note at the top.
        var have = BotCraftwork.Made(body, _meal);

        if (have > _had)
        {
            _cooked += have - _had;
            BotCraftwork.Produced(_meal, have - _had);
            _had = have;
        }

        var left = BotOven.Amount(body, _raw);

        if (left <= 0)
        {
            // Not yet: the meal from the last swing may still be in the engine's timer. See SettleMs.
            if (_swung && Core.TickCount - _swungTick < SettleMs)
            {
                return BotDoing.Work("waiting for the pan");
            }

            return Finish(bot, body, _cooked > 0 ? "the meat is gone" : "the meat went and nothing came back");
        }

        if (_lastLeft != left)
        {
            _lastLeft = left;
            _stirTick = Core.TickCount;
        }

        // <b>The skillet stopped moving and the engine will not say why.</b> A craft refused by an action
        // lock, a tool worn through mid-round, a stack the engine declines to consume — all three look
        // identical from here, which is silence with meat still in the pack. The named give-up is the only
        // thing standing between that and a bot swinging at nothing until the shard restarts.
        if (Core.TickCount - _stirTick >= StallMs)
        {
            logger.Information(
                "{Name}'s skillet stopped: {Swings} swings, {Cooked} cooked, {Left} of meat left untouched for {Stall}s",
                body.Name,
                _swings,
                _cooked,
                left,
                StallMs / 1000
            );

            return Finish(bot, body, $"the skillet has not moved in {StallMs / 1000}s, with {left} of meat in the pack");
        }

        if (_swung && Core.TickCount - _swungTick < SwingMs)
        {
            return BotDoing.Work("cooking");
        }

        _swings++;
        _swung = true;
        _swungTick = Core.TickCount;

        BotCraftwork.Swing(body, BotOven.System, _recipe, _raw, kit);

        return BotDoing.Work("cooking");
    }

    /// <summary>
    /// To the fire, and the arrival is judged by the engine and not by the distance.
    ///
    /// <para>
    /// Standing on the remembered hearth with the engine still refusing is the end of the road for that
    /// hearth, and it is written down under the place's name so the next choice is a different fire rather
    /// than this one again — the ruling <c>BotForge.Walking</c> arrived at after a smith answered the same
    /// unusable walk order for ever. See <c>BotGround.HearthKind</c>.
    /// </para>
    /// </summary>
    private BotDoing Walking(IBotWilful bot, Mobile body)
    {
        if (BotOven.AtAHearth(body))
        {
            _leg = Leg.Work;

            return BotDoing.Work("at the skillet");
        }

        if (body.InRange(_where, 1))
        {
            Refuse(bot);

            var cold = _where;

            if (Next(bot, body))
            {
                return BotDoing.Walk(_map, _where, BotArrival.Beside, "to another fire");
            }

            return BotDoing.Failed(
                $"no fire the engine will cook over within {BotOven.Reach} of ({cold.X}, {cold.Y})"
            );
        }

        return BotDoing.Walk(_map, _where, BotArrival.Beside, "to a fire");
    }

    /// <summary>
    /// This fire is no use to this bot, and a cook standing beside it has proved it is no use to anybody for now.
    ///
    /// <para>
    /// <b>The private half was written and the shared half was not.</b> The engine's heat test is about the place, not
    /// the cook, yet only the cook's own ledger heard it — so every other cook was sent to the same cold tile to find
    /// out for itself: 31 failures by 24 bots at (1440, 1481) on the evening of 14.09.2026, 19 by 17 at (1530, 1007),
    /// and 12 by 11 at (1442, 1477) on build 41, a campfire long out. Rested for everybody rather than struck off,
    /// because a sweep does not come back to ground it has seen and a fire wrongly struck off would be gone until a
    /// restart — unless the engine finds nothing burning on the tile at all, which is a fire that has gone and is
    /// struck off (build 224). See <see cref="BotGround.Cold"/>.
    /// </para>
    /// </summary>
    private void Refuse(IBotWilful bot)
    {
        bot?.Resolve?.Ledger?.Beware(BotGround.HearthKind, _map, _where);
        BotGround.Cold(_map, _where);
    }

    /// <summary>
    /// The way to the fire turned out not to exist. Nothing to bend to — the nearest is the nearest — but
    /// the refusal is filed under the place so the next choice is a different fire.
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        Refuse(bot);

        return Next(bot, bot?.Self);
    }

    /// <summary>Another fire is the trip's own business, not trouble that lifts its hold. See <see cref="BotDeed.BendIsTrouble"/>.</summary>
    public override bool BendIsTrouble => false;

    /// <summary>The nearest other known fire, if this trip has hops left. See <see cref="MostHops"/>.</summary>
    private bool Next(IBotWilful bot, Mobile body)
    {
        if (body == null || _hops >= MostHops)
        {
            return false;
        }

        var next = BotGround.Hearth(bot, body.Location, _where);

        if (next == Point3D.Zero || next == _where)
        {
            return false;
        }

        _hops++;
        Hopped++;
        _where = next;
        _leg = Leg.Walk;

        return true;
    }

    /// <summary>
    /// Counts the suppers: whatever the bot keeps for itself, what somebody's want is waiting for, and the rest.
    ///
    /// <para>
    /// A cook that has finished in a field has finished: nothing is put out from the fire any more (01.10.2026, the auction
    /// removed). What a want is waiting for rides in the pack to whoever asked (<see cref="BotCourier"/>); the rest waits for the
    /// porter. The eater's side is a condition on the bot's own beat; see <c>BotMeal</c>.
    /// </para>
    /// </summary>
    private BotDoing Finish(IBotWilful bot, Mobile body, string why)
    {
        _served = true;

        if (_cooked <= 0)
        {
            // The attempts were real and the skill checks happened. Finished rather than failed: what it
            // cost is what learning a trade costs.
            return BotDoing.Done($"{_swings} swings, nothing came of it — {why}");
        }

        var listed = 0;
        var ordered = 0;
        var pack = body.Backpack;

        if (pack != null)
        {
            // The same gatherer every craft here sells through, so a partial stack and a full one are
            // handled the one way.
            List<Item> made = BotCraftwork.Gather(body, _meal);

            // One supper kept back, because the whole point of the trade is that somebody eats. Split off
            // the same way the brewer keeps its own draughts — LiftItemDupe or nothing, never a sale of the
            // bot's own supplies through a failed split.
            var keep = Keeps;

            for (var i = 0; i < made.Count; i++)
            {
                var stack = made[i];

                if (stack is not { Deleted: false, Movable: true })
                {
                    continue;
                }

                var held = Math.Max(1, stack.Amount);

                if (keep > 0)
                {
                    if (held <= keep)
                    {
                        keep -= held;

                        continue;
                    }

                    if (Mobile.LiftItemDupe(stack, held - keep) == null)
                    {
                        continue;
                    }

                    keep = 0;
                }

                var held2 = Math.Max(1, stack.Amount);
                var want = BotMarket.Demand(bot, _meal);
                var answer = want == null ? 0 : Math.Min(held2, Math.Max(0, want.Payable - ordered));

                ordered += answer;
                listed += held2 - answer;
            }
        }

        Once(body, _meal);

        return BotDoing.Done($"{_cooked} cooked in {_swings} swings, {ordered} for somebody's want and {listed} kept to sell");
    }

    private static bool _said;

    /// <summary>Said once. The first supper cooked on this shard is worth a line and the thousandth is not.</summary>
    private static void Once(Mobile body, Type meal)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} is the first bot on this shard ever to cook: a {Meal}, out of meat that until now was carried across the island and sold to a butcher for two gold",
            body.Name,
            meal?.Name
        );
    }

    public static void Forget() => _said = false;
}
