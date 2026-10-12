using System;
using Server.Engines.Craft;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Making a tool out of iron, wherever the bot is standing, and handing it to whoever asked for it or to the
/// market.
///
/// <para>
/// <b>No place.</b> Tinkering wants no forge, no anvil and no fire — the tinker's tools work in a pack in the
/// road — so this is the one iron trade with no walk in it, which is also why a tinker never fails "no anvil
/// the engine will accept". The shape is the forge's otherwise: count the pack before the next swing, stop
/// when the batch is made, the swings are spent or the iron is gone, and hand over.
/// </para>
///
/// <para>
/// <b>A tool the bot itself uses is kept back.</b> The output is gathered by kind, and the bot's own pickaxe
/// is of that kind. Bound things are left out, and for a tool the class is issued one is left in the pack
/// whatever its binding, because selling one's own pickaxe to make room for the ones just made is the kind of
/// thing this project has done before.
/// </para>
/// </summary>
public sealed class BotTinker : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTinker));

    public const string Trade = "tinker";

    /// <summary>What tinkering is reckoned at per minute before experience corrects it. Below the forge: tools sell for less than blades.</summary>
    public static double Prior { get; set; } = 90.0;

    public static double WorkMinutes { get; set; } = 4.0;

    /// <summary>How many of a tool one stint makes before handing over.</summary>
    public static int Batch { get; set; } = 3;

    /// <summary>Most attempts in a stint. Tinkering fails at the edge of a skill like everything else.</summary>
    public static int MaxSwings { get; set; } = 12;

    /// <summary>How often an attempt is made. The craft has its own timer, and swinging faster hits its lock.</summary>
    public static int SwingMs { get; set; } = 3000;

    /// <summary>What a tool opens on the market at when nothing is known about its price.</summary>
    public static int Guess { get; set; } = 25;

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

    public BotTinker(Map map, Point3D where, Type kind, BotWant order = null)
    {
        _map = map;
        _where = where;
        _kind = kind;
        _order = order;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    /// <summary>An order half again; speculation marked down by the glut of the tool it means to make (01.10.2026). See <see cref="BotGlut.Claim"/>.</summary>
    public override double Expects => _order != null ? Prior * 1.6 : BotGlut.Claim(_kind, Prior);

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => BotTinkering.Skill;

    public override double Coin => _order == null ? 0.4 : 1.0;

    /// <summary>The island's price for what somebody asks for, the surplus price for the rest. See <see cref="BotGlut.Yield"/>.</summary>
    public override int Made => BotGlut.Yield(_kind, _made, Guess);

    public override string Stage => $"tinkering {_kind?.Name ?? "a tool"} ({_swings} attempts, {_made} made)";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal || _kind == null)
        {
            return BotDoing.Failed("no body");
        }

        var tool = BotTinkering.Kit(body);

        if (tool == null)
        {
            return _made > 0 ? Handing(bot, body, "the tinker's tools wore out") : BotDoing.Failed("no tinker's tools");
        }

        _recipe ??= BotTinkering.Recipe(body, _kind);

        if (_recipe == null)
        {
            return BotDoing.Failed($"cannot make {_kind.Name} at {BotTinkering.Able(body):F0} tinkering");
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

            return BotDoing.Failed($"nothing came of the iron in {_swings} attempts{BotCraftEar.Why(body)}");
        }

        var cost = BotCraftwork.Cost(_recipe);

        // Its own iron out of its own bank box, when it tinkers at a bank (01.10.2026: the box is reached only there).
        if (BotTinkering.Ingots(body) < cost && BotMarket.FromBank(bot, BotTinkering.Metal, cost * (Math.Max(1, Batch) + 1)) > 0)
        {
            logger.Information("{Name} took its own iron out of its bank box to make {Item}", body.Name, _kind.Name);
        }

        if (BotTinkering.Ingots(body) < cost)
        {
            if (_made > 0)
            {
                return Handing(bot, body, "out of iron");
            }

            Stints++;
            RanOut++;

            return BotDoing.Failed("out of iron" + BotCraftEar.Why(body));
        }

        if (_swung && Core.TickCount - _swungTick < SwingMs)
        {
            return BotDoing.Work($"tinkering {_kind.Name}");
        }

        _swung = true;
        _swungTick = Core.TickCount;
        _swings++;

        BotCraftwork.Swing(body, BotTinkering.System, _recipe, BotTinkering.Metal, tool);

        return BotDoing.Work($"tinkering {_kind.Name}");
    }

    private BotDoing Handing(IBotWilful bot, Mobile body, string because)
    {
        var goods = BotCraftwork.Gather(body, _kind);

        goods.RemoveAll(item => BotBinding.IsBound(item, bot?.Bond));

        // One of its own kept back, whatever its binding. See the class note.
        if (goods.Count > 0 && BotOutfit.ToolsFor((body as BotMobile)?.Class).Contains(_kind))
        {
            goods.RemoveAt(0);
        }

        if (goods.Count == 0)
        {
            Stints++;
            Fruitless++;

            return BotDoing.Done($"{_swings} attempts at {_kind.Name} and nothing to show");
        }

        // Hand to hand (01.10.2026): nothing leaves the bench. What an order waits for stays in the pack for the walk to whoever
        // ordered it (BotCourier), and the rest waits for the porter — the bank box, the shop by Britain's bank, a shopkeeper. The
        // tinker keeps the spares it made them for (build 362): what it needs to reach Spares is not counted as goods.
        var want = _order ?? BotMarket.Demand(bot, _kind);
        var made = 0;

        for (var i = 0; i < goods.Count; i++)
        {
            made += Math.Max(1, goods[i].Amount);
        }

        var filled = want == null ? 0 : Math.Min(made, want.Payable);
        var listed = made - filled;

        logger.Information(
            "{Name} tinkered {Made} {Item} in {Swings} attempts: {Filled} for somebody's order, to be carried to them, and {Listed} kept, worth {Ask}gp each{Because}",
            body.Name,
            _made,
            _kind.Name,
            _swings,
            filled,
            listed,
            Ask(bot, _kind),
            because == null ? "" : $"; stopped because {because}"
        );

        Stints++;
        Wrought++;
        Pieces += _made;
        ToOrder += filled;
        Listed += listed;

        return BotDoing.Done($"{_made} {_kind.Name} made, {filled} for an order and {listed} kept");
    }

    /// <summary>
    /// What a tool is worth: the market's own price once one is known, never above what the nearest shopkeeper asks for it, never
    /// under the floor.
    ///
    /// <para>
    /// <b>A tool at twenty-five where the provisioner asks eleven is a tool nobody buys.</b> This listed at
    /// <c>Worth(kind, 25)</c>, and <c>BotShopper</c> takes a stall only at or under the counter's price: scissors went for 11gp
    /// over counters 125 times on build 336, a skinning knife for 13 or 14, a skillet for 3. The fletcher's arrows and the
    /// reagents learned this first (<c>BotShops.Shelf</c>).
    /// </para>
    /// </summary>
    public static int Ask(IBotWilful bot, Type kind)
    {
        var worth = BotMarket.Worth(kind, Guess);
        var shelf = BotShops.Shelf(bot, kind, worth);

        return Math.Max(BotMarket.Floor, Math.Min(worth, shelf));
    }

    public static string Describe() =>
        Stints == 0
            ? "no tinkering has ended yet"
            : $"{Stints} tinkering stints ended: {Wrought} with {Pieces} tools made, {ToOrder} for an order and {Listed} kept for the shop by the bank, {RanOut} out of iron, {Fruitless} that made nothing";

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
