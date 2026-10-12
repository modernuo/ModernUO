using System;
using Server.BotAI.V2;
using Server.Logging;

namespace Server.BotAI.Mind;

/// <summary>
/// The debugger as a module of the shard's own bot system, registered from outside it.
///
/// <para>
/// <see cref="BotPhase.World"/>, and after <c>Population</c>: there is nobody to watch until the population
/// has been raised, and the debugger's body is placed relative to where the population lives. It does not
/// require <c>Will</c> — a population that decides nothing is a legitimate state of the shard, and it is
/// precisely the state somebody would most want a debugger for.
/// </para>
///
/// <para>
/// <b>It offers nothing into the auction and nothing offers anything to it.</b> Every other module here
/// either proposes work or answers for a bot's body; this one only reads. That is what lets it be switched
/// off with no effect on anything — and switched on with no effect either, which is the harder half and the
/// reason the observer is not a <c>BotMobile</c>. See <see cref="BotDebugger"/>.
/// </para>
/// </summary>
public sealed class BotDebugModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDebugModule));

    public override string Name => "Debugger";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population"];

    public override void Start()
    {
        BotDebugConfig.Load();
        BotDebugMemory.Load();
        BotDebugLog.Open(BotVigil.Name);
        BotHand.Open(BotVigil.Name);

        // After every other module has loaded its configuration, which is what makes the values this reads
        // the ones the shard is actually running with rather than the ones the source is written with.
        BotDials.Open();

        BotVigil.Start();
        BotHail.Listen();
        BotConsole.Open();

        // The two seams through which a revel touches the population. Both live in BotAIv2 and are filled
        // from here, never called from there: the minds depend on the bots and must not be depended on. See
        // BotAppraisal.Revelry.
        BotAppraisal.Revelry = BotRevel.Worth;

        // The other half of the same seam: the price goes out through Revelry, and what the notice says goes
        // out through here, so the dashboard can show a revel without this assembly being referenced back.
        BotCrier.Posted = BotRevel.Notice;

        // Where the squad stands, for the dashboard's teleport buttons. Patrick's order of 13.09.2026: the
        // helpers reachable the way the watcher is.
        BotCrier.Watchers = BotVigil.Standing;
        BotWill.Completed = BotRevel.Did;

        // What the watcher finds under the island goes into the watcher's own file. See BotHalls: the chart
        // is the bots' knowledge and lives with them, and only the telling of it belongs here.
        BotHalls.Noted = BotDebugLog.Write;

        // The squad as witness to the guilds' meetings, and the model as the duke's and the towns' voice: three hooks in
        // BotParley, filled from here as the revel's are. Patrick's order of 29.09.2026, evening. See BotWitness.
        BotWitness.Open();

        // The squad as the zones' walkers: who is free to go, and how they get there. Patrick's order of 30.09.2026, evening:
        // Argus and the other debuggers walk the world and look at every mob zone. See BotZoneTour.
        BotZoneTour.Walkers = BotVigil.TourWalkers;
        BotZoneTour.Move = BotVigil.Carry;

        // One of them wholly on the zones while the zone watch runs, and the watch's verdicts in the lead's feed (07.10.2026).
        // See BotZoneWatch.
        BotZoneTour.KeeperHook = BotVigil.KeeperName;
        BotZoneWatch.Noted = line => BotDebugLog.Write($"{BotVigil.Name}: {line}");

        // And a debugger stands on each tile the reach map's check examines (01.10.2026). See BotReachCheck.
        BotReachCheck.Walkers = BotVigil.Walkers;
        BotReachCheck.Move = BotVigil.Carry;

        // The squad on the dashboard's live window: where each stands, what it is doing and thinking (02.10.2026). See BotDebugWeb.
        BotWebHooks.Debuggers = BotDebugWeb.Report;

        // The events of the world (07.10.2026, events17): Argus's herald calls them, the crown's purse fills a rumour's chest, and
        // the championship is held at the fair. See BotEventsArgus.
        BotEventsArgus.Open();

        // The model as a town's steward, when it is free (07.10.2026, steward20). See BotStewardArgus.
        BotStewardArgus.Open();
        // The guilds' bouts, called by their heralds, announced by Argus's (07.10.2026, officers20). See BotOfficersArgus.
        BotOfficersArgus.Open();

        _timer?.Stop();
        _timer = new VigilSummaryTimer(TimeSpan.FromMilliseconds(SayEveryMs));
        _timer.Start();

        // The numbers it is actually running with, said once, because a belief about what the debugger is
        // watching for that was formed from the defaults in the source can be wrong by a factor of two
        // without anything looking odd. This shard's rule, and it applies to the thing that enforces it too.
        // Whether anything else is thinking is read rather than asserted. The line used to say "the
        // population thinks with something else", which stopped being true the moment the minds were stood
        // down — and a fact that goes stale in the first line anybody reads is worse than no fact.
        logger.Information(
            "{Name} the debugger is watching, thinking with {Model} ({Others}) held for {Hold} after each answer: measuring every {Sample}ms, moving every {Hover}ms, reporting every {Report}ms and reflecting every {Reflect}ms. Frozen after {Frozen}s of standing while walking, silent work after {Silent}m, no-progress judged after {Settled}m. Its thinking goes to {Log}",
            BotVigil.Name,
            BotVigil.Model,
            BotMinds.All.Count == 0
                ? "nothing else on this shard is thinking, so the card is its own"
                : $"the population's {BotMinds.All.Count} minds think with something else",
            BotVigil.KeepAlive,
            BotVigil.SampleMs,
            BotVigil.HoverMs,
            BotVigil.ReportMs,
            BotVigil.ReflectMs,
            BotWatch.FrozenMs / 1000,
            BotWatch.ImmortalMs / 60000,
            BotWatch.SettledMs / 60000,
            BotDebugLog.Path ?? "nowhere"
        );

        // What it may do to the population as well as what it watches for. A revel moves prices and puts
        // camps on the ground, so the numbers behind it belong in a startup line like every other threshold
        // on this shard.
        logger.Information(
            "It may also declare a revel every {Every}m: one trade worth ×{Bonus} for {Holds}m with a prize up to {Prize}gp, out of a treasury of {Treasury}gp for the session",
            BotRevel.EveryMs / 60000,
            BotRevel.Bonus,
            BotRevel.HoldsMs / 60000,
            BotRevel.MostPrize,
            BotRevel.Treasury
        );

        if (BotMarshal.Standing != null)
        {
            logger.Information(
                "{Marshal} is the marshal of events among them, asked every {Every}m for a revel, a camp, a tournament, an errand on the board, a standing order, a bounty, a price on a head or a fair; the revels are its business while it stands",
                BotMarshal.Name,
                BotMarshal.EveryMs / 60000
            );
        }
    }

    /// <summary>
    /// How often the debugger says what it has been doing, in its own line in the shard's log.
    ///
    /// <para>
    /// <b>It never said any of it.</b> <see cref="Summarise"/> has existed since the debugger did, composing
    /// a line that carries the only numbers by which this observer can be judged - how many bots it reminded
    /// and shook, and of those, how many were going again by the next window. Nothing called it. The Baron
    /// and the captain print theirs on a clock; the thing whose whole purpose is measurement printed nothing,
    /// so eleven hundred interventions accumulated with no record anywhere of whether one of them helped.
    /// A number nobody prints is a number nobody has.
    /// </para>
    /// </summary>
    public static int SayEveryMs { get; set; } = 300000;

    private static Timer _timer;

    private sealed class VigilSummaryTimer : Timer
    {
        public VigilSummaryTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => logger.Information("The debugger: {What}", Summarise());
    }

    /// <summary>What it has done, for whoever is reading the session log.</summary>
    public static string Summarise() => $"{BotVigil.Describe()}; {BotRevel.Describe()}; {BotVenues.Describe()}; {BotWitness.Describe()}";

    public override void Reset()
    {
        // The population is about to be replaced, so nothing it did counts towards a contest.
        BotAppraisal.Revelry = null;
        BotCrier.Posted = null;
        BotCrier.Watchers = null;
        BotWill.Completed = null;
        BotHalls.Noted = null;
        BotZoneTour.Walkers = null;
        BotZoneTour.Move = null;
        BotZoneTour.KeeperHook = null;
        BotZoneWatch.Noted = null;
        BotReachCheck.Walkers = null;
        BotReachCheck.Move = null;
        BotWebHooks.Debuggers = null;
        BotEventsArgus.Close();
        BotStewardArgus.Close();
        BotOfficersArgus.Close();
        BotWitness.Close();
        BotWitness.Forget();
        BotRevel.Forget();
        BotWaves.Forget();

        logger.Information("The debugger, before the reload: {What}", Summarise());

        _timer?.Stop();
        _timer = null;

        BotHail.Forget();
        BotHail.Reset();
        BotVigil.Reset();
    }
}
