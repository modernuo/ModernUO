using System;
using System.Diagnostics;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The population's clock. One timer for everybody, and each bot due on its own schedule.
///
/// <para>
/// <b>The pace is the movement subsystem's own number, not a knob here, and that is the fix for the one
/// defect the first version's ticker had.</b> There, the period was <c>interval × phases</c> — two numbers
/// in a config file whose product nobody checked — and it shipped at 800ms against a walking step of 400ms.
/// The bots were not stuck and nothing in the log looked wrong; they simply moved at half a pedestrian's
/// pace for the entire session, because a bot cannot step more often than it is asked to. Here a bot's next
/// turn is set from <see cref="BotWalk.StepDelayMs"/> when it takes one, so the beat cannot be slower than a
/// step by construction. There is no product to get wrong.
/// </para>
///
/// <para>
/// The timer's own interval is therefore only the resolution: how finely due times can be honoured. It is
/// far shorter than a step, so what it costs is one pass over the population per tick — a hundred and fifty
/// comparisons — and what it buys is that nobody is ever late by more than that interval.
/// </para>
///
/// <para>
/// <b>Spread comes from the due times, not from phases.</b> Bots are seeded staggered across one step's
/// worth of turns when they are born, so the work of a step lands evenly across ticks instead of all of it
/// landing on one. That is what phases were for; done this way it needs no configuration and cannot be set
/// to a value that throttles movement.
/// </para>
/// </summary>
public static class BotBeat
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBeat));

    /// <summary>
    /// How often the timer looks at the population. The resolution of the schedule, not the pace of a bot.
    ///
    /// A tenth of a second: fine enough that a bot due for a step takes it within a quarter of the step's
    /// own delay, coarse enough that the pass costs nothing worth measuring.
    /// </summary>
    public static int IntervalMs { get; set; } = 100;

    private static BeatTimer _timer;

    /// <summary>How many times the clock has looked, and how many turns it has handed out.</summary>
    public static long Ticks { get; private set; }

    public static long Turns { get; private set; }

    public static long Faults { get; private set; }

    /// <summary>Decisions taken between a bot's turns because something had happened to it that would not wait. See BotMobile.Hurry.</summary>
    public static long Hurried { get; private set; }

    public static bool Running => _timer != null;

    public static void Start()
    {
        if (_timer != null)
        {
            return;
        }

        var interval = TimeSpan.FromMilliseconds(Math.Max(10, IntervalMs));

        _timer = new BeatTimer(interval);
        _timer.Start();

        logger.Information(
            "The population's clock is running: looked at every {Interval}ms, each bot taking a turn every {Step}ms",
            IntervalMs,
            BotWalk.StepDelayMs(BotMobile.Runs)
        );
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    public static void Reset()
    {
        Stop();

        Ticks = 0;
        Turns = 0;
        Faults = 0;
        Hurried = 0;
    }

    /// <summary>
    /// Everybody who is due. Bots are visited by index over the live list and deleted ones leave holes
    /// rather than shifting it — see <see cref="BotPopulation.Forget"/> — because a bot can be deleted by
    /// its own turn, and a list that shifts underneath the loop skips whoever moved into the gap.
    /// </summary>
    /// <summary>How often the cost of getting about is reported. The same cadence as the decision census.</summary>
    public static int SummaryMs { get; set; } = 300000;

    private static bool _summarised;

    private static long _summaryTick;

    /// <summary>
    /// What movement has cost lately, said out loud on a clock.
    ///
    /// <para>
    /// <b>These numbers existed and were never printed except on a world reload</b>, which is to say never —
    /// so the one budget the whole population shares was invisible while it was being spent. That is how a
    /// speculative reachability check in a proposer could quietly starve every walking bot on the shard:
    /// searches were shrinking to the floor and coming back with nothing, and the only symptom anybody could
    /// see was bots failing to get anywhere.
    /// </para>
    /// </summary>
    private static void Summarise(long now)
    {
        if (!_summarised)
        {
            _summarised = true;
            _summaryTick = now;

            return;
        }

        if (now - _summaryTick < SummaryMs)
        {
            return;
        }

        _summaryTick = now;

        logger.Information("Getting about: {Paths}; {Walk}; {Reach}; {Refused}; {Footing}; gates: {Gates}; {Danger}; {Sight}", BotPath.Describe(), BotWalk.Describe(), BotReach.Describe(), BotRefused.Describe(), BotFooting.Describe(), BotGates.Describe(), BotDanger.Describe(), BotSight.Describe());
        logger.Information("Navigation: {Tiers}; {Chart}", Server.Engines.Pathing.Tiered.NavigationService.Describe(), BotChart.Describe());

        // <b>The market was in the same position these were, and for longer.</b> Its own summary existed and
        // went to two places: a gump nobody has open at four in the morning, and the world reload. So the one
        // board the whole population trades on was invisible for a whole night, and how full it was could only
        // be inferred from the errors it printed when it overflowed. Said here rather than on the market's own
        // beat because that beat is every thirty seconds and this is a report, not a heartbeat.
        logger.Information("The market: {What}", BotMarket.Describe());

        // Patrick's measure of the board: each line a bot asks for, met in its first hour or not, and why not (02.10.2026, build
        // 391). See BotLines. The same counters are the "needs" block of /api/state.
        logger.Information("Lines: {What}", BotLines.Describe());

        // Supply against spoken demand, by kind, and what the makers did about it (01.10.2026, Patrick's point 8). See BotGlut.
        logger.Information("Balance: {What}", BotGlut.Describe());

        // What the population knows between it. On the fifth tab in full; here because a thing that can only
        // be seen by opening a window is a thing nobody sees at four in the morning, which is the lesson the
        // market's own summary taught two hours ago.
        logger.Information("What we know: {What}", BotCommons.Describe());

        // <b>The same lesson a third time, and this one was worse: BotPurse.Describe had no caller at all.</b>
        // Not printed on a reload, not on a gump, not anywhere — a method that existed and was dead. Meanwhile
        // three subsystems spent the afternoon refusing to buy things for want of money and no line on this
        // shard said how much money there was.
        logger.Information("Money: {What}", BotPurse.Describe());

        // And the one purse that mints: what the city holds, what it has spent and what it is standing for.
        logger.Information("City: {What}", BotCity.Describe());

        // The board of errands: what is asked of the population and what came of it. See BotQuests.
        logger.Information("Quests: {What}", BotQuests.Describe());

        logger.Information("Kit: {What}", BotTidy.Describe());

        // The island's own memory of itself, on the same clock as everything else that is read rather than
        // watched. See BotQuad: this is the shard's standing opinion of its ground, and the tab that shows it
        // is a tab nobody has open at four in the morning.
        logger.Information("The island: {What}; {Hunting}; {Kept}; clearings: {Clearings}", BotQuad.Describe(), BotHunter.Describe(), BotKept.Describe(), BotClearer.Describe());

        logger.Information("At death's door: {What}; {Supplies}", BotMobile.DescribeGasps(), BotShopper.Describe());

        // How long a bot takes to answer what happens to it and to its own: the stopwatch from a blow, a creature seen coming, a
        // failing health, a cry, a wound and a fight's end to the first decision about each (06.10.2026). See BotReflex.
        logger.Information("Reflexes: {What}; the answers: {Cure}", BotReflex.Describe(), BotMobile.DescribeAlarms());

        logger.Information("Standing still: {What}; {Home}", BotStall.Describe(), BotHomer.Describe());




        // <b>Four more of the same, found by asking the question of the whole assembly at once.</b> A Describe
        // with no caller anywhere is the commonest defect in this project by count — BotPurse, BotGround, the
        // market and the commons had all been in this state — so the assembly was searched for the shape
        // rather than for the next instance of it. These four came back: what the woods and the ground give
        // up, what a bot picks off the things it kills, and what it was handed when it was raised.
        logger.Information(
            "Gathering: {Forage}; {Herbs}; {Pickings}; {Outfit}; {Ore}",
            BotForager.Describe(),
            BotHerbalist.Describe(),
            BotPicker.Describe(),
            BotOutfit.Describe(),
            BotOre.Describe()
        );

        // <b>And three more behind a second wall: a live Describe wrapped in a dead Summarise.</b> Eight
        // modules each carry a facade that gathers their own summaries into one line, and not one of those
        // facades has a caller — so a Describe that looked called from a search was reachable only through a
        // method nothing runs. What that hid is the other half of the money question: what the population
        // buys and sells. "The population has 20903gp" and "the population bought nothing all afternoon" are
        // the two halves of the same answer, and only the first of them was ever printed.
        // <b>And the same trap caught the day's own new work within the hour.</b> BotPlunder and BotFreedom
        // were written on 08.09.2026 and their Describes went into BotHuntModule.Summarise — one of the eight
        // dead facades this comment block names three paragraphs above. Printed here instead, where the
        // numbers are actually read.
        // What the wants are made of, not just how many, beside what the shops by the bank show. See BotMarket.Board.
        logger.Information("The board: {What}", BotMarket.Board());

        logger.Information(
            "Spoils: {Chests}; {Prisoners}; {Plunderer}; {Liberator}",
            BotPlunder.Describe(),
            BotFreedom.Describe(),
            BotPlunderer.Describe(),
            BotLiberator.Describe()
        );

        // A restock asked of the island before the walk to a counter, by family (03.10.2026). See BotAskFirst.
        logger.Information(
            "Trade: {Shops}; {Peddling}; {Quarry}; {Standing}; {AskFirst}",
            BotShops.Describe(),
            BotPeddler.Describe(),
            BotQuarry.Describe(),
            BotPopulation.Describe(),
            BotAskFirst.Describe()
        );

        // Births beside the newcomers that make most of them: how each class's newborns leaned (01.10.2026, Patrick's point 7).
        logger.Information(
            "Rest: {Rest}; {Growth}; births: {Births}; inns: {Inns}",
            BotRest.Describe(),
            BotGrowth.Describe(),
            BotBirth.Describe(),
            BotInns.Describe()
        );

        logger.Information("Travel: {Towns}; {Roads}; {Travel}; peoples: {Peoples}", BotTowns.Describe(), BotRoadbook.Describe(), BotTraveller.Describe(), BotPeoples.Describe());

        // Who lives in which town, and who moved (29.09.2026). See BotResidence.
        logger.Information("Residences: {What}", BotResidence.Describe());

        // What the guilds' mages have marked, and the walks and marches their runes saved. See BotRunes.
        logger.Information("Runes: {Runes}", BotRunes.Describe());

        // The ships: the chart, the docks, the lanes, the voyages and the helm. See BotSea (point 7 of 29.09.2026).
        logger.Information("Sea: {Sea}", BotSea.Describe());

        logger.Information("Lessons: {Lessons}", BotTutor.Describe());

        // Fires in the woods, who sat at them and who stopped to talk on the road: Patrick's point 6 of 29.09.2026. See BotCamp.
        logger.Information("Camp: {Camp}", BotCamp.Describe());

        // Bots answering each other's moods — what was heard, what was offered, and how the offers ended: Patrick's point 4 of 01.10.2026. See BotSocial.
        logger.Information("Society: {What}", BotSocial.Describe());

        // What the bots said, channel by channel, an hour at a time, and how much of the trade reached a player in the trade room
        // (02.10.2026, Patrick: the bots "write into the global one and clog half the screen"). BotVoice.Describe had no caller.
        logger.Information("Voice: {What}", BotVoice.Describe());

        // The guilds' officers at the end of the line: posts, the store, the herald, the bouts, the scouts (07.10.2026). See BotOfficers.
        logger.Information("Guilds: {What}", BotGuilds.Describe() + BotOfficers.Clause());

        // What the guilds own, and — the half that matters on an evening when nothing is bought — how far
        // short of owning it they are. A hall is the one thing on this shard that outlives the population,
        // so it is worth a line of its own rather than a clause in somebody else's.
        logger.Information("Estate: {What}", BotEstate.Describe());

        // The guilds' money, guild by guild: the chest and the members' spare that together are what a hall is bought with,
        // the dues that fill the chest, and where each guild stands — seat, hall, ground. The line "why is nothing built"
        // wanted on 29.09.2026, when every guild read "0 of 5000" all day. See BotDues.
        logger.Information("Treasuries: {What}", BotDues.Describe());

        // The guilds' houses in the towns, who is waiting for one and what the survey found empty. See BotGuildHouses.
        logger.Information("Houses: {What}", BotGuildHouses.Describe());

        // The wars and the seats on their own line: the estate's had grown past three thousand characters
        // and a line nobody can read is a line nobody reads. See MAP.md §1.
        logger.Information("Wars: {Wars}; {Seats}", BotWar.Describe(), BotSeat.Describe());

        // The meetings that stand between a grievance and a war, the agreements sworn at them, and the guilds' dealings with
        // their towns (29.09.2026). See BotParley and BotBurgh.
        logger.Information("Diplomacy: {Meetings}; {Towns}; {Watch}", BotParley.Describe(), BotBurgh.Describe(), BotTownWatch.Describe());

        // What the population costs the loop, said on the same clock as everything else. The figures were
        // kept and printed only at boot, where they read nought - see 13.09.2026, when "are the bots slow"
        // had no number to answer with.
        logger.Information("The clock: {What}", Describe());
    }

    /// <summary>Loop time spent in the population's tick, in milliseconds, and the worst single tick.</summary>
    public static double SpentMs { get; private set; }

    public static double WorstMs { get; private set; }

    private static void Tick()
    {
        Ticks++;

        var began = Stopwatch.GetTimestamp();

        try
        {
            Look(began);
        }
        finally
        {
            var ms = (Stopwatch.GetTimestamp() - began) * 1000.0 / Stopwatch.Frequency;

            SpentMs += ms;

            if (ms > WorstMs)
            {
                WorstMs = ms;
            }
        }
    }

    /// <summary>A tick slower than this says where its time went, once per offender per five minutes.</summary>
    public static double SlowMs { get; set; } = 100.0;

    /// <summary>Slow ticks seen, and the worst single bot turn's cost and owner.</summary>
    public static long Slow { get; private set; }

    private static long _slowSaidTick;

    private static double Since(long mark) => (Stopwatch.GetTimestamp() - mark) * 1000.0 / Stopwatch.Frequency;

    private static void Look(long began)
    {

        var bots = BotPopulation.Bots;
        var now = Core.TickCount;

        // <b>Timed by segment, so a slow tick can say where it went.</b> The clock line of 21:56 on 13.09.2026
        // put the worst tick at 468ms with nothing to say which of the nine things below had taken it; the
        // pathfinder's own worst was 150. Each segment is a timestamp read, which is nothing against a tick.
        var mark = Stopwatch.GetTimestamp();
        var worstMs = 0.0;
        string worst = null;

        void Segment(string name)
        {
            var ms = Since(mark);

            if (ms > worstMs)
            {
                worstMs = ms;
                worst = name;
            }

            mark = Stopwatch.GetTimestamp();
        }

        Summarise(now);
        Segment("the summary");

        // <b>The pins, on the population's fast beat rather than on its five-minute summary.</b> They were
        // asked from Summarise, which meant "rewrite every minute" could never mean anything of the sort —
        // the soonest it could fire was five. BotMarkers keeps its own throttle, so this costs a subtraction
        // per tick and delivers the interval it actually names.
        BotMarkers.Tick();
        Segment("the pins");

        // The slow souring between guilds whose halls stand near each other. It keeps its own throttle for
        // the same reason the pins do, so this is a subtraction a tick and fires at the interval it names.
        BotRegard.Drift();
        Segment("the drift of opinions");

        // Who is standing on the ground they claimed, and whose claims have run their time. See BotClaim.
        BotClaim.Look();
        Segment("the claims");

        // Which way each guild charges strangers for hunting its land, and who is hunting it untold. Its own throttle.
        // See BotToll.
        BotToll.Look();
        Segment("the tolls");

        // Wars whose clock has run out, and enemies standing on anybody's ground. Both keep their own
        // throttle. See BotWar and BotFeud.Watch.
        BotWar.Beat();
        BotFeud.Watch();
        Segment("the wars");

        // Whether anybody has had enough of a guild that is getting nowhere. Its own throttle, and one
        // member a minute at most. See BotGuilds.Review.
        BotGuilds.Review();

        // And the other direction: one bot without a guild joins one or founds its own. See BotGuilds.Gather.
        BotGuilds.Gather();
        Segment("the guild reviews");

        // And the town's shelves, which otherwise can only be refilled by somebody buying from them — and
        // nobody is ever sent to a shelf that has sold out. Its own throttle, and the engine's own hour
        // inside that. See BotShops.Keep.
        BotShops.Keep();
        Segment("the shops");

        // The ground sweep, a slice a tick on the population's clock rather than whole on one miner's turn (01.10.2026, build
        // 382). See BotGround.Sweep: 596 and 701ms on cold ground at boot, 116–143ms on ground first seen, on one turn.
        BotGround.Sweep();
        Segment("the ground sweep");

        // The cries that turned dire since the last look, answered: the best placed of ours near each are sent now. See
        // BotCry.Dispatch (06.10.2026).
        BotCry.Dispatch();
        Segment("the cries");

        // And the companies asked to answer since the last look: an enemy just taken on, a member just become a patient. See
        // BotSquads.Hurry (06.10.2026, react16).
        BotSquads.Answer();
        Segment("the companies' answers");

        var turnWorstMs = 0.0;
        BotMobile turnWorst = null;

        // The costliest turn's own figures, copied the moment it becomes the costliest: the next turn overwrites them.
        var turnWorstDecideMs = 0.0;
        var turnWorstAuctionMs = 0.0;
        var turnWorstWalkMs = 0.0;
        string turnWorstProposer = null;
        var turnWorstProposerMs = 0.0;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot == null || bot.Deleted)
            {
                continue;
            }

            if (bot.Scheduled && now - bot.DueTick < 0)
            {
                // <b>Not its turn, but something has happened to it that will not wait for one (06.10.2026, react14).</b> It
                // decides now and steps when it is due. See BotMobile.Hurry.
                if (bot.Urgent)
                {
                    bot.Urgent = false;
                    Hurried++;

                    BotWill.BeginTurn();

                    try
                    {
                        bot.React();
                    }
                    catch (Exception e)
                    {
                        Faults++;

                        logger.Error(e, "{Name} threw deciding in a hurry; the rest of the population carries on", bot.Name);
                    }
                }

                continue;
            }

            bot.Scheduled = true;

            // Its turn decides anyway.
            bot.Urgent = false;

            // Whether this bot is getting anywhere at all. See BotStall: standing still is this shard's most
            // expensive defect and the only one that never wrote a line of its own.
            BotStall.Look(bot);

            // Each bot's own pace rather than the population's. One that has run itself out of breath walks,
            // and a walk is four hundred milliseconds against a run's two hundred; scheduling it as though it
            // were still running asks it to move twice as often as the engine will allow.
            // Asked of the body rather than of the flag alone: a bot on a horse is allowed twice the pace
            // and was being given a footman's. See BotWalk.StepDelayMs.
            bot.DueTick = now + BotWalk.StepDelayMs(bot, bot.Running);

            // A marching leader with a member well behind it goes at a walk (half a walk until build 377). See BotSquad.Lagging.
            if (bot.Squad is { } marching && ReferenceEquals(marching.Leader, bot) && marching.Lagging())
            {
                bot.DueTick = now + BotWalk.StepDelayMs(bot, false) * Math.Max(1, BotSquad.PacedSteps);
            }

            Turns++;

            var turnMark = Stopwatch.GetTimestamp();

            // So a turn that decides nothing — a revival — does not read the last bot's figures.
            BotWill.BeginTurn();
            BotWalk.BeginTurn();

            try
            {
                if (bot.Fallen)
                {
                    BotPopulation.Revive(bot);

                    continue;
                }

                bot.Beat();
            }
            catch (Exception e)
            {
                // Loud and contained: one bot's mistake must not take the population's clock with it, and it
                // must not be quiet either. A silent exception here stops every bot, which reads as a brain
                // that has stopped deciding.
                Faults++;

                logger.Error(e, "{Name} threw on its turn; the rest of the population carries on", bot.Name);
            }
            finally
            {
                var turnMs = Since(turnMark);

                if (turnMs > turnWorstMs)
                {
                    turnWorstMs = turnMs;
                    turnWorst = bot;
                    turnWorstDecideMs = BotWill.TurnMs;
                    turnWorstAuctionMs = BotWill.TurnAuctionMs;
                    turnWorstWalkMs = BotWalk.TurnMs;
                    turnWorstProposer = BotWill.TurnProposer;
                    turnWorstProposerMs = BotWill.TurnProposerMs;
                }
            }
        }

        Segment("the bots' turns");

        var total = (Stopwatch.GetTimestamp() - began) * 1000.0 / Stopwatch.Frequency;

        if (total < SlowMs)
        {
            return;
        }

        Slow++;

        // Said once per five minutes, not once per slow tick: a slow save is one line, not a hundred.
        if (_slowSaidTick != 0 && now - (_slowSaidTick + SummaryMs) < 0)
        {
            return;
        }

        _slowSaidTick = now;

        // <b>And where inside that turn (01.10.2026, build 382).</b> "Quillathe's at 235ms on mine" named the bot and the work
        // and nothing else, 80 times in 35 minutes on build 381; the deciding half is now split into the work's own Advance, the
        // auction, and the one proposer that cost the most of it, so the next reader can go to a file. See BotWill.TurnMs.
        logger.Information(
            "A slow tick: {Total:F0}ms, {WorstMs:F0}ms of it in {Worst}; the costliest turn was {Bot}'s at {TurnMs:F0}ms on {Deed} (deciding {Decide:F0}ms, of which the auction {Auction:F0}ms and its costliest proposer {Proposer} {ProposerMs:F0}ms; walking {Walk:F0}ms); {Slow} such ticks so far",
            total,
            worstMs,
            worst ?? "nothing in particular",
            turnWorst?.Name ?? "nobody",
            turnWorstMs,
            turnWorst?.Resolve?.Deed?.Kind ?? "nothing",
            turnWorstDecideMs,
            turnWorstAuctionMs,
            turnWorstProposer ?? "none",
            turnWorstProposerMs,
            turnWorstWalkMs,
            Slow
        );
    }

    public static string Describe() =>
        $"{Ticks} looks, {Turns} turns handed out, {Faults} faults; every {IntervalMs}ms, a turn each every {BotWalk.StepDelayMs(BotMobile.Runs)}ms; "
        + $"{SpentMs:F0}ms of the loop in all ({(Ticks > 0 ? SpentMs / Ticks : 0.0):F2}ms a look, worst {WorstMs:F1}ms), of which deciding {BotWill.SpentMs:F0}ms over {BotWill.Decisions} turns and walking {BotWalk.SpentMs:F0}ms; {Hurried} of those decisions were taken between turns, for an event that would not wait";

    private sealed class BeatTimer : Timer
    {
        public BeatTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => Tick();
    }
}
