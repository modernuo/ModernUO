using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Making arrows: buy the wood if it is short of it, cut the shafts, feather them, and hand them to whoever
/// put the money down.
///
/// <para>
/// <b>Three legs and no station.</b> The smith walks to a forge and the tailor to a counter; a fletcher works
/// wherever it is standing, so the only walking here is to a carpenter for logs and to a counter to sell.
/// See <see cref="BotFletching"/> for why this trade exists and why the feather is the half that matters.
/// </para>
///
/// <para>
/// <b>Feathers are never bought from a shopkeeper, because no shopkeeper on this shard has one.</b> They
/// come out of the pack — a hunter that went through a bird's corpse is carrying them — or off the
/// population's own market, where that hunter listed them. That is the whole loop Patrick asked for, and it
/// is the same shape as the leather one: something is killed, the market moves the parts, a crafter turns
/// them into a thing the killer needs.
/// </para>
/// </summary>
public sealed class BotFletch : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotFletch));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "fletch";

    /// <summary>What a fletching chain is reckoned at before the ledger knows better.</summary>
    public static double Prior { get; set; } = 90.0;

    /// <summary>How long one is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 1.5;

    /// <summary>
    /// How often an attempt is made.
    ///
    /// <para>
    /// Not every beat: CraftItem.Craft takes the CraftSystem action lock and returns silently when it is
    /// already held, so a bot that swings on every beat spends its afternoon waiting for itself. The
    /// needle's figure, for the needle's reason — see BotSew.SwingMs.
    /// </para>
    /// </summary>
    public static int SwingMs { get; set; } = 3000;

    /// <summary>How long the tool may produce nothing and spend nothing before the chain is given up.</summary>
    public static int StallMs { get; set; } = SwingMs * 8;

    public static void Forget()
    {
    }

    private enum Leg
    {
        Wood,
        Work,
        Sell
    }

    private readonly Map _map;

    private readonly Point3D _where;

    private BaseVendor _shop;

    /// <summary>Times this errand has been sent on to another counter because the shelf emptied. See BotShops.Next.</summary>
    private int _repicks;

    private readonly int _price;

    private readonly int _take;

    private readonly BotWant _order;

    /// <summary>The want this is made for, or null on spec. Read by <see cref="BotCraftFirst"/> (03.10.2026, econ13).</summary>
    public BotWant Order => _order;

    private Leg _leg;

    private int _arrows;

    private int _swings;

    private int _made;

    /// <summary>Arrows in the pack when the work began, so its own quiver is not counted as made.</summary>
    private int _had;

    /// <summary>Seeded on the first pass through the work, because the quiver is not empty when it starts.</summary>
    private bool _counting;

    private bool _swung;

    private long _swungTick;

    /// <summary>Material left at the last look. Minus one, which no amount can equal — see BotSew.</summary>
    private int _lastLeft = -1;

    private long _stirTick;

    public BotFletch(Map map, Point3D where, BaseVendor shop, int price, int take, BotWant order = null)
    {
        _map = map;
        _where = where;
        _shop = shop;
        _price = price;
        _take = take;
        _order = order;
        // <b>The leg is chosen by whether wood is wanted, not by whether a shopkeeper was found.</b> See Wood, which takes the
        // bot's own logs out of its bank box at a bank before it walks to a counter.
        _leg = take > 0 ? Leg.Wood : Leg.Work;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    /// <summary>
    /// An order is worth more than speculation: the money for it is already down. See BotSmith. Speculation while the island is full
    /// of arrows is worth less still (01.10.2026). See <see cref="BotGlut.Claim"/>.
    /// </summary>
    public override double Expects => _order != null ? Prior * 1.6 : BotGlut.Claim(typeof(Arrow), Prior);

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => SkillName.Fletching;

    public override int Outlay => _take * _price;

    /// <summary>Its wood is bought over a shopkeeper's counter when it has one to go to. See <see cref="BotDeed.AtCounter"/>.</summary>
    public override bool AtCounter => _shop != null;

    /// <summary>Nothing here is coin. What it produces is arrows, and those are counted as goods.</summary>
    public override double Coin => 0.0;

    public override int Made => _made;

    public override string Stage =>
        _leg switch
        {
            Leg.Wood => $"after {_take} Log for arrows",
            Leg.Work => $"fletching ({_arrows} arrows in {_swings} attempts)",
            _ => $"putting {_arrows} arrows out"
        };

    /// <summary>The carpenter could not be reached. Written under the counter's name, as the porter does.</summary>
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
                Leg.Wood => Wood(bot, body),
                Leg.Work => Fletching(body),
                _ => Selling(bot, body)
            };

            if (doing.Kind != BotDoingKind.None)
            {
                return doing;
            }
        }

        return BotDoing.Failed("could not settle on a next step");
    }

    /// <summary>
    /// Enough wood to feather every feather it holds, and not a log more.
    ///
    /// <para>
    /// The feather is the binding half — nobody sells one — so the wood is bought to match it rather than by
    /// the armful. A fletcher standing on two hundred logs and four feathers has spent money to make four
    /// arrows.
    /// </para>
    /// </summary>
    private BotDoing Wood(IBotWilful bot, Mobile body)
    {
        if (BotFletching.Possible(body) >= BotFletching.LeastArrows)
        {
            _leg = Leg.Work;

            return default;
        }

        // Wood enough for the feathers it holds, which is all a round bought only feathers for needs.
        if (_take <= 0)
        {
            _leg = Leg.Work;

            return default;
        }

        // Its own logs out of its own bank box before paying anybody for the same thing, standing at a bank (01.10.2026).
        if (BotMarket.FromBank(bot, typeof(Log), _take) > 0 && BotFletching.Possible(body) >= BotFletching.LeastArrows)
        {
            _leg = Leg.Work;

            return default;
        }

        // (Another bot's wood off a stall came before the counter until the auction went on 01.10.2026. A woodcutter's logs reach
        // a fletcher now by a carrier's walk to the wood its trade has asked for — BotStores says the want.)
        if (_shop == null || _shop.Deleted || _shop.Map == null || _shop.Map == Map.Internal)
        {
            return BotDoing.Failed("no merchant selling wood");
        }

        if (!body.InRange(_shop.Location, BotShops.CounterReach))
        {
            // Followed rather than aimed at: a shopkeeper wanders. See BotPeddle.
            return BotDoing.Walk(_shop.Map, _shop, BotArrival.Within(BotShops.CounterReach), $"to {_shop.Name} for wood");
        }

        if (BotShops.Buy(bot, _shop, typeof(Log), _take, out var refused) <= 0)
        {
            // The shelf emptied by whoever got here first: on to the next counter with wood on it. See BotShops.Next.
            var next = BotShops.Next(bot, _shop, typeof(Log), ref _repicks);

            if (next != null)
            {
                _shop = next;

                return BotDoing.Walk(next.Map, next, BotArrival.Within(BotShops.CounterReach), $"on to {next.Name} for wood");
            }

            return BotDoing.Failed(refused ?? "no wood to be had");
        }

        _leg = Leg.Work;

        return default;
    }

    /// <summary>
    /// Shafts first, then arrows, one attempt every SwingMs and counted out of the pack <b>before</b> the
    /// next one is made.
    ///
    /// <para>
    /// <b>Crafting is asynchronous, and this leg read the pack in the same breath as the swing.</b>
    /// <c>CraftItem.Craft</c> ends at <c>new InternalTimer(...).Start()</c> — the arrow appears a second or
    /// so later — so "after" was always equal to "before", every round decided nothing had come of it, and
    /// the chain gave itself up twenty seconds later. It was never caught because no fletcher on this shard
    /// had ever had a feather to start a round with: 0 rounds taken on, ever, across every log. The needle
    /// has always done this correctly; this is its shape. Same fault, same repair, in <c>BotBrew</c>.
    /// </para>
    /// </summary>
    private BotDoing Fletching(Mobile body)
    {
        var tool = BotFletching.Kit(body);

        if (tool == null)
        {
            return BotDoing.Failed("nothing to fletch with");
        }

        if (!_counting)
        {
            _counting = true;
            _had = BotFletching.Made(body, typeof(Arrow));
        }

        // What the last attempt produced, counted before the next one is made.
        var have = BotFletching.Made(body, typeof(Arrow));

        if (have > _had)
        {
            _arrows += have - _had;
            BotCraftwork.Produced(typeof(Arrow), have - _had);
            _had = have;

            // The island's price for what somebody asks for, the surplus price for the rest (01.10.2026). See BotGlut.Yield.
            _made = BotGlut.Yield(typeof(Arrow), _arrows, BotFletching.Worth);
        }

        var feathers = BotFletching.Feathers(body);

        if (feathers <= 0)
        {
            // Not a fault of this bot's: the feather is the half nobody sells. Said plainly so that a reader
            // looking at a run of these goes to the hunters rather than to the carpenter.
            return Finish("no feathers left to fletch with");
        }

        // Everything the chain eats on one number, for the reason the needle watches its cloth: a craft that
        // consumes on failure too moves this through an honest run of bad luck and leaves it stock still
        // when the action lock has jammed.
        var left = feathers + BotFletching.Shafts(body) + BotFletching.Logs(body);

        if (_lastLeft != left)
        {
            _lastLeft = left;
            _stirTick = Core.TickCount;
        }

        if (Core.TickCount - _stirTick >= StallMs)
        {
            logger.Information(
                "{Name}'s fletching stopped: {Swings} attempts, {Arrows} made, {Left} of feather and wood left untouched for {Stall}s",
                body.Name,
                _swings,
                _arrows,
                left,
                StallMs / 1000
            );

            return Finish($"the tool has not moved in {StallMs / 1000}s, with {left} of feather and wood in the pack");
        }

        if (_swung && Core.TickCount - _swungTick < SwingMs)
        {
            return BotDoing.Work($"fletching, {_arrows} arrows so far");
        }

        // Wood into shafts when the shafts have run out, otherwise shafts into arrows. One swing either way.
        var cutting = BotFletching.Shafts(body) <= 0;

        if (cutting && BotFletching.Logs(body) <= 0)
        {
            return Finish("no wood left to cut");
        }

        var material = cutting ? typeof(Log) : typeof(Shaft);

        // The arrow has two resources and the general lookup refuses anything with more than one. See
        // BotFletching.Feathering, which is the whole reason an arrow had never been made on this shard.
        var recipe = cutting
            ? BotFletching.Recipe(body, typeof(Log), typeof(Shaft))
            : BotFletching.Feathering(body);

        if (recipe == null)
        {
            return BotDoing.Failed(cutting ? "it does not know how to cut a shaft" : "it does not know how to feather a shaft");
        }

        _swings++;
        _swung = true;
        _swungTick = Core.TickCount;

        BotFletching.Swing(body, recipe, material, tool);

        return BotDoing.Work(cutting ? "cutting shafts" : $"fletching, {_arrows} arrows so far");
    }

    private BotDoing Finish(string why)
    {
        if (_arrows <= 0)
        {
            return BotDoing.Failed(why);
        }

        _leg = Leg.Sell;

        return default;
    }

    /// <summary>
    /// What was fletched, counted: what an order waits for, and the rest. Nothing leaves the bench any more (01.10.2026): the
    /// order's arrows ride in the pack to whoever ordered them (<see cref="BotCourier"/>), the rest wait for the porter.
    /// </summary>
    private BotDoing Selling(IBotWilful bot, Mobile body)
    {
        var ordered = 0;
        var listed = 0;

        List<Item> made = BotThread.Gather(bot?.Self, typeof(Arrow));

        for (var i = 0; i < made.Count; i++)
        {
            var stack = made[i];
            var held = Math.Max(1, stack.Amount);
            var want = _order ?? BotMarket.Demand(bot, typeof(Arrow));
            var answer = want == null ? 0 : Math.Min(held, Math.Max(0, want.Payable - ordered));

            ordered += answer;
            listed += held - answer;
        }

        if (ordered > 0)
        {
            logger.Information(
                "{Name} fletched {Arrows} arrows and {Ordered} of them are for an order, to be carried to it",
                body.Name,
                _arrows,
                ordered
            );
        }

        return BotDoing.Done($"{_arrows} arrows in {_swings} attempts, {ordered} for an order and {listed} kept to sell");
    }
}
