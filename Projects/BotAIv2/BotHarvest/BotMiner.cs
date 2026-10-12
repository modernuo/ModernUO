using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a mining trip to anybody carrying a pick.
///
/// <para>
/// <b>The preconditions live here rather than in the undertaking, and that is the whole point of a
/// proposer.</b> Without a fire on record the chain would end with a bot holding a pack of rock, and the
/// ledger would file that failure against the <em>seam</em> — so the bot would slowly learn that a
/// perfectly good mine was worthless, for a reason that had nothing to do with the mine. Something that
/// cannot be finished is not offered.
/// </para>
///
/// <para>
/// <b>The tool decides who mines, not the class name.</b> A gatherer is born with a pickaxe, bound and
/// weightless; anybody who buys or loots one is a miner while it holds it. The first version asked which
/// archetypes were permitted to work the land, which meant a list that had to be edited every time a class
/// was added — and adding a class silently excluded it.
/// </para>
/// </summary>
public sealed class BotMiner : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMiner));

    private static bool _saidNoFire;

    private static bool _saidNoCounter;

    public string Name => "Miner";

    /// <summary>An ordinary want, weighed against every other ordinary want.</summary>
    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        if (BotOre.Tool(body) == null)
        {
            return null;
        }

        // A pack with no room in it is not offered a seam. See BotDig.Burdened.
        if (BotDig.Burdened(body))
        {
            Burdened++;

            return null;
        }

        // Whatever this bot is standing in the middle of, swept once for everybody. The first bot to ask is
        // the one that asks for it; the sweep refuses politely once the ground here is known.
        BotGround.Survey(map, body.Location);

        // <b>Nothing is offered off ground still being read (01.10.2026, build 382).</b> The sweep is walked a slice a tick on
        // the population's clock now (BotGround.Sweep) rather than whole on this turn, so for a few seconds after the first ask
        // the fires, counters and seams round here are not on the board yet — and the "no fire within its own reach" error
        // below, said once a session, would be said of ground nobody had read.
        if (BotGround.Sweeping(map, body.Location))
        {
            Awaiting++;

            return null;
        }

        if (BotGround.Fire(bot, body.Location) == Point3D.Zero)
        {
            Missing(ref _saidNoFire, "fire", body, map);

            return null;
        }

        if (BotGround.Counter(bot, body.Location) == Point3D.Zero)
        {
            Missing(ref _saidNoCounter, "counter", body, map);

            return null;
        }

        var seam = BotGround.Seam(bot);

        if (seam.Exists)
        {
            return new BotDig(seam);
        }

        // <b>Nothing left to dig is a reason to go looking, not a reason to stop.</b> Patrick's order of
        // 05.09.2026, and the ground had earned it: the seam list ran 509 to 84 in two hours and twenty
        // minutes with 445 struck off as barren behind it, and every sweep this shard makes happens where a
        // bot already is — so the map cannot grow without somebody walking off the edge of it on purpose.
        // See BotProspect, which carries no pickaxe: its arrival is the whole of the work.
        //
        // Offered only to a bot that already has the tool, because that is who the ore is for, and only
        // when there is genuinely nothing to swing at — a prospector sent out while rock is on the board is
        // a miner not mining.
        var frontier = BotGround.Frontier(map, body.Location);

        if (frontier != Point3D.Zero && BotOre.Carried(body) < BotOre.WorthSmelting)
        {
            Sent++;

            return new BotProspect(map, frontier, BotGround.Seams.Count);
        }

        // No rock on record, but a pack with ore in it. A seam at the bot's own feet is the honest way to
        // say "there is nothing to dig here": the undertaking finds nothing to swing at, sees what it is
        // already carrying, and goes straight to the fire with it. Ore left in a pack is ore nobody can buy.
        if (BotOre.Carried(body) < BotOre.WorthSmelting)
        {
            return null;
        }

        // <b>Unless carrying it somewhere has just failed here.</b> This is the one offer in the subsystem
        // whose place is the bot's own feet, so it is the one that repeats identically for as long as the bot
        // stands still — and a bot holding ore it cannot melt stands very still indeed. Everything else is
        // spared this by being keyed to a seam somewhere else; without the check, a miner whose forge cannot
        // be reached takes the same trip and fails it three times a second until the shard restarts.
        var ledger = bot.Resolve?.Ledger;

        if (ledger != null && ledger.Cautious(BotDig.Trade, map, body.Location))
        {
            return null;
        }

        return new BotDig(new BotSeam(map, body.Location, "ore", 0.0));
    }

    /// <summary>Miners sent out past the frontier because there was no rock left on the board.</summary>
    public static long Sent { get; private set; }

    /// <summary>Seams not offered because the bot's pack had no room for the ore. See <c>BotDig.Burdened</c>.</summary>
    public static long Burdened { get; private set; }

    /// <summary>Asks answered with nothing because the ground round the bot was still being swept. See <c>BotGround.Sweep</c>.</summary>
    public static long Awaiting { get; private set; }

    private static void Missing(ref bool said, string what, Mobile body, Map map)
    {
        if (said)
        {
            return;
        }

        said = true;

        // Once, by name, in the same voice the module loader uses for a subsystem that ought to be running.
        // Silence here would look exactly like a population that did not feel like mining.
        //
        // <b>Said of this bot and not of the shard, which is the correction its sisters in BotTailor,
        // BotShopper and BotAlchemist all had to make.</b> BotGround.Counter answers from ONE bot's
        // position, so "Mining is not offered on Felucca" was a shard-wide claim built out of one miner
        // standing somewhere awkward — read at 20:42 on 04.09.2026 beside forty finished mining trips, which
        // is exactly how loudly a line has to disagree with the world before anybody checks it. The article
        // went with it: the parameter carried "a counter" and the sentence supplied its own "no", so the log
        // read "no a counter".
        logger.Error(
            "{Name} at {Where} could not be offered mining on {Map}: no {What} within its own reach, so the trip could not be finished",
            body?.Name ?? "a miner",
            body?.Location ?? Point3D.Zero,
            map,
            what
        );
    }

    /// <summary>
    /// Lets the complaints be made again. Called when the world is reloaded: the next world may well have a
    /// forge in it, and a warning suppressed for ever is a warning about the wrong world.
    /// </summary>
    public static void Forget()
    {
        Burdened = 0;
        Awaiting = 0;
        _saidNoFire = false;
        _saidNoCounter = false;
    }
}
