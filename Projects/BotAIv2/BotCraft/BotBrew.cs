using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Brewing: buy the glass if it is short of it, work the mortar, and hand the bottles to whoever put the
/// money down.
///
/// <para>
/// <b>Two legs and no station.</b> A mortar and pestle works wherever the bot is standing, so the only
/// walking here is to a counter for empty bottles. See <see cref="BotFlask"/> for why this trade exists.
/// </para>
///
/// <para>
/// <b>The reagent is never bought here, and that is the point of the chain.</b> A caster short of herbs has
/// a shopping errand of its own, a gatherer walks into a wood for them and a hunter turns them up on the
/// ground. What this leg buys is glass, which is five gold a hundred and never the reason a potion does not
/// get made.
/// </para>
/// </summary>
public sealed class BotBrew : BotDeed, IBotWantCarrier
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBrew));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "brew";

    /// <summary>What a brewing round is reckoned at before the ledger knows better. The fletcher's figure.</summary>
    public static double Prior { get; set; } = 90.0;

    /// <summary>How long one is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 1.5;

    /// <summary>
    /// How often an attempt is made.
    ///
    /// <para>
    /// <b>Not every beat, and this is the difference between a trade and a jam.</b> <c>CraftItem.Craft</c>
    /// opens by taking the <c>CraftSystem</c> action lock and returns silently when it is already held, so a
    /// bot that swings on every beat spends its afternoon being told to wait for itself. The needle's figure,
    /// for the needle's reason — see <c>BotSew.SwingMs</c>.
    /// </para>
    /// </summary>
    public static int SwingMs { get; set; } = 3000;

    /// <summary>How long the mortar may produce nothing and spend nothing before the round is given up.</summary>
    public static int StallMs { get; set; } = SwingMs * 8;

    private enum Leg
    {
        Glass,
        Fund,
        Work,
        Sell
    }

    private readonly Map _map;

    private readonly Point3D _where;

    private BaseVendor _shop;

    /// <summary>Times this round has been sent on to another counter because the shelf emptied. See BotShops.Next.</summary>
    private int _repicks;

    private readonly int _price;

    private readonly int _take;

    private readonly BotWant _order;

    private readonly Type _potion;

    /// <summary>
    /// Brewed for <see cref="_order"/> on its buyer's advance (03.10.2026, econ13): the missing glass and herbs bought at
    /// <see cref="_shop"/> with the buyer's money, for this many draughts. See <see cref="BotAdvance"/>.
    /// </summary>
    private readonly bool _funded;

    private readonly int _fundUnits;

    private readonly int _fundGlass;

    private readonly int _fundHerbs;

    private readonly Type _reagent;

    private readonly int _fundCost;

    /// <summary>Whether the round has let go of the units it held on the want, so the carrier's walk can take them.</summary>
    private bool _released;

    private Leg _leg;

    private int _bottled;

    private int _swings;

    private int _made;

    /// <summary>What was in the pack when this round began, so a healer's own bottles are not counted as made.</summary>
    private int _had;

    /// <summary>Seeded on the first pass through the work, because the pack is not empty when it starts.</summary>
    private bool _counting;

    private bool _swung;

    private long _swungTick;

    /// <summary>Material left at the last look. Minus one, which no amount can equal — see BotSew.</summary>
    private int _lastLeft = -1;

    private long _stirTick;

    public BotBrew(Map map, Point3D where, Type potion, BaseVendor shop, int price, int take, BotWant order = null)
    {
        _map = map;
        _where = where;
        _potion = potion;
        _shop = shop;
        _price = price;
        _take = take;
        _order = order;

        // <b>The leg is chosen by whether glass is wanted, not by whether a shopkeeper was found.</b> Word
        // for word the fletcher's correction about wood, on the material this trade needs most: "no shop"
        // must not mean "skip the buying leg", because the population's own stalls are a source and every
        // bot that drinks a draught puts an empty on one. See Glass, which reads them first.
        _leg = take > 0 ? Leg.Glass : Leg.Work;
    }

    /// <summary>
    /// A round brewed for a want on its buyer's advance (03.10.2026, econ13): to the counter for the <paramref name="glass"/>
    /// bottles and <paramref name="herbs"/> herbs the pack lacks for <paramref name="units"/> draughts, bought with the buyer's
    /// money, then the mortar. The units are held on the want while the round lasts, as a carrier holds what it brings.
    /// </summary>
    public BotBrew(Map map, Point3D where, Type potion, BaseVendor shop, BotWant order, int units, int glass, int herbs, Type reagent, int cost)
    {
        _map = map;
        _where = where;
        _potion = potion;
        _shop = shop;
        _order = order;
        _funded = true;
        _fundUnits = Math.Max(1, units);
        _fundGlass = Math.Max(0, glass);
        _fundHerbs = Math.Max(0, herbs);
        _reagent = reagent;
        _fundCost = Math.Max(0, cost);
        _leg = Leg.Fund;
    }

    /// <summary>The want this round brews for, or null on spec. Read by <see cref="BotCraftFirst"/>.</summary>
    public BotWant Order => _order;

    /// <summary>The market's sweep: a funded round holds its units on the want until it lets them go. See <see cref="IBotWantCarrier"/>.</summary>
    public bool Carries(BotWant want) => _funded && !_released && ReferenceEquals(_order, want);

    public bool Stale => false;

    public Type Goods => _potion;

    public override void Taken(IBotWilful bot)
    {
        if (_funded && _order != null && BotMarket.Stands(_order))
        {
            _order.Reserve(bot, _fundUnits);
        }
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    /// <summary>
    /// An order is worth more than speculation: the money for it is already down. See BotSmith. Speculation on a draught the island
    /// is full of is worth less still (01.10.2026). See <see cref="BotGlut.Claim"/>.
    /// </summary>
    public override double Expects => _order != null ? Prior * 1.6 : BotGlut.Claim(_potion, Prior);

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => SkillName.Alchemy;

    /// <summary>Nothing of its own on a funded round: the buyer's advance pays the counter (econ13).</summary>
    public override int Outlay => _funded ? 0 : _take * _price;

    /// <summary>Its glass is bought over a shopkeeper's counter when it has one to go to. See <see cref="BotDeed.AtCounter"/>.</summary>
    public override bool AtCounter => _shop != null;

    /// <summary>Nothing here is coin. What it produces is bottles, and those are counted as goods.</summary>
    public override double Coin => 0.0;

    public override int Made => _made;

    public override string Stage =>
        _leg switch
        {
            Leg.Glass => $"after {_take} Bottle to brew with",
            Leg.Fund  => $"after {_fundGlass} Bottle and {_fundHerbs} {_reagent?.Name} for {_order?.Buyer?.Self?.Name ?? "a buyer"}'s {_fundUnits} {_potion?.Name}, on its money ({_fundCost}gp)",
            Leg.Work  => $"brewing ({_swings} attempts, {_bottled} made)",
            _         => $"putting {_bottled} bottles out"
        };

    /// <summary>The counter could not be reached. Written under the counter's name, as the fletcher does.</summary>
    public override bool Bend(IBotWilful bot)
    {
        if (_shop is { Deleted: false })
        {
            bot?.Resolve?.Ledger?.Beware(BotGround.CounterKind, _map, _shop.Location);
        }

        return false;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        for (var guard = 0; guard < 6; guard++)
        {
            var doing = _leg switch
            {
                Leg.Glass => Glass(bot, body),
                Leg.Fund  => Fund(bot, body),
                Leg.Work  => Brewing(body),
                _         => Selling(bot, body)
            };

            if (doing.Kind != BotDoingKind.None)
            {
                return doing;
            }
        }

        return BotDoing.Failed("could not settle on a next step");
    }

    /// <summary>Empty glass off a counter, and its own back off the market before it pays anybody.</summary>
    private BotDoing Glass(IBotWilful bot, Mobile body)
    {
        if (BotFlask.Bottles(body) >= BotFlask.LeastBottles)
        {
            _leg = Leg.Work;

            return default;
        }

        // Its own glass out of its own bank box first, standing at a bank: the one material this trade gets back for nothing every
        // time somebody drinks (01.10.2026: the box is reached only there, where its stall was reached from anywhere).
        if (BotMarket.FromBank(bot, typeof(Bottle), _take) > 0 && BotFlask.Bottles(body) >= BotFlask.LeastBottles)
        {
            _leg = Leg.Work;

            return default;
        }

        // (Another bot's glass off a stall, bought from wherever the brewer stood, came before the counter until the auction went on
        // 01.10.2026. Glass nobody brews with now reaches a counter by a peddle, and a brewer buys it there.)
        if (_shop == null || _shop.Deleted || _shop.Map == null || _shop.Map == Map.Internal)
        {
            return BotDoing.Failed("no merchant selling glass");
        }

        if (!body.InRange(_shop.Location, BotShops.CounterReach))
        {
            // Followed rather than aimed at: a shopkeeper wanders. See BotPeddle.
            return BotDoing.Walk(_shop.Map, _shop, BotArrival.Within(BotShops.CounterReach), $"to {_shop.Name} for glass");
        }

        if (BotShops.Buy(bot, _shop, typeof(Bottle), _take, out var refused) <= 0)
        {
            // The shelf emptied by whoever got here first: on to the next counter with glass on it. See BotShops.Next.
            var next = BotShops.Next(bot, _shop, typeof(Bottle), ref _repicks);

            if (next != null)
            {
                _shop = next;

                return BotDoing.Walk(next.Map, next, BotArrival.Within(BotShops.CounterReach), $"on to {next.Name} for glass");
            }

            return BotDoing.Failed(refused ?? "no glass to be had");
        }

        _leg = Leg.Work;

        return default;
    }

    /// <summary>
    /// The funded round's counter (03.10.2026, econ13): what the pack still lacks for the draughts, priced at this counter, lent
    /// by the buyer's account and spent here; what the counter did not take goes straight back. See <see cref="BotAdvance.Lend"/>.
    /// </summary>
    private BotDoing Fund(IBotWilful bot, Mobile body)
    {
        var buyer = _order?.Buyer?.Self;

        if (!BotMarket.Stands(_order) || buyer is not { Deleted: false, Alive: true })
        {
            return BotDoing.Failed("the want it meant to brew for is gone");
        }

        if (_shop == null || _shop.Deleted || _shop.Map == null || _shop.Map == Map.Internal)
        {
            return BotDoing.Failed("no counter selling glass and herbs");
        }

        if (!body.InRange(_shop.Location, BotShops.CounterReach))
        {
            return BotDoing.Walk(_shop.Map, _shop, BotArrival.Within(BotShops.CounterReach), $"to {_shop.Name} for glass and herbs on {buyer.Name}'s money");
        }

        // What is still missing now, at this counter's prices: the pack may have changed on the walk.
        var recipe = BotAdvance.Able(body, _potion, out _);

        if (recipe == null)
        {
            return BotDoing.Failed("its Alchemy no longer carries the draught");
        }

        var (reagent, per, glass) = BotFlask.Costs(recipe);
        var lackGlass = Math.Max(0, _fundUnits * glass - BotFlask.Bottles(body));
        var lackHerbs = Math.Max(0, _fundUnits * per - BotFlask.Amount(body, reagent));

        if (lackGlass + lackHerbs <= 0)
        {
            _leg = Leg.Work;

            return default;
        }

        var factor = Math.Max(1.0, BotBurgh.Factor(body, _shop));
        var glassPrice = lackGlass > 0 ? (int)Math.Ceiling(BotShops.Price(_shop, typeof(Bottle)) * factor) : 0;
        var herbPrice = lackHerbs > 0 ? (int)Math.Ceiling(BotShops.Price(_shop, reagent) * factor) : 0;

        if (lackGlass > 0 && glassPrice <= 0 || lackHerbs > 0 && herbPrice <= 0)
        {
            return BotDoing.Failed($"{_shop.Name} sells no glass or no {reagent?.Name} now");
        }

        var cost = lackGlass * glassPrice + lackHerbs * herbPrice;

        if (cost > _fundUnits * _order.Offer)
        {
            return BotDoing.Failed($"{buyer.Name}'s {_order.Offer}gp a draught no longer pays for its glass and herbs at {cost}gp");
        }

        if (!BotAdvance.Lend(bot, _order, _fundUnits, cost))
        {
            return BotDoing.Failed($"{buyer.Name}'s money would not put {cost}gp down for its glass and herbs");
        }

        var before = BotYield.Wealth(body);
        var bottles = lackGlass > 0 ? BotShops.Buy(bot, _shop, typeof(Bottle), lackGlass, out _) : 0;
        var herbs = lackHerbs > 0 ? BotShops.Buy(bot, _shop, reagent, lackHerbs, out _) : 0;

        BotAdvance.Spent(bot, Math.Max(0, before - BotYield.Wealth(body)), bottles, herbs);

        logger.Information(
            "{Name} bought {Bottles} bottles and {Herbs} {Reagent} on {Buyer}'s money for its want of {Units} {Potion} (want #{Id})",
            body.Name,
            bottles,
            herbs,
            reagent?.Name,
            buyer.Name,
            _fundUnits,
            _potion?.Name,
            _order.Id
        );

        _leg = Leg.Work;

        return default;
    }

    /// <summary>
    /// One attempt at a time, counted out of the pack <b>before</b> the next one is made.
    ///
    /// <para>
    /// <b>Crafting is asynchronous, and reading the pack in the same breath as the swing reads it too
    /// early.</b> <c>CraftItem.Craft</c> ends at <c>new InternalTimer(...).Start()</c> — the bottle appears a
    /// second or so later — so a leg that swung and then counted saw no change every single time, decided
    /// nothing had come of it, and gave the round up: 0 finished against 34 failed and 20 dropped in the
    /// twenty minutes to 11:31 on 04.09.2026, on a trade that was in fact brewing. The needle got this right
    /// long ago and says so in as many words; this is its shape, and <c>BotFletch</c> had the same fault.
    /// </para>
    /// </summary>
    private BotDoing Brewing(Mobile body)
    {
        var tool = BotFlask.Kit(body);

        // Only before the first attempt: after it, a mortar that is gone is a mortar that wore through. See below.
        if (tool == null && _swings == 0)
        {
            return BotDoing.Failed("nothing to brew with");
        }

        if (!_counting)
        {
            // Seeded rather than started from nought: a healer walks into this carrying its own draughts,
            // and counting those as made would price the round at what it did not do.
            _counting = true;
            _had = BotFlask.Made(body, _potion);
        }

        // What the last attempt produced, counted before the next one is made. Failures produce nothing and
        // are supposed to: that is what the herbs are paying for.
        var have = BotFlask.Made(body, _potion);

        if (have > _had)
        {
            _bottled += have - _had;
            BotCraftwork.Produced(_potion, have - _had);
            _had = have;

            // The island's price for what somebody asks for, the surplus price for the rest (01.10.2026). See BotGlut.Yield.
            _made = BotGlut.Yield(_potion, _bottled, BotFlask.Worth);
        }

        // <b>A mortar that wears through ends the round, not the work in it.</b> The scribe's pen on this bench — see
        // BotInscribe.Writing: the engine destroys a tool on its last use, and this failed "nothing to brew with"
        // with the bottles just filled still in the pack. The last attempt is let land, then Finish carries what was
        // made to market as it does when the herbs run out, and fails only when nothing was.
        if (tool == null)
        {
            if (_swung && Core.TickCount - _swungTick < SwingMs)
            {
                return BotDoing.Work($"brewing, {_bottled} bottles so far");
            }

            return Finish("the mortar wore through");
        }

        var recipe = BotFlask.Recipe(body, _potion);

        if (recipe == null)
        {
            // Not a fault of this bot's, and the two halves are worth telling apart for whoever reads a run
            // of these: glass comes off a counter and the herb has to be walked into a wood for.
            return Finish(BotFlask.Bottles(body) <= 0 ? "no glass left to brew into" : "no herbs left to brew with");
        }

        var (reagent, _, _) = BotFlask.Costs(recipe);

        // Both halves on one number, because a swing spends both and either running dry ends the round. The
        // needle watches its one material for exactly this: a craft that consumes on failure too moves this
        // through an honest run of bad luck, and leaves it stock still when the action lock has jammed.
        var left = BotFlask.Amount(body, reagent) + BotFlask.Bottles(body);

        if (_lastLeft != left)
        {
            _lastLeft = left;
            _stirTick = Core.TickCount;
        }

        if (Core.TickCount - _stirTick >= StallMs)
        {
            logger.Information(
                "{Name}'s mortar stopped: {Swings} attempts, {Bottles} made, {Left} of herb and glass left untouched for {Stall}s",
                body.Name,
                _swings,
                _bottled,
                left,
                StallMs / 1000
            );

            return Finish($"the mortar has not moved in {StallMs / 1000}s, with {left} of herb and glass in the pack");
        }

        if (_swung && Core.TickCount - _swungTick < SwingMs)
        {
            return BotDoing.Work($"brewing, {_bottled} bottles so far");
        }

        _swings++;
        _swung = true;
        _swungTick = Core.TickCount;

        BotFlask.Swing(body, recipe, reagent, tool);

        return BotDoing.Work($"brewing, {_bottled} bottles so far");
    }

    private BotDoing Finish(string why)
    {
        if (_bottled <= 0)
        {
            return BotDoing.Failed(why);
        }

        _leg = Leg.Sell;

        return default;
    }

    /// <summary>
    /// What was brewed, counted: the class's own draughts kept, what an order waits for, and the rest. Nothing leaves the bench any
    /// more (01.10.2026): the order's draughts ride in the pack to whoever ordered them (<see cref="BotCourier"/>), and the rest wait
    /// for the porter — the bank box, the shop by Britain's bank, a shopkeeper.
    /// </summary>
    private BotDoing Selling(IBotWilful bot, Mobile body)
    {
        // A funded round lets go of the units it held on the want: the carrier's walk takes them now, and the hand-over settles the
        // advance (econ13). See BotAdvance.Settle.
        if (_funded && !_released)
        {
            _released = true;
            _order?.Release(bot);
        }

        var ordered = 0;
        var listed = 0;

        // What the class itself carries stays in the pack: a healer that sells the heal potion it is
        // standing up with has made the shard poorer, not richer.
        var keep = Keeps(bot);

        List<Item> made = BotCraftwork.Gather(body, _potion);

        for (var i = 0; i < made.Count; i++)
        {
            var stack = made[i];
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
                    // The split could not be made, so the whole stack stays rather than being sold out from
                    // under a bot that needs one of them. See the twin of this line in BotUnload.
                    continue;
                }

                keep = 0;
                held = Math.Max(1, stack.Amount);
            }

            var want = _order ?? BotMarket.Demand(bot, _potion);
            var answer = want == null ? 0 : Math.Min(held, Math.Max(0, want.Payable - ordered));

            ordered += answer;
            listed += held - answer;
        }

        if (ordered > 0)
        {
            logger.Information(
                "{Name} brewed {Bottles} bottles of {Potion} and {Ordered} of them are for an order, to be carried to it",
                body.Name,
                _bottled,
                _potion?.Name,
                ordered
            );
        }

        return BotDoing.Done($"{_bottled} bottles in {_swings} attempts, {ordered} for an order and {listed} kept to sell");
    }

    /// <summary>How many of what it just brewed this bot is meant to be carrying itself.</summary>
    private int Keeps(IBotWilful bot)
    {
        var bottles = BotOutfit.PotionsFor(bot?.Class);

        for (var i = 0; i < bottles.Count; i++)
        {
            if (bottles[i].Kind == _potion)
            {
                return bottles[i].Count;
            }
        }

        return 0;
    }
}
