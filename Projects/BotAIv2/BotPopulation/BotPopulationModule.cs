using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The population as a module: reads who should exist, deletes whoever came back from the save, raises the
/// rest, and starts the clock.
///
/// <para>
/// <b>This is the module that turns six written subsystems into a running shard.</b> Everything else has been
/// waiting for an object that holds a bond, a journey and a resolve, and gets asked to act — and until this
/// existed, none of it ran at all.
/// </para>
///
/// <para>
/// <see cref="BotPhase.World"/>, and this one genuinely needs it: a bot is placed on a map. Requires
/// <c>Classes</c> (what a bot is), <c>Movement</c> (the pace of the clock comes from a step's delay) and
/// <c>Will</c> (a turn is a decision followed by a step). <b>Not</b> <c>Harvest</c> — a population with
/// nothing to do is a legitimate state, and it is the state that proves the census is telling the truth
/// rather than covering for a missing subsystem.
/// </para>
///
/// <para>
/// Turned off, the shard is exactly what it was before this file existed: seven subsystems loaded, nobody to
/// use them. That is the cleanest possible A/B for any question of the form "is this the bots or is this the
/// shard".
/// </para>
/// </summary>
public sealed class BotPopulationModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPopulationModule));

    public override string Name => "Population";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Classes", "Movement", "Will"];

    public override void Start()
    {
        BotPopulationConfig.Load();

        // The rest clock's own file: its dials, the prime time schedule and the critical floors (02.10.2026). Before the
        // reclaim below, which asks the clock who is resting.
        BotRestConfig.Load();

        // Going back for your own corpse is ordinary work, weighed against everything else. It lives here
        // rather than with the hunt because the corpse it is about is the bot's own, and where a bot fell is
        // something only the population knows.
        BotWill.Offer(new BotUndertaker());

        // Taking a full pack to the counter. It lives here for the same reason: what a bot is carrying, and
        // whether that is about to stop it walking, is the population's business rather than any trade's.
        BotWill.Offer(new BotPorter());

        // Coming back when there is nothing worth doing out there. It lives here because where the
        // population lives is the population's own fact, and because it is the only errand on the shard
        // whose subject is the camp rather than anything in the world. See BotHomeward: six casters stood
        // together 350 tiles south of it on 02.09.2026 with no money and no work, and not one of the
        // thirty-four proposers had an answer that amounted to "then go home".
        BotWill.Offer(new BotHomer());


        // Patrick's order of 18.09.2026: what the save returns is taken back rather than thrown away, and only
        // the shortfall is raised. See BotPopulation.Reclaim for why configuration is still the authority — and
        // BotGrowth for the newcomers laid over it, which the configuration alone would delete.
        var mix = BotGrowth.Mix(BotPopulationConfig.Mix);
        var deleted = BotPopulation.Reclaim(mix, out var kept);
        var back = 0;

        foreach (var (_, many) in kept)
        {
            back += many;
        }

        if (back > 0 || deleted > 0)
        {
            logger.Information(
                "{Back} bots came back from the world save with their place, their pack, their bank and what they had learned, {Ghosts} of them dead at the save and left for the reviver; {Deleted} were deleted for not being asked for any more",
                back,
                BotMobile.BackAsGhost,
                deleted
            );
        }

        var born = BotPopulation.Raise(mix, kept);

        if (born == 0 && back == 0)
        {
            // Loud, because every other subsystem will now report zero of everything and none of them is at
            // fault. A shard with no bots is not a broken brain, it is an empty world.
            logger.Error(
                "No bots were raised, so nothing else in this assembly will do anything. Check the class names and the home point in Configuration/bot-population.json"
            );

            return;
        }

        BotBeat.Start();

        logger.Information(
            "Population raised: {Born} bots at {Where} on {Map}; {State}",
            born,
            BotPopulation.Where,
            BotPopulation.Home,
            BotBeat.Describe()
        );

        // Patrick's order of 24.09.2026: bots tire, rest and come back, and newcomers arrive every two hours. After
        // the population exists, because the clock it starts reads it.
        BotRest.Start();
        BotPeoples.Load();

        // <b>After the whole roster exists, and not before.</b> Patrick's rules of 09.09.2026 are statements
        // about a group — five at least, one of them able to make things, fifteen at most — and a bot being
        // born cannot be told whether it satisfies any of them. See BotGuilds.Muster.
        BotGuilds.Muster();

        // And The Shadow, whose members the muster has just dealt into other guilds. See BotUnderworld.Reform.
        BotUnderworld.Reform();

        // Said separately and always, including the nought. "Nobody was remembered" and "nobody was saved in
        // the first place" look identical in a log that prints neither, and this is the line that will be
        // read on the morning somebody wonders why the smith is a novice again.
        logger.Information(
            "Learning carried over: {Restored} of {Remembered} remembered bots picked up where they left off, with {Returned}gp of earlier earnings handed back (savings carry over: {Savings})",
            BotProgress.Restored,
            BotProgress.Remembered,
            BotProgress.Returned,
            BotProgress.Savings
        );
    }

    /// <summary>
    /// A world reload is a different world, and every bot in this one belongs to the world being replaced.
    /// The clock stops first: a beat that runs while the population is being torn down is a beat over deleted
    /// bots.
    /// </summary>
    public override void Reset()
    {
        BotGuilds.Forget();
        BotCharter.Forget();

        // <b>The island is no longer cleared here, and that is the point of saving it.</b> This used to drop
        // every quadrant on a reload, on the reasoning that the records name a Map and a Map from the world
        // being replaced is a deleted object. The facets themselves are not replaced — Map.Maps outlives any
        // world — and what the records really hold is coordinates and counters. See BotQuadStore: the
        // ground's reputation is written to disk and read back, so it survives a reload and a restart both.
        logger.Information("The island, before the reload: {State}", BotQuad.Describe());

        logger.Information("Population, before the reload: {State}", BotPopulation.Describe());

        BotRest.Stop();
        BotBeat.Reset();
        BotPopulation.Reset();
    }

    public static string Summarise() => BotPopulation.Describe();
}
