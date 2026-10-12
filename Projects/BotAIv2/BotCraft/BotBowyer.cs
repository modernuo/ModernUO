using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A bow out of seven logs, and a crossbow out of seven more: the last weapon on this shard nobody made.
///
/// <para>
/// <b>Patrick's sixth point of the night of 29.09.2026: "make the economy self-sufficient."</b> The craft page said it
/// plainly since 28.09: arrows are fletched, blades forged, armour sewn, tools tinkered — and every bow is bought over a
/// counter. <c>DefBowFletching</c> has had the recipe all along: a bow at Fletching 30–70 from seven logs, a crossbow at
/// 60–100, both with the fletcher's tools every crafter is born with. The wood is the island's own (<c>BotChop</c>), so
/// the whole of the archers' kit is now made here.
/// </para>
///
/// <para>
/// <b>Demand is read off the board and the stalls, as the tinker reads it.</b> An order for a bow is filled first; a bow
/// is made on speculation only while the stalls hold fewer than <see cref="BotBowyer.LeastOnMarket"/> — a maker with no
/// reader of demand fills the stalls with bows (C12).
/// </para>
/// </summary>
public sealed class BotBowmake : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBowmake));

    public const string Trade = "bowyer";

    /// <summary>A bow is worth a blade's forging a minute; a tinker's ninety would lose to the forge every time.</summary>
    public static double Prior { get; set; } = 150.0;

    public static double WorkMinutes { get; set; } = 4.0;

    public static int Batch { get; set; } = 2;

    public static int MaxSwings { get; set; } = 10;

    public static int SwingMs { get; set; } = 3000;

    /// <summary>What a bow is guessed to be worth on the stall before the market says. The provisioner sells them at about 35.</summary>
    public static int Guess { get; set; } = 40;

    public static long Stints { get; private set; }

    public static long Wrought { get; private set; }

    public static long Pieces { get; private set; }

    public static long RanOut { get; private set; }

    public static long Fruitless { get; private set; }

    public static long ToOrder { get; private set; }

    public static long Listed { get; private set; }

    private readonly Map _map;

    private readonly Point3D _where;

    private readonly BotWant _order;

    /// <summary>The want this is made for, or null on spec. Read by <see cref="BotCraftFirst"/> (03.10.2026, econ13).</summary>
    public BotWant Order => _order;

    private readonly Type _kind;

    private CraftItem _recipe;

    private int _swings;

    private int _made;

    private int _had = -1;

    private bool _swung;

    private long _swungTick;

    public BotBowmake(Map map, Point3D where, Type kind, BotWant order = null)
    {
        _map = map;
        _where = where;
        _kind = kind;
        _order = order;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    /// <summary>An order more; speculation marked down by the glut of the bow it means to make (01.10.2026). See <see cref="BotGlut.Claim"/>.</summary>
    public override double Expects => _order != null ? Prior * 1.4 : BotGlut.Claim(_kind, Prior);

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => SkillName.Fletching;

    public override double Coin => _order == null ? 0.5 : 1.0;

    /// <summary>The island's price for what somebody asks for, the surplus price for the rest. See <see cref="BotGlut.Yield"/>.</summary>
    public override int Made => BotGlut.Yield(_kind, _made, Guess);

    public override bool Steadfast => true;

    public override string Stage => $"making {_kind?.Name ?? "a bow"} ({_swings} attempts, {_made} made)";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal || _kind == null)
        {
            return BotDoing.Failed("no body");
        }

        var tool = BotFletching.Kit(body);

        if (tool == null)
        {
            return _made > 0 ? Handing(bot, body, "the fletcher's tools wore out") : BotDoing.Failed("no fletcher's tools");
        }

        _recipe ??= BotBowyer.Recipe(body, _kind);

        if (_recipe == null)
        {
            return BotDoing.Failed($"cannot make {_kind.Name} at {body.Skills.Fletching.Base:F0} fletching");
        }

        if (_had < 0)
        {
            _had = BotCraftwork.Made(body, _kind);
        }

        var have = BotCraftwork.Made(body, _kind);

        if (have > _had)
        {
            BotCraftwork.Produced(_kind, have - _had);
            _made += have - _had;
            _had = have;
        }

        if (_made >= Math.Max(1, Batch) || _swings >= MaxSwings)
        {
            if (_made > 0)
            {
                return Handing(bot, body, null);
            }

            Stints++;
            Fruitless++;

            return BotDoing.Failed($"nothing came of the wood in {_swings} attempts{BotCraftEar.Why(body)}");
        }

        var cost = BotCraftwork.Cost(_recipe);

        // Its own logs out of its own bank box, standing at a bank (01.10.2026: the box is reached only there).
        if (BotFletching.Logs(body) < cost && BotMarket.FromBank(bot, typeof(Log), cost * 2) > 0)
        {
            logger.Information("{Name} took its own logs out of its bank box to make {Item}", body.Name, _kind.Name);
        }

        if (BotFletching.Logs(body) < cost)
        {
            if (_made > 0)
            {
                return Handing(bot, body, "out of logs");
            }

            Stints++;
            RanOut++;

            return BotDoing.Failed("out of logs" + BotCraftEar.Why(body));
        }

        if (_swung && Core.TickCount - _swungTick < SwingMs)
        {
            return BotDoing.Work($"making {_kind.Name}");
        }

        _swung = true;
        _swungTick = Core.TickCount;
        _swings++;

        BotCraftwork.Swing(body, BotFletching.System, _recipe, typeof(Log), tool);

        return BotDoing.Work($"making {_kind.Name}");
    }

    private BotDoing Handing(IBotWilful bot, Mobile body, string because)
    {
        var goods = BotCraftwork.Gather(body, _kind);

        goods.RemoveAll(item => BotBinding.IsBound(item, bot?.Bond));

        if (goods.Count == 0)
        {
            Stints++;
            Fruitless++;

            return BotDoing.Done($"{_swings} attempts at {_kind.Name} and nothing to show");
        }

        // Hand to hand (01.10.2026): nothing leaves the bench. What an order waits for stays in the pack for the walk to whoever
        // ordered it (BotCourier); the rest waits for the porter — the bank box, the shop by Britain's bank, a shopkeeper.
        var want = _order ?? BotMarket.Demand(bot, _kind);
        var made = 0;

        for (var i = 0; i < goods.Count; i++)
        {
            made += Math.Max(1, goods[i].Amount);
        }

        var filled = want == null ? 0 : Math.Min(made, want.Payable);
        var listed = made - filled;

        logger.Information(
            "{Name} made {Made} {Item} in {Swings} attempts: {Filled} for somebody's order, to be carried to them, and {Listed} kept{Because}",
            body.Name,
            _made,
            _kind.Name,
            _swings,
            filled,
            listed,
            because == null ? "" : $"; stopped because {because}"
        );

        Stints++;
        Wrought++;
        Pieces += _made;
        ToOrder += filled;
        Listed += listed;

        return BotDoing.Done($"{_made} {_kind.Name} made, {filled} for an order and {listed} kept");
    }

    public static string Describe() =>
        Stints == 0
            ? "no bow-making has ended yet"
            : $"{Stints} bow-making stints ended: {Wrought} with {Pieces} bows made, {ToOrder} for an order and {Listed} kept for the shop by the bank, {RanOut} out of logs, {Fruitless} that made nothing";

    public static void Forget()
    {
        Stints = 0;
        Wrought = 0;
        Pieces = 0;
        RanOut = 0;
        Fruitless = 0;
        ToOrder = 0;
        Listed = 0;
    }
}

/// <summary>Offers a bot with fletcher's tools, the skill and the logs a bow the board wants or the stalls are short of.</summary>
public sealed class BotBowyer : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBowyer));

    /// <summary>The bows, in the order they are made when nothing is asked for.</summary>
    public static readonly Type[] Bows = [typeof(Bow), typeof(Crossbow)];

    /// <summary>Fewest logs worth beginning with: a bow and a miss.</summary>
    public static int LeastWood { get; set; } = 14;

    /// <summary>Fewest of a bow on the stalls before making more is offered.</summary>
    public static int LeastOnMarket { get; set; } = 2;

    public static long Asked { get; private set; }

    public static long NoKit { get; private set; }

    public static long NoWood { get; private set; }

    public static long ToOrder { get; private set; }

    public static long OnSpec { get; private set; }

    public static long Stocked { get; private set; }

    private static bool _said;

    public string Name => "Bowyer";

    public BotStanding Rung => BotStanding.Free;

    public static bool IsBow(Type kind) => kind == typeof(Bow) || kind == typeof(Crossbow);

    /// <summary>The recipe for that bow, if the bot's skill allows an attempt with a fair chance; null otherwise.</summary>
    public static CraftItem Recipe(Mobile bot, Type wanted)
    {
        var system = BotFletching.System;

        if (system?.CraftItems == null || wanted == null || bot == null)
        {
            return null;
        }

        var able = bot.Skills.Fletching.Base;
        var recipes = system.CraftItems;

        for (var i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];

            if (recipe?.ItemType != wanted || !BotCraftwork.Simple(recipe, typeof(Log)))
            {
                continue;
            }

            return BotCraftwork.Requirement(recipe, SkillName.Fletching) <= able - BotThread.Margin ? recipe : null;
        }

        return null;
    }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive || BotFletching.System == null)
        {
            return null;
        }

        if (BotFletching.Kit(body) == null)
        {
            NoKit++;

            return null;
        }

        // Only a bot with the skill for the easiest bow: everybody else asks nothing of this.
        if (Recipe(body, typeof(Bow)) == null && Recipe(body, typeof(Crossbow)) == null)
        {
            return null;
        }

        Asked++;

        if (BotFletching.Logs(body) < LeastWood)
        {
            BotMarket.FromBank(bot, typeof(Log), LeastWood * 2);
        }

        if (BotFletching.Logs(body) < LeastWood)
        {
            NoWood++;

            return null;
        }

        var order = Order(bot, body);

        if (order != null)
        {
            ToOrder++;
            order.Offered();
            Once(body, order.Kind.Name, "to order");

            return new BotBowmake(map, body.Location, order.Kind, order);
        }

        var kind = Choose(body);

        if (kind == null)
        {
            Stocked++;

            return null;
        }

        OnSpec++;
        BotGlut.Spec(kind);
        Once(body, kind.Name, "because the island is short of it");

        return new BotBowmake(map, body.Location, kind);
    }

    private static BotWant Order(IBotWilful bot, Mobile body)
    {
        var wants = BotMarket.Wants;
        BotWant best = null;
        var bestWorth = 0;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (!want.IsOpen || ReferenceEquals(want.Buyer, bot) || !IsBow(want.Kind) || want.Worth <= bestWorth || Recipe(body, want.Kind) == null)
            {
                continue;
            }

            // Only an order a carrier from here could deliver, and not one already made and waiting in the pack for the walk
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

    /// <summary>
    /// The bow the island is shortest of that this bot can make, or null when every bow is stocked: the shops by the bank and the
    /// bot's own pack and bank box (<see cref="BotMarket.Stock"/>, since the stalls went on 01.10.2026).
    /// </summary>
    private static Type Choose(Mobile body)
    {
        Type best = null;
        var shortest = int.MaxValue;

        for (var i = 0; i < Bows.Length; i++)
        {
            var kind = Bows[i];
            var have = BotMarket.Stock(body, kind);

            if (have >= LeastOnMarket || have >= shortest || Recipe(body, kind) == null)
            {
                continue;
            }

            best = kind;
            shortest = have;
        }

        return best;
    }

    private static void Once(Mobile body, string kind, string why)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information("{Name} is the first to make a bow: {Kind}, {Why}", body.Name, kind, why);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody able to make a bow has been asked"
            : $"{Asked} asked to make bows: {ToOrder} to order, {OnSpec} for the island's stock, {Stocked} found every bow stocked, {NoWood} had no logs; {NoKit} asked without fletcher's tools; {BotBowmake.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        NoKit = 0;
        NoWood = 0;
        ToOrder = 0;
        OnSpec = 0;
        Stocked = 0;
        _said = false;
        BotBowmake.Forget();
    }
}
