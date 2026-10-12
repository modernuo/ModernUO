using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a bot with tinker's tools and some iron a stint at making the tool the island is shortest of, and
/// offers it the board's orders first.
///
/// <para>
/// <b>Demand is read off the stalls, not guessed.</b> A tool is offered only while the market holds fewer than
/// <see cref="LeastOnMarket"/> of it, so the tinker stops when the stalls are stocked and starts again when
/// somebody buys — which is the glass rule (a material with no producer must not be ordered by the armful)
/// turned the other way round: a producer with no reader of demand must not make by the armful.
/// </para>
///
/// <para>
/// <b>The iron is read off the stalls too (30.09.2026).</b> This asked only its own pack, so a tinker was a tinker only on the
/// afternoon it had dug and smelted iron itself — and on that afternoon its forge outbid it for the same ingots. Build 336, its
/// first 105 minutes: 1331 asks, 1211 "had no iron", 112 offered for the stalls and none of them taken ("over tinker" 26
/// times, every time under the forge, at 123–561 a minute against the forge's 288–1370), no tool made — while the market held
/// 59 to 618 iron ingots, most of the time over two hundred, at six gold ("The board: most stocked: IronIngot"), and the population bought 125 scissors, 47 skinning knives, 44 pickaxes and 10
/// hatchets over counters for 3240gp. A tinker short of iron now buys a batch's worth off the cheapest stall as the stint's
/// first leg — placeless, so no walk — priced as the stint's outlay, and counted apart when it cannot pay for it.
/// </para>
/// </summary>
public sealed class BotTinkerer : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTinkerer));

    /// <summary>Fewest ingots worth beginning a stint with: enough for one tool and a miss.</summary>
    public static int LeastMetal { get; set; } = 8;

    /// <summary>Fewest of a tool on the stalls before making more is offered.</summary>
    public static int LeastOnMarket { get; set; } = 2;

    private static bool _saidNoSystem;

    private static bool _said;

    public static long Asked { get; private set; }

    public static long NoKit { get; private set; }

    public static long NoMetal { get; private set; }

    public static long ToOrder { get; private set; }

    public static long OnSpec { get; private set; }

    public static long Stocked { get; private set; }

    /// <summary>Stints offered with the iron to be bought off a stall first. See the class note.</summary>
    /// <summary>Iron on a stall this tinker could not pay a batch's worth of, and the fattest purse among them.</summary>
    public string Name => "Tinkerer";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        if (BotTinkering.Kit(body) == null && BotTinkering.Seed(body))
        {
            logger.Information("{Name} was given its tinker's tools: no shop within its reach sells them, and the class is born with them now", body.Name);
        }

        if (BotTinkering.Kit(body) == null)
        {
            NoKit++;

            return null;
        }

        Asked++;

        if (BotTinkering.System == null)
        {
            if (!_saidNoSystem)
            {
                _saidNoSystem = true;

                logger.Error("The tinkering system does not exist yet, so nobody can make tools");
            }

            return null;
        }

        // Its own iron out of its own bank box, standing at a bank; another bot's iron comes by a carrier to the metal its trade
        // has asked for (BotBullion), since the stalls it was bought off from anywhere went on 01.10.2026.
        if (BotTinkering.Ingots(body) < LeastMetal)
        {
            BotMarket.FromBank(bot, BotTinkering.Metal, LeastMetal * 2);
        }

        if (BotTinkering.Ingots(body) < LeastMetal)
        {
            NoMetal++;

            return null;
        }

        var order = Order(bot, body);
        var tool = order?.Kind ?? BotTinkering.Choose(body, LeastOnMarket, out _);

        if (tool == null)
        {
            Stocked++;

            return null;
        }

        if (order != null)
        {
            ToOrder++;
            order.Offered();
            Once(body, order.Kind.Name, "to order");

            return new BotTinker(map, body.Location, order.Kind, order);
        }

        // A tool nobody ordered needs a place in the pack (06.10.2026, work17). See BotRoom.SpecShut.
        if (BotRoom.SpecShut(body))
        {
            return null;
        }

        OnSpec++;
        BotGlut.Spec(tool);
        Once(body, tool.Name, "because the island is short of it");

        return new BotTinker(map, body.Location, tool, null);
    }

    private static BotWant Order(IBotWilful bot, Mobile body)
    {
        var wants = BotMarket.Wants;
        BotWant best = null;
        var bestWorth = 0;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (!want.IsOpen || ReferenceEquals(want.Buyer, bot) || !BotTinkering.IsTool(want.Kind) || want.Worth <= bestWorth)
            {
                continue;
            }

            if (BotTinkering.Recipe(body, want.Kind) == null)
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

    private static void Once(Mobile body, string tool, string why)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information("{Name} is the first to tinker: {Tool}, {Why}", body.Name, tool, why);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody with tinker's tools has been asked"
            : $"{Asked} asked to tinker: {ToOrder} to order, {OnSpec} for the island's stock, {Stocked} found every tool stocked, {NoMetal} had fewer than {LeastMetal} iron in the pack (or in the box at a bank); {NoKit} asked without tinker's tools; {BotTinker.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        NoKit = 0;
        NoMetal = 0;
        ToOrder = 0;
        OnSpec = 0;
        Stocked = 0;
        _said = false;
    }
}
