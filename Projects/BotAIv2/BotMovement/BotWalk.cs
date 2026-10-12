using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Something standing in the way that can be asked to move. Implemented by the bot.
///
/// The measurement that made this necessary: (1371, 1477, 10) is the gate to the Britain graveyard, and
/// two bots parked on it accounted for seventy-seven refused steps in two minutes — twice, on two
/// different days, with two different bots. A wandering monster moves on by itself; a bot at an auction or
/// going through a corpse does not, and if it happens to be in a doorway then nobody gets through the
/// doorway. The tile is the problem, not the bot, so the answer belongs to whoever is standing on it.
/// </summary>
public interface IBotAside
{
    /// <summary>
    /// Take a step, if there is anything else to be doing. Returns whether the tile was actually freed.
    /// </summary>
    bool StepAsideFor(Mobile asker);
}

/// <summary>What one attempt at moving did.</summary>
public enum BotWalkResult
{
    /// <summary>No journey. Nothing to do.</summary>
    Idle,

    /// <summary>Standing on, or beside, the destination. See <see cref="BotArrival"/>.</summary>
    Arrived,

    /// <summary>A tile of the plan was walked.</summary>
    Stepped,

    /// <summary>A shut door was in the way and has been opened. Next tick walks through it.</summary>
    OpenedDoor,

    /// <summary>Somebody was on the next tile. They have been asked to move and the tile is off the next plan.</summary>
    WentRound,

    /// <summary>The plan would not walk, but a step was possible, so a step was taken.</summary>
    Improvised,

    /// <summary>Mid-spell. The engine forbids movement, and this is not a wall.</summary>
    Casting,

    /// <summary>Standing where it is on purpose for a moment, the plan kept: a leader holding its column. See <see cref="BotJourney.Hold"/>.</summary>
    Held,

    /// <summary>The step was refused and nothing helped. Not fatal — the next tick tries again.</summary>
    Blocked,

    /// <summary>There is no way there. Proven, not guessed. The journey is over.</summary>
    Refused,

    /// <summary>Genuinely getting nowhere for long enough to admit it. The journey is over.</summary>
    GaveUp,

    /// <summary>
    /// The same, and told apart from it on purpose: a hundred attempts at stepping without the bot moving a
    /// single tile.
    ///
    /// <para>
    /// <b>This is a fact about the tile the bot is standing on, never about where it was going.</b> Told
    /// apart because <see cref="BotWill"/> writes an unreached ending into <see cref="BotRefused"/>, against
    /// the destination — and on 08.09.2026 that put 1242 refusals on ground that had done nothing wrong and
    /// steered 39254 choices away from it, of which arrival later disproved five per cent. Every one of them
    /// was a bot whose mount had run out of steps and which could not have walked anywhere at all, to any
    /// destination on the island. See BotJourney.Stalled.
    /// </para>
    /// </summary>
    Stalled
}

/// <summary>
/// The moment a step is actually taken — where everything the planner deliberately ignores gets handled.
///
/// <para>
/// The planner reasons about static ground only: land, statics, houses, boats. Creatures, dropped items
/// and shut doors are not in it, because they move, and a planner that treats a skeleton in a doorway as a
/// wall teaches itself that the doorway is one. This is where that debt is paid, once per step, with the
/// engine as the final authority.
/// </para>
///
/// <para>
/// <b>The last line of defence lives here, and it is the one place in the whole design that trusts the
/// engine over the plan.</b> If the plan will not walk and some step is nonetheless possible, take it. The
/// engine is the only authority on whether a step is legal right now, and a bot that can get somewhere is
/// worth more than a bot that is correct about being unable to get where it meant to go.
/// </para>
/// </summary>
public static class BotWalk
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWalk));

    /// <summary>
    /// A walking step, matching the engine's own <c>movement.delay.walkFoot</c> default. Bots have no
    /// <c>NetState</c>, so nothing throttles them, and this is the only thing keeping them from
    /// teleporting along their route at tick rate.
    /// </summary>
    public const int WalkStepMs = 400;

    /// <summary>A running step, matching <c>movement.delay.runFoot</c>.</summary>
    public const int RunStepMs = 200;

    /// <summary>A walking step on a mount, matching <c>movement.delay.walkMount</c>.</summary>
    public const int WalkMountStepMs = 200;

    /// <summary>A running step on a mount, matching <c>movement.delay.runMount</c>.</summary>
    public const int RunMountStepMs = 100;

    /// <summary>How far away a door can be and still be the thing blocking this step.</summary>
    private const int DoorReach = 3;

    /// <summary>
    /// Attempts against the same occupied tile before it is planned around rather than waited out.
    /// </summary>
    private const int PatienceWithOccupants = 2;

    /// <summary>The vertical space a standing person occupies — two bodies further apart are on two floors.</summary>
    private const int PersonHeight = BotArrival.PersonHeight;

    /// <summary>Scratch, reused: a plan that allocates is a plan that costs more in collection than in work.</summary>
    private static readonly List<Point3D> _path = [];

    /// <summary>
    /// Whether the population may move at all. <b>False until the movement module starts</b>, so switching
    /// the module off in <c>modernuo.json</c> freezes every bot where it stands while everything else goes
    /// on running.
    ///
    /// That is the diagnostic the first version never had. Half its investigations were the same question —
    /// is this navigation, or is navigation covering for something else? — and the answer took hours of
    /// watching a live shard. Four of the things it eventually found under the navigation layer were not
    /// navigation at all.
    /// </summary>
    public static bool Walking { get; internal set; }

    public static long Steps { get; private set; }

    public static long Refusals { get; private set; }

    public static long Doors { get; private set; }

    public static long Detours { get; private set; }

    public static long Improvised { get; private set; }

    public static long GaveUp { get; private set; }

    /// <summary>Errands ended because the destination itself was proved no good, rather than the road to it.</summary>
    public static long Dropped { get; private set; }

    /// <summary>Loop time spent stepping and planning, in milliseconds. Timed by the bot's turn; see BotMobile.Beat.</summary>
    public static double SpentMs { get; private set; }

    public static void Spent(long ticks)
    {
        TurnMs = ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        SpentMs += TurnMs;
    }

    /// <summary>The last turn's walking, in milliseconds, for the slow-tick line's costliest turn. See BotWill.TurnMs.</summary>
    public static double TurnMs { get; private set; }

    /// <summary>Clears the last turn's figure. Called by BotBeat before each turn.</summary>
    public static void BeginTurn() => TurnMs = 0.0;

    /// <summary>Searches given the stranded ceiling to find a way round, after the far side proved to be the world.</summary>
    public static long Escalated { get; private set; }

    /// <summary>Of those, the ones that found a way or got closer.</summary>
    public static long EscalationFound { get; private set; }

    /// <summary>And the ones that did not, which ended the errand there and then instead of nine searches later.</summary>
    public static long EscalationFailed { get; private set; }

    /// <summary>Plans drawn to a leg of the chart's route rather than straight at the target. See <see cref="BotChart"/>.</summary>
    public static long Legs { get; private set; }

    /// <summary>Legs that could not be walked: the gate shunned on the chart and the route drawn again.</summary>
    public static long LegsFailed { get; private set; }

    /// <summary>
    /// Searches by what they were for, and how many of each reached their goal.
    ///
    /// <para>
    /// One share for all searches reads a chase and a flight as failures of the roads. A chase is a search
    /// towards something that moves, and a flight is a search away from something with no goal at all but
    /// "further"; neither says anything about whether a bot can get from one fixed place to another, which
    /// is the question the 99 % is asked about. Three shares, so the question can be answered about the
    /// searches it is a question about.
    /// </para>
    /// </summary>
    public static long FixedSearches { get; private set; }

    public static long FixedReached { get; private set; }

    public static long ChaseSearches { get; private set; }

    public static long ChaseReached { get; private set; }

    public static long FlightSearches { get; private set; }

    public static long FlightReached { get; private set; }

    public static long StationSearches { get; private set; }

    public static long StationReached { get; private set; }

    /// <summary>
    /// <b>The walkers' searches, all four purposes, and how many arrived (01.10.2026, build 386).</b> The headline share on the
    /// dashboard read <c>BotPath.Reached / BotPath.Searches</c>: a search for a tile one off that the ground refuses — a creature
    /// on a rock, a station on a wall-top — is Partial there, though the walker has read beside as arriving since 29.09 (see
    /// <see cref="Noted"/>). Thirty minutes after the wipe of 01.10 (build 385) that put the headline at 92.0 % while every
    /// purpose stood at 99.3–99.9 %: 3391 of the 3396 partial searches were "one tile short taken as arriving". The headline is
    /// now the walkers' own count; the search engine's raw share is kept beside it.
    /// </summary>
    public static long WalkerSearches => FixedSearches + ChaseSearches + FlightSearches + StationSearches;

    public static long WalkerReached => FixedReached + ChaseReached + FlightReached + StationReached;

    /// <summary>Plans the navigation graph said had no way on the ground (dropped only when <see cref="TrustNoWay"/>).</summary>
    public static long NoWay { get; private set; }

    /// <summary>Whether the graph's "no way" ends the errand at once, or is only counted while the straight plan tries.</summary>
    public static bool TrustNoWay { get; set; }

    /// <summary>Searches given the whole ceiling, by the errand that asked. The bill, itemised.</summary>
    private static readonly Dictionary<string, long> _burned = [];

    /// <summary>Errands ended as hopeless or as having no way round, by kind.</summary>
    private static readonly Dictionary<string, long> _lost = [];

    private static void Count(Dictionary<string, long> book, string reason)
    {
        reason ??= "?";
        book[reason] = book.GetValueOrDefault(reason) + 1;
    }

    /// <summary>The five biggest entries of a book, for the summary. Cold path.</summary>
    private static string Top(Dictionary<string, long> book)
    {
        if (book.Count == 0)
        {
            return "nobody";
        }

        var rows = new List<KeyValuePair<string, long>>(book);

        rows.Sort(static (a, b) => b.Value.CompareTo(a.Value));

        using var say = Server.Text.ValueStringBuilder.Create(128);

        for (var i = 0; i < rows.Count && i < 5; i++)
        {
            if (i > 0)
            {
                say.Append(", ");
            }

            say.Append($"{rows[i].Key} {rows[i].Value}");
        }

        return say.ToString();
    }

    public static void Reset()
    {
        Steps = 0;
        Refusals = 0;
        Doors = 0;
        Detours = 0;
        Improvised = 0;
        GaveUp = 0;
        Dropped = 0;
        Boxed = 0;
        Knots = 0;
        Escalated = 0;
        Stationed = 0;
        EscalationFound = 0;
        EscalationFailed = 0;
        Legs = 0;
        LegsFailed = 0;
        LegEnded = 0;
        NoWay = 0;
        FixedSearches = 0;
        FixedReached = 0;
        ChaseSearches = 0;
        ChaseReached = 0;
        FlightSearches = 0;
        FlightReached = 0;
        StationSearches = 0;
        StationReached = 0;
        ChasesGivenUp = 0;
        ChasesReached = 0;
        BotJourney.ForgetKeptUp();
        ThrashMoved = 0;
        Holds = 0;
        _chase.Clear();
        _burned.Clear();
        _lost.Clear();
    }

    /// <summary>Plans stopped because the leg had already dropped the errand. See <see cref="Plan"/>.</summary>
    public static long LegEnded { get; private set; }

    public static string Describe() =>
        $"{Steps} steps taken, {Refusals} refused by the engine, {Doors} doors opened, {Detours} tiles gone round, {Improvised} improvised, {GaveUp} journeys given up, {Dropped} destinations dropped as no good, {Boxed} steps where the engine refused all eight directions, {Besides} plans one tile short taken as arriving, {Thrashed} walks ended for planning one place more than {ThrashPlans} times in {ThrashMs / 1000}s ({ThrashRefused} of the places written refused; {ThrashMoved} counts started again for a walker that had come {ThrashTiles} tiles on), {Holds} turns a walk was held where it stood by its work with the plan kept,{ChasesGivenUp} chases ended for planning more than {ChasePlans} roads after one target with none reaching it ({ChasesReached} counts cleared by a plan that reached), {BotJourney.KeptUp} plans after somebody about its own business that reached it and started the closing count again ({BotJourney.KeptOn} walks the old count would have given up went on), {Knots} stepped aside from somebody who would not; the whole ceiling was burned by: {Top(_burned)} ({Stationed} station searches held to {StationCeilingMs:F0}ms instead); {Escalated} searches at the stranded ceiling for a way round, {EscalationFound} found one and {EscalationFailed} ended the errand instead of nine more searches; {Legs} plans drawn to a leg of a route and {LegsFailed} legs that could not be walked ({LegEnded} plans stopped where the leg had already dropped the errand); routes drawn: {BotJourney.Routed} over the navigation graph, {BotJourney.Gated} to a gate into another land, {BotJourney.Charted} over the old chart where the graph was not drawn yet, {BotJourney.NoWay} with no way on the ground ({NoWay} plans the graph said had none{(TrustNoWay ? ", dropped" : ", planned straight instead")}), {BotJourney.MapRefused} never asked of the planner for no open ground at the target on the reach map; {BotGates.Unwedged} bodies standing where nothing stands set on the nearest floor ({BotGates.Unwedgeable} left where they were for want of one); errands lost as hopeless or without a way round, by kind: {Top(_lost)}";

    /// <summary>How long the caller should wait before asking again.</summary>
    public static int StepDelayMs(bool run) => run ? RunStepMs : WalkStepMs;

    /// <summary>
    /// The same, for a body that may be on a horse.
    ///
    /// <para>
    /// <b>Riding looked like hanging because the rider was being stepped at a footman's pace.</b> The engine
    /// has four delays and this file knew two of them: <c>movement.delay.walkMount</c> is 200 against a foot
    /// walk's 400, and <c>movement.delay.runMount</c> is 100 against a foot run's 200 — see
    /// <c>Server.Movement.CalcMoves</c>, which is where <c>Mobile.ComputeMovementSpeed</c> reads them. So a
    /// mounted bot moved at half the speed the engine allows it, while the client drew a horse animating at
    /// the speed a horse animates: a gallop, then a wait, then a gallop. Nothing was stuck; the cadence was
    /// simply the wrong one, and the stutter is what that looks like from outside.
    /// </para>
    /// </summary>
    public static int StepDelayMs(Mobile bot, bool run) =>
        bot?.Mounted == true
            ? run ? RunMountStepMs : WalkMountStepMs
            : run ? RunStepMs : WalkStepMs;

    /// <summary>
    /// One attempt at getting closer. Plans if it must, steps if it can, and says what happened.
    ///
    /// The caller owns the clock: this does one thing and returns, and the tick decides when to come back
    /// — <see cref="StepDelayMs"/>.
    /// </summary>
    public static BotWalkResult Advance(Mobile bot, BotJourney journey, bool run)
    {
        if (!Walking || bot == null || journey == null || !journey.Active)
        {
            return BotWalkResult.Idle;
        }

        var map = bot.Map;

        if (map == null || map == Map.Internal || !bot.Alive)
        {
            return BotWalkResult.Idle;
        }

        // Errands whose reason has evaporated go first, and this is how the road resumes by itself: the
        // interruption that was "kill that thing" ends when the thing is dead, and what it was covering is
        // already on top by the time anything else is decided.
        if (journey.Prune() > 0 && !journey.Active)
        {
            return BotWalkResult.Idle;
        }

        journey.Catch(bot.Location);

        if (journey.Arrived(bot.Location))
        {
            return BotWalkResult.Arrived;
        }

        // <b>Turned back at the guard line</b> (07.10.2026, steward20): a bot of a guild its steward barred from a town does not walk
        // into it from outside. Stalled, not Refused: the bar is a fact about the bot's guild, never about the ground, and a refusal
        // would be written against the place for every bot. One comparison when nobody is barred. See BotStewardship.TurnsBack.
        if (BotStewardship.TurnsBack(bot, journey.Target))
        {
            journey.Complete();

            return BotWalkResult.Stalled;
        }

        // A walk its work is holding where it stands (a leader for its column): no step, no attempt counted against the stall
        // watch, and the plan and route kept for the turn the hold lapses. See BotJourney.Hold (01.10.2026, build next).
        if (journey.Held)
        {
            Holds++;

            return BotWalkResult.Held;
        }

        // Before anything else, because everything below assumes the bot is trying: a bot that has made no
        // progress along its plan for long enough has to be allowed to admit it, or it is lost until the
        // shard restarts.
        if (journey.Stalled())
        {
            // Whose errand is ending matters to everybody upstairs. See Ended.
            var side = journey.Current?.Interruption == true;

            // The load and the stamina are in the line because "made no progress" has two completely
            // different causes and they are indistinguishable without them: ground it cannot cross, or a pack
            // it cannot carry. Past the engine's overweight ceiling every step costs five stamina and at zero
            // the step is refused outright — a bot in that state cannot walk to the bank that would cure it.
            logger.Information(
                "{Name} made no progress towards {Where} and has given up ({Reason}); {Left} errands left, carrying {Load} of {Ceiling} stones, {Stam} stamina",
                bot.Name,
                journey.Target,
                journey.Reason,
                journey.Queued - 1,
                BotLadder.Load(bot),
                BotLadder.Ceiling(bot),
                bot.Stam
            );

            journey.Complete();
            GaveUp++;

            return side ? BotWalkResult.Blocked : BotWalkResult.Stalled;
        }

        // Mid-cast, before anything is counted.
        //
        // <b>The first version could not tell a spell from a wall, and this is the last corner of that.</b>
        // v2 already knew not to redraw the route over a cast — the refusal reports itself as
        // <see cref="BotWalkResult.Casting"/> — but the attempt was tallied on the way there, and the stall
        // measure counts attempts. So a caster standing still to finish a spell spent its patience on doing
        // exactly what it was supposed to be doing: a hundred attempts is twenty seconds, and the journey gave
        // up in the middle of the fight. One warrior-mage took nine hunts that way and finished none of them.
        //
        // The engine will not move a caster at all, so there is nothing here to attempt and nothing to learn
        // from failing to.
        if (bot.Spell != null)
        {
            return BotWalkResult.Casting;
        }

        // Counted here, before anything can succeed or fail: the stall measure is attempts at stepping, not
        // seconds, so that a bot held up by a fight pays nothing for the delay.
        journey.Attempted();

        var planning = journey.Current?.Interruption == true;

        var planned = journey.NeedsPlan(bot.Location);

        if (planned && !Plan(bot, journey, map))
        {
            return planning ? BotWalkResult.Blocked : BotWalkResult.Refused;
        }

        // <b>A place in the line the search came back short of is a refused one, said when the plan is drawn (30.09.2026,
        // build 351).</b> 347 asked it of the road when the company next handed out places, and by then the short walk was
        // usually over and the road empty: on 350 the rest fired 4 times against 261 partial station searches — Ingrun 74,
        // Harbek 67, Aerith 52 at the mountain by (1040, 1310), Dagrun 48 at (2700, 530) — and the journeys to a place in
        // the line read 81 %. The member walks its short plan out; the company leaves it without a new place for StationRestMs.
        if (planned && journey.Partial && journey.Reason is "station")
        {
            BotSquad.StationRefused(bot);
            ShortStations++;

            // <b>The first short plan is walked; a second is the same wall again (30.09.2026, build 353).</b> On 351 the rest above
            // was stamped 734 times in its second quarter hour and withheld 60 places: the company was not handing out places
            // again — the walker was re-planning the one it had, from the end of each short plan to a new short plan, and the
            // journeys to a place in the line fell from 99.5 % to 78 %. The first is walked, as near as the ground allows; at
            // the second the place is let go and the member stands where it got to, resting from places as above.
            if (journey.Plans >= 2)
            {
                journey.Complete();
                StationsLetGo++;

                return BotWalkResult.Blocked;
            }
        }

        // Search after search with nowhere at all to walk. Not a refusal — that is provable and this is not
        // — but continuing to ask is the fence-hugging this subsystem exists to end, in a new form.
        if (journey.Hopeless)
        {
            var side = journey.Current?.Interruption == true;

            Count(_lost, journey.Reason);

            logger.Information(
                "{Name} could not get one tile closer to {Where} in {Plans} plans and has dropped it ({Reason})",
                bot.Name,
                journey.Target,
                journey.Plans,
                journey.Reason
            );

            journey.Complete();
            GaveUp++;

            return Ended(side);
        }

        if (!journey.TryNextTile(out var next))
        {
            // Planned, and there is nothing to walk: the search could not get one tile closer. Not a
            // refusal — that is a separate, provable thing — so try the world directly before concluding.
            return Improvise(bot, journey, next: bot.Location, run);
        }

        var before = bot.Location;
        var direction = bot.GetDirectionTo(next, run);

        // Set first, then move. The engine treats a Move whose direction differs from the mobile's own as a
        // turn and goes nowhere, so without this every other tick is spent pivoting.
        bot.Direction = direction;

        if (bot.Move(direction) && bot.Location != before)
        {
            Steps++;

            journey.Stepped(bot.Location);

            // The bot has just done something the reach ledger may believe impossible. If the two tiles are
            // filed under different sealed pockets, the ledger is out of date — somebody built a door, or a
            // wall came down — and the world has just corrected it.
            BotReach.Contradict(map, before, bot.Location);

            // And the reach map is told of it: a step from open ground onto ground it calls anything else is the map wrong
            // there, counted and drawn again (01.10.2026). See BotReachMap.Stepped.
            BotReachMap.Stepped(bot, before, bot.Location);

            return journey.Arrived(bot.Location) ? BotWalkResult.Arrived : BotWalkResult.Stepped;
        }

        Refusals++;

        return Refused(bot, journey, map, next, run);
    }

    /// <summary>
    /// How to report an errand that has just been given up: as the undertaking's failure, or as nothing much.
    ///
    /// <para>
    /// <b>Only the bot's own errand belongs to the undertaking.</b> An interruption is something that happened
    /// <em>to</em> the bot — it was hit, and the road went underneath while it dealt with that — so giving up
    /// on one says nothing whatever about the work. Reporting it as a refusal made the decision layer fail the
    /// undertaking instead: a caster shooting from across a river, which cannot be walked to, was quietly
    /// cancelling whatever the bot had been doing, over and over. A healer lost the patient it was mending;
    /// miners lost the ore they were carrying.
    /// </para>
    ///
    /// <para>
    /// <see cref="BotWalkResult.Blocked"/> is the honest answer for that case, and the decision layer already
    /// ignores it: nothing is wrong with the work, the bot simply could not get at something. The road it was
    /// on is still underneath, exactly as the queue intends.
    /// </para>
    /// </summary>
    private static BotWalkResult Ended(bool interruption) =>
        interruption ? BotWalkResult.Blocked : BotWalkResult.GaveUp;

    /// <summary>
    /// How many plans in a row may come back short of the destination, without the destination ever getting
    /// nearer, before the far side of it is asked about.
    ///
    /// <para>
    /// Two, and the number is a discriminator rather than a patience setting. A bot walking a long road
    /// legitimately gets a short plan every time — but each one starts further along, so its best distance
    /// keeps improving and this never reaches two. What does reach two is a bot whose road ends at the
    /// same place however often it is redrawn, which is the signature of something in the way rather than
    /// something far off. Costing the far-side look at that signature is what keeps it off the hot path.
    /// </para>
    /// </summary>
    public static int PlansBeforeAskingTheFarSide { get; set; } = 2;

    /// <summary>The most a station search may cost once it has stopped closing, in milliseconds. See Plan.</summary>
    public static double StationCeilingMs { get; set; } = 15.0;

    /// <summary>Station searches held to <see cref="StationCeilingMs"/> instead of the whole ceiling.</summary>
    public static long Stationed { get; private set; }

    /// <summary>Draws a plan. Returns false only when the destination is provably unreachable.</summary>
    private static bool Plan(Mobile bot, BotJourney journey, Map map)
    {
        // <b>A far target is walked a leg at a time, over the chart.</b> Patrick's order of 26.09.2026 (BotChart): a
        // search from where the bot stands, with a ceiling, cannot see a way round longer than its ceiling, and every
        // correction below this line — the escalation, the far side, the stranded ceiling — is a way of deciding that it
        // did not. The chart knows the way round; each leg is a short walk the search finds at once.
        // <b>Standing on a public moongate the route was drawn through: through it (29.09.2026).</b> The engine asks a player
        // which city by a gump; a bot has no client, so it is put through here, and the next leg is drawn from the far side.
        if (journey.Current is { } moonErrand && moonErrand.Gate != Point3D.Zero && bot.X == moonErrand.Gate.X && bot.Y == moonErrand.Gate.Y
            && BotGates.Moon(moonErrand.Gate, moonErrand.GateTo) is { } moon && BotGates.Travel(bot, moon))
        {
            return true;
        }

        // A teleporter at another height than the ground under it does not fire for a body standing on it. See BotGates.Nudge.
        if (journey.Current is { } gateErrand && gateErrand.Gate != Point3D.Zero && bot.X == gateErrand.Gate.X && bot.Y == gateErrand.Gate.Y
            && BotGates.Nudge(bot, gateErrand.Gate))
        {
            return true;
        }

        // A gate that put the walker down above the floor there: set on the floor before anything is planned from it, as the
        // first step would set it (30.09.2026, build 338). See BotGates.Alight.
        if (journey.Current is { } landing && landing.Gate != Point3D.Zero
            && Math.Max(Math.Abs(bot.X - landing.GateTo.X), Math.Abs(bot.Y - landing.GateTo.Y)) <= BotGates.Through)
        {
            BotGates.Alight(bot);
        }

        // <b>The route is priced for the one who will walk it (01.10.2026, build 388).</b> The planners' danger hook knows who is
        // asking only for as long as the draw takes; a draw from anywhere else is priced for the median bot. See BotDanger.For.
        bool legged;
        Point3D leg;

        BotDanger.For(bot);

        // A leg towards a road the appraisal offered though the straight line refused it: counted, for the deaths on such roads.
        BotDanger.Walks(bot, journey.Target);

        try
        {
            legged = journey.Leg(map, bot.Location, out leg);
        }
        finally
        {
            BotDanger.Clear();
        }

        var legErrand = journey.Current;

        if (legged && PlanLeg(bot, journey, map, leg))
        {
            return true;
        }

        // <b>A leg that ended its errand ends the plan (06.10.2026, work15).</b> PlanLeg drops a walk that thrashes and answers
        // false, the same false as "this leg could not be walked, plan straight instead", so the straight plan below went on
        // with whatever was left: on an empty journey a target of (0, 0, 0) and no reason, which the reach map then refused as
        // a second drop in the same beat. 506 of the 529 "reach map has no ground a walk from home reaches" drops on 04.10
        // were that (0, 0, 0), 72 in the first hour of 06.10, each right after "planned 31 roads to one place".
        if (legged && (!journey.Active || !ReferenceEquals(journey.Current, legErrand)))
        {
            LegEnded++;

            return false;
        }

        // <b>No way on the ground, said at once.</b> The navigation graph found none: from its components without searching,
        // or after searching all the ground joined to the bot's. Before the tiers this was found out by twelve searches
        // and a look at the far side, or not at all. Near targets still get the precise search's own look.
        // <b>Counted and not acted on, until the graph is proved right about it.</b> On build 264 it said no way twice in
        // its first ten minutes, and once it was wrong: Ronan's herbs at (1371, 1460, 10) were twelve tiles of plan from
        // (1380, 1470, 10) for the precise search. The straight plan is tried as before; <see cref="TrustNoWay"/> drops.
        if (journey.NoWayThere && Math.Max(Math.Abs(bot.X - journey.Target.X), Math.Abs(bot.Y - journey.Target.Y)) > BotJourney.DirectTiles)
        {
            NoWay++;

            if (TrustNoWay)
            {
                Count(_lost, journey.Reason);

                return Drop(bot, journey, "the ground has no way there");
            }

            // Once an errand: the straight plan is drawn again every few beats.
            if (journey.Current is { NoWaySaid: false } errand)
            {
                errand.NoWaySaid = true;

                logger.Information(
                    "{Name}: the navigation graph says there is no way from {From} to {Where} ({Reason}); planned straight instead",
                    bot.Name,
                    bot.Location,
                    journey.Target,
                    journey.Reason
                );
            }
        }

        // A search is charged by how far it is going, so a chase costs a few milliseconds and an island
        // crossing costs sixty. That is right for almost everything and wrong for one case: a goal twenty
        // tiles off whose road runs four hundred tiles round a lake is a short journey by every measure the
        // planner has. The signal that tells them apart is already here — a journey that has drawn a plan and
        // got no closer — so the cheap search goes first and the whole ceiling is bought only where the cheap
        // one has been shown to fail.
        //
        // <b>One plan, not two, and it was tried at two and measured.</b> A single plan that fails to better
        // the errand's best distance looks like ordinary noise, and raising the bar to two - the same
        // threshold the far side is asked at - did exactly what it was meant to: searches asking for the whole
        // ceiling fell from a third of all of them to under three per cent, and the price of a search from
        // 20.45ms to 8.68. It also cost the population eleven per cent of its work. Jobs finished in minutes
        // five to ten of a run, which is the only number here that is an outcome rather than a cost: 208 and
        // 210 with the bar at one, 187 with it at two. The cheap search was not finding the way and the bots
        // were walking further to get anywhere.
        //
        // So the escalation stays eager, and what pays for it is that it is still bounded by the governor:
        // 181 to 212ms a second of an allowance of five hundred, against 469 before searches were charged by
        // distance at all.
        // <b>And one long look for a way round, after which the answer is taken.</b> Measured over the
        // twelve hours of 13.09.2026: 1,317,804 searches, 94 minutes of the loop's time, 256,744 of them
        // handed the whole ceiling because the journey was not closing - up to twelve sixty-millisecond
        // searches in a row for an errand that ended hopeless, the far side having already said the target
        // was in the world. The nine after the third looked at the ground the first three had. So: once the
        // far side has been asked and answered "the world", the next plan gets the stranded ceiling once,
        // and if that neither finds a way nor gets closer the errand ends there. A starved search is not a
        // verdict and does not count as the one look. The eager first escalation above is kept as measured.
        var escalate = journey.Probed && !journey.Escalated && journey.PlansSinceCloser >= PlansBeforeAskingTheFarSide;

        // <b>The whole ceiling, and not a share of it by distance - that was tried on 13.09.2026 at 22:05 and
        // reverted at 22:20.</b> Sixteen milliseconds for a short journey cut the pathfinder's cost five times
        // and broke every walk to a shopkeeper: the goal is ten tiles off through a wall, the road runs round
        // the shop to its door, a sixteen-millisecond search stopped at the wall, two of those sent the far
        // side to look, the far side found the counter's enclosure and sealed it, and 1,362 restocks in ten
        // minutes ended "no way through to Abira inside a house". The whole ceiling is what finds the door.
        // <b>And a plan drawn round something the bot has seen is owed the whole ceiling too (03.10.2026, sight13).</b> A reach of
        // forty tiles across lies on the straight road, the way round it is forty tiles longer, and a search held to its distance's
        // share stops at the reach's edge and comes back short. See BotSight.
        var ceiling = escalate ? BotPath.StrandedCeilingMs : journey.PlansSinceCloser > 0 || journey.Sighted != null ? BotPath.CeilingMs : 0.0;

        // <b>Except a station, which is not an errand and is not owed the door.</b> A company re-issues every
        // member's place every beat the anchor moves, and in a cave or across a fight most of those places
        // cannot be closed on; each miss above was then handed the whole sixty milliseconds, and the next
        // beat handed it again from a new anchor. 1,534 whole-ceiling station searches in twelve minutes of
        // build 23 on 14.09.2026, 2,326 partials that ended no nearer than they started, 24 ms a look and
        // rising. A place in the line is worth a short look; if that finds nothing the next beat asks again.
        // The shop-door argument above is about errands, which keep their whole ceiling.
        if (ceiling > 0.0 && journey.Reason is "station" or "sweep")
        {
            ceiling = StationCeilingMs;
            Stationed++;
        }
        else if (ceiling > 0.0)
        {
            Count(_burned, journey.Reason);
        }

        var outcome = BotPath.Find(
            map,
            bot.Location,
            journey.Target,
            journey.Arrival,
            _path,
            BotOutlaw.Road(bot, map, journey.Target, journey.Avoid(bot.Location)),
            ceiling
        );

        Noted(bot, journey, outcome);

        // A plan with not one step in it, from a body standing where nothing stands, is the same wedge as a sealed one: the
        // search found the pocket beside the body (BotReach.Ask, build next) and could not take the first step off its tile.
        if (outcome == BotPathOutcome.Partial && _path.Count == 0 && BotGates.Unwedge(bot))
        {
            return true;
        }

        if (Thrashing(bot, journey, out var thrashed))
        {
            return Drop(bot, journey, thrashed, thrash: true);
        }

        // A chase that no plan reaches, ended here rather than eleven minutes on. Not written as a refused place: a somebody
        // is not a somewhere, which is the rule BotWill.Note keeps for the same ending. See ChasePlans.
        if (Chasing(bot, journey, outcome, out var chased))
        {
            return Drop(bot, journey, chased);
        }

        // <b>One tile off and no way onto it is arriving (29.09.2026).</b> The tile is a tree, a wall-top or somebody
        // standing on it; the next beat's Arrived reads beside as reached and the errand ends where it should.
        // A station is two tiles' worth of beside: Lorandil asked the road to a station two tiles off 119 times in twenty
        // minutes of build 281, four milliseconds each; the formation gives a fresh station when the ring turns.
        if (outcome != BotPathOutcome.Reached && Math.Max(Math.Abs(bot.X - journey.Target.X), Math.Abs(bot.Y - journey.Target.Y)) <= BotJourney.BesideTiles(journey.Reason))
        {
            journey.Beside();
            Besides++;

            return true;
        }

        // A statement about the world, and therefore worth acting on at once. The first version spent
        // twenty-five seconds of a bot's life to reach this same conclusion, and then only sometimes.
        if (outcome == BotPathOutcome.Sealed)
        {
            // <b>A body wedged where nothing can stand is set on the nearest floor before anything is given up (01.10.2026, build
            // next).</b> See BotGates.Unwedge: Lorandil at (5145, 1969, 5), a tile the reach map calls blocked, failed every errand
            // "no way from here" for minutes on build 384 while the population's own rescue leaves a bot down a dungeon alone.
            if (!BotReach.LastByMap && BotGates.Unwedge(bot))
            {
                return true;
            }

            // From inside the dungeon block to another land the gates call joined: filed against the pair of lands, so the gates
            // stop saying yes to a way the walker keeps refusing (06.10.2026, work17). See BotGates.WalkRefused.
            if (!BotReach.LastByMap)
            {
                BotGates.WalkRefused(map, bot.Location, journey.Target);
            }

            // Said apart when it was the reach map's answer (01.10.2026): no open ground a walk from home reaches is near it.
            return Drop(bot, journey, BotReach.LastByMap ? "the reach map has no ground a walk from home reaches there" : "there is no way from here");
        }

        journey.Planned(outcome, _path, bot.Location);

        if (escalate && !BotPath.LastStarved)
        {
            journey.Escalated = true;
            Escalated++;

            if (outcome == BotPathOutcome.Partial && journey.PlansSinceCloser > 0)
            {
                EscalationFailed++;
                Count(_lost, journey.Reason);

                return Drop(bot, journey, $"no way round it was found in {BotPath.StrandedCeilingMs:F0}ms");
            }

            EscalationFound++;

            return true;
        }

        // Everything below is the other question, and it is asked from the other end.
        //
        // <b>This is the whole of what a bot standing here can and cannot find out.</b> "How do I get there"
        // has now failed three times running without the destination coming any nearer, and asking it a third
        // time buys nothing: a search begun at the bot has to enumerate the mainland before it may conclude
        // anything, which no clock on any shard will pay for. "Is there anything there to get to" is a
        // different question, it is cheap from the destination's side, and the answer is a fact about the
        // world that every bot on the shard gets to keep.
        if (outcome != BotPathOutcome.Partial || journey.Probed || journey.PlansSinceCloser < PlansBeforeAskingTheFarSide)
        {
            return true;
        }

        var far = BotPath.Enclose(map, journey.Target, journey.Arrival);

        // The population has looked at somebody else's far side too recently. Not an answer, so the errand is
        // not marked as having had one — the next plan asks again.
        if (far == BotEnclosure.Deferred)
        {
            return true;
        }

        journey.Probed = true;

        // No road can end at a tile that will not take a body. Nothing in the walker could see this before:
        // the plan is drawn, the steps are legal, the bot walks them perfectly and simply never arrives.
        if (far == BotEnclosure.NoFooting)
        {
            // Kept for everybody, not only for this walk: the next bot handed this tile would learn it again the same
            // way. See BotFooting.
            BotFooting.Note(map, journey.Target);

            return Drop(bot, journey, "there is nowhere there to stand");
        }

        // A pocket has been filed and it belongs to everybody now. Whether it refuses *this* journey is the
        // ledger's own question, because the bot may be standing inside the pocket itself.
        if (far == BotEnclosure.Enclosed
            && BotReach.Ask(map, bot.Location, journey.Target, journey.Arrival, tally: false) == BotReachVerdict.Sealed)
        {
            return Drop(bot, journey, "it is shut in and this bot is outside it");
        }

        return true;
    }

    /// <summary>
    /// A plan to one leg of the chart's route. A leg that proves shut, or that plans twice without getting any nearer
    /// along the route, is not a verdict on the target: its gate is shunned and the route drawn again (BotJourney.LegFailed),
    /// and after <c>BotJourney.MostReroutes</c> of those the errand is walked straight, as it was before the chart.
    /// </summary>
    /// <summary>
    /// What a search was for, and whether it got there — see <see cref="FixedSearches"/>. A search that did not
    /// is an event, with the bot, the purpose and the place, so the page can say who cannot get where. Called
    /// after both searches: the precise one at the target and the one at the next leg of a route.
    /// </summary>
    /// <summary>
    /// <b>A walker planning the same fixed goal over and over is thrashing, whatever its counters say (29.09.2026, build 330).</b>
    /// Odile the Healer, sent out to the woods for herbs on Jhelom's south isle, stepped between (1448, 3907) and (1449,
    /// 3907) and planned 601 roads in forty seconds on 329, every one partial and empty: each step "closer" by the crow put
    /// the no-progress count back to nought, and the rules that give an errand up (MaxPlansWithoutCloser, MaxEmptyPlans)
    /// never reached their numbers. That one bot took the fixed-goal share of the dashboard from 99 to 92 %. More than
    /// <see cref="ThrashPlans"/> plans at one fixed goal inside <see cref="ThrashMs"/> ends the walk. A chase and a place in a
    /// company re-plan by their nature and are not asked.
    /// </summary>
    public static int ThrashPlans { get; set; } = 30;

    public static int ThrashMs { get; set; } = 20000;

    /// <summary>
    /// <b>"Without getting anywhere" is now measured, not assumed (01.10.2026, build next).</b> The guard counted plans, and a
    /// walker can plan a great deal while getting on: a delve's leader marching a column of twelve re-planned on every turn it
    /// resumed from a hold (BotJourney.Hold explains why) and was dropped at (1488, 1375), (1530, 1291) and (1600, 1241) on
    /// build 386 — forty, eighty and a hundred and twenty tiles from its muster — "because it planned 31 roads to one place in
    /// 18s", each drop writing the Orc Caves' room into BotRefused for every hunter to read. Odile on 329 stepped between two
    /// tiles. A walker that has come this many tiles from where the count began is getting somewhere, and the count starts
    /// again from there. Nought keeps the old rule.
    /// </summary>
    public static int ThrashTiles { get; set; } = 12;

    /// <summary>Walks ended for thrashing. See <see cref="ThrashPlans"/>.</summary>
    public static long Thrashed { get; private set; }

    /// <summary>Loop counts started again for a walker that had covered <see cref="ThrashTiles"/> since the count began.</summary>
    public static long ThrashMoved { get; private set; }

    /// <summary>Turns a walk was held where it stood by its work, the plan kept. See <see cref="BotJourney.Hold"/>.</summary>
    public static long Holds { get; private set; }

    /// <summary>Places written into the refusal ledger because the walker looped on them. See <see cref="Drop"/>.</summary>
    public static long ThrashRefused { get; private set; }

    private static readonly Dictionary<Serial, (Point3D Goal, long Since, int Plans, Point3D From)> _thrash = [];

    private static bool Thrashing(Mobile bot, BotJourney journey, out string why)
    {
        why = null;

        if (ThrashPlans <= 0 || journey.Current?.Follow != null || journey.Reason is "station" or "sweep")
        {
            return false;
        }

        var now = Core.TickCount;
        var goal = journey.Target;

        if (!_thrash.TryGetValue(bot.Serial, out var seen) || seen.Goal != goal || now - seen.Since > ThrashMs)
        {
            if (_thrash.Count > 1024)
            {
                _thrash.Clear();
            }

            _thrash[bot.Serial] = (goal, now, 1, bot.Location);

            return false;
        }

        // Getting somewhere: the count starts again from here. See ThrashTiles.
        if (ThrashTiles > 0 && Math.Max(Math.Abs(bot.X - seen.From.X), Math.Abs(bot.Y - seen.From.Y)) >= ThrashTiles)
        {
            ThrashMoved++;
            _thrash[bot.Serial] = (goal, now, 1, bot.Location);

            return false;
        }

        seen.Plans++;
        _thrash[bot.Serial] = seen;

        if (seen.Plans <= ThrashPlans)
        {
            return false;
        }

        _thrash.Remove(bot.Serial);
        Thrashed++;
        why = $"it planned {seen.Plans} roads to one place in {(now - seen.Since) / 1000}s without getting anywhere";

        return true;
    }

    /// <summary>
    /// <b>A chase after something no plan reaches is ended after this many plans, whatever the target does and however often the
    /// order is re-issued (01.10.2026, build 382).</b> The guard above is for a fixed place and leaves a chase alone by design;
    /// the journey's own count (BotJourney.MaxPlansWithoutCloser) belongs to one errand object and starts again with every
    /// order sent afresh, every plan discarded to improvise a step, and — until build 382 — every tile the target itself
    /// wandered nearer. On build 381 Odile planned 1071 roads at Judith in Jhelom over 11.6 minutes and Adela 915 at a sheep,
    /// the two of them nearly the whole of the chase share's fall from ~98 % to 26 %, and the proof of no way came only when
    /// the errand had been through every other exit. Kept here per bot and keyed by the target, so it counts across all of
    /// that: a plan that reaches the target clears it (reachable is reachable, however far it runs), a plan that closes by
    /// more than the target's own drift (BotJourney.FollowSlack) starts it again, and the rest count. Nought turns it off.
    /// </summary>
    public static int ChasePlans { get; set; } = 12;

    /// <summary>A chase after the same target taken up again after this long is a new chase.</summary>
    public static int ChaseMs { get; set; } = 120000;

    /// <summary>Chases ended for planning <see cref="ChasePlans"/> roads after one target without a way to it.</summary>
    public static long ChasesGivenUp { get; private set; }

    /// <summary>Chase records cleared by a plan that reached its target.</summary>
    public static long ChasesReached { get; private set; }

    private static readonly Dictionary<Serial, (Serial Follow, long Since, long Last, int Plans, int Nearest)> _chase = [];

    private static bool Chasing(Mobile bot, BotJourney journey, BotPathOutcome outcome, out string why)
    {
        why = null;

        if (ChasePlans <= 0 || journey.Current?.Follow is not { } follow)
        {
            return false;
        }

        var now = Core.TickCount;
        var target = journey.Target;
        var away = Math.Max(Math.Abs(bot.X - target.X), Math.Abs(bot.Y - target.Y));

        // Reached, or beside it: there is a way, and the count means nothing.
        if (outcome == BotPathOutcome.Reached || away <= BotJourney.BesideTiles(journey.Reason))
        {
            if (_chase.Remove(bot.Serial))
            {
                ChasesReached++;
            }

            return false;
        }

        if (!_chase.TryGetValue(bot.Serial, out var seen) || seen.Follow != follow.Serial || now - seen.Last > ChaseMs)
        {
            if (_chase.Count > 1024)
            {
                _chase.Clear();
            }

            _chase[bot.Serial] = (follow.Serial, now, now, 1, away);

            return false;
        }

        seen.Last = now;

        // Closed by more than the target's own wandering could explain: still a chase, not a wall.
        if (away < seen.Nearest - BotJourney.FollowSlack)
        {
            seen.Nearest = away;
            seen.Plans = 1;
            _chase[bot.Serial] = seen;

            return false;
        }

        seen.Plans++;
        _chase[bot.Serial] = seen;

        if (seen.Plans <= ChasePlans)
        {
            return false;
        }

        _chase.Remove(bot.Serial);
        ChasesGivenUp++;
        why = $"it planned {seen.Plans} roads after {follow.Name} in {(now - seen.Since) / 1000}s and none reached it, the nearest {seen.Nearest} tiles off";

        // Kept for whoever sent the chase, by the creature's serial, so the same walk is not sent again at once. See Unreached.
        if (_unreached.Count > 1024)
        {
            _unreached.Clear();
        }

        _unreached[bot.Serial] = (follow.Serial, now);

        return true;
    }

    /// <summary>The chase record of a bot that is gone. Called with the rest of the population's forgetting.</summary>
    /// <summary>
    /// How long a chase the walker gave up stays given up for the bot that walked it (06.10.2026, work15). Two minutes, the
    /// walker's own <see cref="ChaseMs"/>.
    ///
    /// <para>
    /// <b>The walker ended the chase, and the company sent it again in the next beat.</b> Company 94 fought a skeleton north of
    /// Cove at (2430-2490, 840-880) on 06.10.2026: Morgrim and Bardryn were dropped "planned 13 roads after a skeleton … none
    /// reached it" 100 times in 25 minutes, every one or two seconds, 821 and 538 partial searches (71 % of the shard's), and the
    /// walkers' share fell from 99.63 % to 98.68 % in three minutes. The chase limit worked; nothing that sent the chase could
    /// read that it had: a member's walk to the company's fight is re-issued by BotEnlist every beat it is not in reach.
    /// </para>
    /// </summary>
    public static int UnreachedMs { get; set; } = 120000;

    /// <summary>The last creature each bot's walker gave a chase up on, and when.</summary>
    private static readonly Dictionary<Serial, (Serial Follow, long Tick)> _unreached = [];

    /// <summary>Whether this bot's walker gave up a chase after <paramref name="follow"/> within <see cref="UnreachedMs"/>.</summary>
    public static bool Unreached(Mobile bot, Mobile follow) =>
        bot != null && follow != null && _unreached.TryGetValue(bot.Serial, out var given) && given.Follow == follow.Serial
        && Core.TickCount - given.Tick < UnreachedMs;

    public static void ForgetChase(Mobile bot)
    {
        if (bot != null)
        {
            _chase.Remove(bot.Serial);
        }
    }

    private static void Noted(Mobile bot, BotJourney journey, BotPathOutcome outcome)
    {
        var chase = journey.Current?.Follow != null;
        var flight = !chase && journey.Reason != null && journey.Reason.StartsWith("away from", System.StringComparison.Ordinal);
        var station = !chase && !flight && journey.Reason is "station" or "sweep";

        // Beside is reached: see Plan. A search to a tile one off that the ground refuses is not a road not found.
        var reached = outcome == BotPathOutcome.Reached
                      || Math.Max(Math.Abs(bot.X - journey.Target.X), Math.Abs(bot.Y - journey.Target.Y)) <= BotJourney.BesideTiles(journey.Reason);

        if (chase)
        {
            ChaseSearches++;
            ChaseReached += reached ? 1 : 0;
        }
        else if (flight)
        {
            FlightSearches++;
            FlightReached += reached ? 1 : 0;
        }
        else if (station)
        {
            // A place in a company's line, issued every beat the line moves: not a place the bot chose, and
            // two thirds of all failed searches on build 270 — 200 of 300, 158 of them one tile off.
            StationSearches++;
            StationReached += reached ? 1 : 0;
        }
        else
        {
            FixedSearches++;
            FixedReached += reached ? 1 : 0;
        }

        if (reached)
        {
            return;
        }

        BotEvents.PathFailed(
            bot,
            (bot as BotMobile)?.Resolve?.Deed?.Kind,
            journey.Reason,
            outcome.ToString(),
            bot.Location,
            journey.Target,
            BotPath.LastMs,
            BotPath.LastExpansions,
            journey.PlansSinceCloser,
            chase,
            flight
        );
    }

    private static bool PlanLeg(Mobile bot, BotJourney journey, Map map, Point3D leg)
    {
        Legs++;

        var avoid = BotOutlaw.Road(bot, map, journey.Target, journey.Avoid(bot.Location));

        // A gate's tile is stood on, not approached: the engine teleports on the step onto it. See BotGates.
        var outcome = BotPath.Find(
            map,
            bot.Location,
            leg,
            journey.LegExact ? BotArrival.Exactly : BotArrival.Within(BotJourney.LegReached),
            _path,
            avoid,
            journey.PlansSinceCloser > 0 || avoid.HasReaches ? BotPath.CeilingMs : 0.0
        );

        Noted(bot, journey, outcome);

        if (Thrashing(bot, journey, out var thrashedLeg))
        {
            return Drop(bot, journey, thrashedLeg, thrash: true);
        }

        // A search starved of clock, or kept out of ground on purpose, is no verdict on the gate: BotPath files no pocket
        // from either, and neither is a gate shunned for it.
        var fair = !BotPath.LastStarved && avoid.Empty;

        if (outcome == BotPathOutcome.Sealed)
        {
            // Planned straight at the target this beat instead of leaving an empty plan; the route is drawn again next.
            LegsFailed++;
            journey.LegFailed(map, fair);

            return false;
        }

        journey.Planned(outcome, _path, bot.Location);

        if (outcome == BotPathOutcome.Partial && journey.PlansSinceCloser >= PlansBeforeAskingTheFarSide)
        {
            LegsFailed++;
            journey.LegFailed(map, fair);
        }

        return true;
    }

    /// <summary>
    /// Ends the errand because the destination is provably no good, and says which proof it was.
    ///
    /// Only this errand. Whatever it was covering is still worth doing — a road proved impossible does not
    /// make the market it led to uninteresting, and clearing the queue would throw away the very thing the
    /// queue exists to keep.
    /// </summary>
    private static bool Drop(Mobile bot, BotJourney journey, string why, bool thrash = false)
    {
        // <b>A place the walker looped on is written where the choosers read (30.09.2026, build 354).</b> The loop guard of 330
        // ended the walk and nothing else learned of it: Orsik "has dropped (1136, 2595, 18) because it planned 31 roads to one
        // place in 6s" 85 times in ten minutes on 353, every six seconds, the cook sending it back to the same fire each time
        // — the drop never reached the work's own Bend, which files a fire cold — and the walks the guard ended rose from 3
        // and 9 on 351–352 to 90 (`night-r24-gap.md`). BotRefused is read by the hearth, forge and seam finders and the hunt;
        // written here, the next choice is somewhere else. Not a place in a company's line, which the rest above covers.
        if (thrash && journey.Target != Point3D.Zero && bot.Map != null && journey.Reason is not ("station" or "sweep"))
        {
            BotRefused.Refuse(bot.Map, journey.Target);
            ThrashRefused++;
        }

        logger.Information(
            "{Name} has dropped {Where} because {Why} ({Reason})",
            bot.Name,
            journey.Target,
            why,
            journey.Reason
        );

        // Read before Complete: completing takes the errand off, and Reason is then the next one's (build 338: the rest of
        // 337 never fired, "withheld from a member resting" 0 through three summaries). See BotSquad.StationRefused.
        var stationed = journey.Reason is "station" or "sweep";

        journey.Complete();
        Dropped++;

        if (stationed)
        {
            BotSquad.StationRefused(bot);
        }

        return false;
    }

    /// <summary>Works out why a step was refused, and does whatever that calls for.</summary>
    private static BotWalkResult Refused(Mobile bot, BotJourney journey, Map map, Point3D next, bool run)
    {
        // Mid-spell. The engine will not move a caster, so this refusal says nothing about the ground —
        // and in the first version the walker could not tell a spell from a wall, so it counted the
        // failures, redrew the route twice and abandoned the errand. Mages were being told that their own
        // casting was a dead end.
        if (bot.Spell != null)
        {
            return BotWalkResult.Casting;
        }

        // A door first, and early. Waiting several failures before trying the handle means standing in the
        // street outside a bank looking foolish, and writing the doorway off would teach the population
        // that the inside of every building in the world is unreachable.
        if (OpenDoorTowards(bot, next))
        {
            Doors++;

            return BotWalkResult.OpenedDoor;
        }

        // Something alive. It will move — or can be asked to — so the thing to do is go round it, and never
        // to conclude anything about the ground.
        if (AskAsideAt(bot, map, next, out var freed))
        {
            // Once is not evidence. Most things in the way are already leaving, and redrawing the route for a
            // skeleton walking past spends a search to avoid a tile that will be empty before the plan ends.
            // Twice on the same tile is somebody who is staying — a bot at an auction, going through a
            // corpse, waiting out a cooldown — and that is worth planning around.
            if (journey.NoteBlocked(next) >= PatienceWithOccupants)
            {
                journey.AvoidTile(next);
                journey.Discard();
                Detours++;

                // <b>Asked twice and it did not move, so stop treating the asking as an answer.</b>
                //
                // IBotAside.StepAsideFor says in its own summary that it returns whether the tile was
                // actually freed, and this discarded that and reported WentRound regardless — which is not a
                // refusal, so nothing below here ever ran. Four bots met west of Britain at (1344-1345,
                // 877-879) on 03.09.2026, each standing on another's next tile, each asking, none moving, and
                // all four stood there until the stall watch carried three of them home four minutes later.
                // Their tiles allowed all eight directions and the floor was exactly where each thought it
                // was: nothing whatever was wrong with the ground, and nothing in the walker was willing to
                // take a step that was not the planned one.
                if (!freed)
                {
                    Knots++;

                    return Improvise(bot, journey, next, run);
                }
            }

            return BotWalkResult.WentRound;
        }

        return Improvise(bot, journey, next, run);
    }

    /// <summary>
    /// The plan will not walk. Ask the engine what will.
    ///
    /// <para>
    /// One step off the intended line to either side, then two, and only then the three that face away.
    /// The order is the whole of the reasoning: a wider arc is not going round an obstacle, it is going
    /// somewhere else, so it is tried last and never instead.
    /// </para>
    ///
    /// <para>
    /// <b>It stopped at two, and a bot whose only way out is behind it never tried the way out.</b> Ulla the
    /// WarriorArcher stood on a ledge at (1598, 1774, 32) for six and a half minutes on 03.09.2026 with
    /// nobody within two tiles of it, its own tile allowing steps by the planner's reckoning and every one of
    /// the five this offered refused by the engine. Three of the eight were never asked. Standing still for
    /// six minutes to avoid walking the wrong way is the trade this file exists to refuse: a bot that can get
    /// somewhere is worth more than a bot that is correct about being unable to get where it meant to go, and
    /// the plan drawn from wherever it lands will find the real way round.
    /// </para>
    /// </summary>
    private static BotWalkResult Improvise(Mobile bot, BotJourney journey, Point3D next, bool run)
    {
        var nothingTried = next == bot.Location;

        var heading = nothingTried
            ? (int)(bot.GetDirectionTo(journey.Aim) & Direction.Mask)
            : (int)(bot.GetDirectionTo(next) & Direction.Mask);

        // Straight at it first, but only when nothing has been tried yet.
        //
        // The offsets exist to go round something the plan already failed against, so retrying the failed
        // direction would be wasted. But when the plan had nowhere to walk at all, nothing has been refused
        // — and skipping the straight direction there means never attempting the most obvious move.
        ReadOnlySpan<int> offsets = [0, 1, -1, 2, -2, 3, -3, 4];

        for (var i = nothingTried ? 0 : 1; i < offsets.Length; i++)
        {
            // The run flag is put back on. Stripping it to do the arithmetic is necessary — the flag is a bit
            // above the three that name a compass point — but stepping without it tells every client watching
            // that the bot is walking, so a running bot moved at a run's pace with a walk's gait. That is the
            // odd skating step, and it is only ever visible from a client, which is why it survived so long.
            var direction = (Direction)((heading + offsets[i] + 8) & 0x7) | (run ? Direction.Running : 0);
            var before = bot.Location;

            bot.Direction = direction;

            if (!bot.Move(direction) || bot.Location == before)
            {
                continue;
            }

            Improvised++;

            // An improvised step is the likeliest move in the whole system to prove the reach ledger wrong:
            // it is the bot going somewhere no plan sent it.
            BotReach.Contradict(bot.Map, before, bot.Location);

            // Off the plan now, so the plan is worthless. Cheaper to draw a new one from where the bot
            // actually is than to walk a route that starts two tiles away.
            journey.Discard();

            return BotWalkResult.Improvised;
        }

        Boxed++;

        return BotWalkResult.Blocked;
    }

    /// <summary>
    /// Times the engine refused every one of the eight directions.
    ///
    /// The strongest statement the walker can make about a tile, and the one it could not make before: with
    /// five of eight tried, "blocked" meant "not that way" and this means "not any way". A bot here is not
    /// getting out by walking, whatever any plan says.
    /// </summary>
    public static long Boxed { get; private set; }

    /// <summary>Plans to a place in a company's line that came back short of it, each resting the member from places. See <see cref="Advance"/>.</summary>
    public static long ShortStations { get; private set; }

    /// <summary>Places in the line let go at their second short plan. See <see cref="Advance"/>.</summary>
    public static long StationsLetGo { get; private set; }

    /// <summary>Plans that ended one tile short of a target the ground would not take the walker onto, taken as arriving. See BotJourney.Beside.</summary>
    public static long Besides { get; private set; }

    /// <summary>
    /// Times somebody in the way was asked twice, did not move, and the bot stepped somewhere else instead.
    ///
    /// The measure of knots. It should be small and it should not be nought — nought would mean asking is
    /// always answered, which is what the walker used to assume without ever checking.
    /// </summary>
    public static long Knots { get; private set; }

    /// <summary>
    /// Whether somebody alive is on that tile, and if any of them is a bot, asks it to move.
    ///
    /// <para>
    /// <b>The asking happens after the enumeration, never inside it.</b> Stepping aside moves an entity, and
    /// moving an entity mutates the very collection being walked — the engine's enumerator then throws
    /// "collection was modified", which with a population of bots means it throws almost immediately and
    /// repeatedly. The first version documents this as a crash it had. Two lines and a local variable are the
    /// entire difference between a working mechanism and a shard full of exceptions.
    /// </para>
    ///
    /// <para>
    /// One asked per attempt, and one is enough: a tile has one occupant that matters, and if a second is
    /// somehow on it, the next refusal asks that one.
    /// </para>
    /// </summary>
    /// <param name="freed">
    /// Whether the tile is now clear. Only somebody askable can free it, and only by actually going: a
    /// creature that merely proves the tile taken frees nothing, and neither does a bot with nowhere of its
    /// own to step.
    /// </param>
    private static bool AskAsideAt(Mobile bot, Map map, Point3D tile, out bool freed)
    {
        Mobile occupant = null;

        freed = false;

        foreach (var mobile in map.GetMobilesAt(tile.X, tile.Y))
        {
            if (mobile == bot || !mobile.Alive || Math.Abs(mobile.Z - tile.Z) >= PersonHeight)
            {
                continue;
            }

            occupant = mobile;

            // A bot can be asked to move, so it is the one worth remembering; anything else merely proves
            // the tile is taken. Keep looking in case something askable is standing here too.
            if (mobile is IBotAside)
            {
                break;
            }
        }

        if (occupant == null)
        {
            return false;
        }

        if (occupant is IBotAside aside)
        {
            freed = aside.StepAsideFor(bot);
        }

        return true;
    }

    /// <summary>
    /// Opens the nearest shut door that is actually between the bot and where it is going.
    ///
    /// Locked doors stay locked: <see cref="BaseDoor.Use"/> enforces that itself, and a bot without the key
    /// learns that this way is genuinely shut — which is the honest lesson, and the one the planner already
    /// agrees with, since it counts a locked door as a wall.
    /// </summary>
    private static bool OpenDoorTowards(Mobile bot, Point3D goal)
    {
        var map = bot.Map;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        BaseDoor best = null;
        var bestDistance = double.MaxValue;

        foreach (var door in map.GetItemsInRange<BaseDoor>(bot.Location, DoorReach))
        {
            if (door.Deleted || door.Open)
            {
                continue;
            }

            var location = door.GetWorldLocation();

            // Only a door that is in the way: one on the far side of the bot is somebody else's problem.
            // Measured with a tile of slack, because a doorway the bot is level with is still the thing
            // between it and the room beyond.
            if (Utility.GetDistanceToSqrt(location, goal) > bot.GetDistanceToSqrt(goal) + 1.0)
            {
                continue;
            }

            var distance = bot.GetDistanceToSqrt(location);

            if (distance >= bestDistance)
            {
                continue;
            }

            best = door;
            bestDistance = distance;
        }

        if (best == null)
        {
            return false;
        }

        best.Use(bot);

        return best.Open;
    }
}
