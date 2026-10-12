using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What a bot is trying to get to, what it has put aside to do first, and the plan it is walking now.
///
/// <para>
/// <b>Owned by the bot</b>, like <see cref="BotBond"/>, and for the same reason: a bot that is deleted takes
/// its journey with it, and answering "where is this bot going" means asking the bot.
/// </para>
///
/// <para>
/// <b>A queue, not a single destination — and that is what makes an interruption something other than a
/// loss.</b> A bot walking to market that gets hit has three honest answers, and in the first version it only
/// had one. There, fleeing was a <em>goal</em>: it overwrote the errand, so the bot forgot where it had been
/// going, and half an hour of walking became a bot standing in a field wondering what it was for. Here, being
/// attacked pushes a second errand on top of the first. Deal with what is in front of you; the road is still
/// underneath, and it resumes by itself.
/// </para>
///
/// <para>
/// So the three answers become: the threat is several times over — keep walking, the errand never changed;
/// it is manageable — put the destination aside, kill the thing, then carry on; it killed you — the queue
/// dies with the bot. Movement implements the putting-aside. <b>Which of the three</b> is the decision
/// layer's, and nothing here has an opinion about combat.
/// </para>
///
/// <para>
/// <b>What counts as progress is the part that has to be got right</b>, and the first version got it wrong
/// three separate ways. It measured distance to the goal — true only while the only way to walk is straight,
/// and false the moment a bot leaves an enclosure, because the gate is twenty tiles the wrong way. It
/// compared a fresh plan's length against the finished one's, so a bot crossing a continent correctly, one
/// leg at a time, was told off every twenty-five seconds for going backwards. And it judged bots that were
/// not walking at all: a bot standing at a counter trading was measured against the distance to a home it had
/// no intention of visiting, and after twenty-five seconds was declared stuck, had its errand cancelled and
/// was barred from trading for five minutes — for trading.
/// </para>
///
/// <para>
/// So: progress is measured <b>along the plan</b>, in <b>attempts rather than seconds</b>, it restarts
/// whenever the plan does, and a journey with no plan is not judged at all.
/// </para>
/// </summary>
public sealed class BotJourney
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotJourney));

    /// <summary>
    /// How long a plan is trusted before it is drawn again from scratch. The world moves: a house goes up, a
    /// gate is shut, a corpse is dropped in a doorway. Nothing depends on this — a plan that stops working is
    /// noticed the moment a step fails — but a bot on a long walk should not be following a picture of the
    /// world from two minutes ago.
    /// </summary>
    public static int PlanStaleMs { get; set; } = 45000;

    /// <summary>
    /// How many attempts at stepping a bot may make, without getting anywhere, before the errand is given up.
    ///
    /// <para>
    /// <b>Attempts, not seconds, and this is the whole reason it is not a clock.</b> A bot held up by a fight
    /// is not failing to walk — nobody is asking it to walk. A clock would run out mid-fight and abandon the
    /// journey for the crime of being attacked, which is the opposite of the rule this design exists to
    /// state. Counting attempts makes the measure indifferent to time: a ten-minute battle costs zero
    /// attempts, and a hundred refused steps means a hundred refused steps whenever they happened.
    /// </para>
    ///
    /// <para>
    /// A hundred is generous — at a step every four hundred milliseconds, about forty seconds of continuous
    /// fruitless walking. It is a backstop and not a policy: the honest ways out are arriving, dying and
    /// being refused. But a bot getting nowhere and unable to admit it is a bot lost until the shard
    /// restarts, and the first version had one wander into a guarded city, where it could neither fight nor
    /// loot nor train, and stay for the rest of the session looking busy.
    /// </para>
    /// </summary>
    public static int StallAttempts { get; set; } = 100;

    /// <summary>
    /// How many plans in a row may come back with nowhere to walk before the errand is given up.
    ///
    /// The first version capped this and v2's first draft did not, which quietly recreated the defect the
    /// whole subsystem exists to end. A bot in a walled yard too large to be sealed inside the search's time
    /// ceiling gets a partial result with an empty path, every tick, for ever — no progress, no refusal, no
    /// complaint. That is fence-hugging again, wearing a new hat.
    /// </summary>
    public static int MaxEmptyPlans { get; set; } = 8;

    /// <summary>
    /// How many plans in a row an errand may draw without ever getting closer to where it is going.
    ///
    /// <para>
    /// <b>The backstop for a journey that is busy and going nowhere, which is a different failure from every
    /// other one here and was the one left uncovered.</b> <see cref="MaxEmptyPlans"/> catches a bot with
    /// nowhere to walk and <see cref="StallAttempts"/> catches a bot whose steps are refused — but a bot on a
    /// spit of land in the water has neither symptom. It gets a perfectly good plan to the tip of the spit,
    /// walks it, makes real progress along it, replans, walks back, and does that for ever: every individual
    /// plan succeeds, every step is allowed, and the destination never comes closer. Three bots spent
    /// twenty-three minutes that way and nothing in the log looked wrong, because from the inside it is
    /// indistinguishable from walking somewhere.
    /// </para>
    ///
    /// <para>
    /// The measure is the <b>best distance reached over the whole errand</b>, not distance within one plan.
    /// That distinction is what keeps it from repeating the first version's mistake: leaving a walled yard
    /// means walking away from the goal first, and a long road is walked in dashes that each start further
    /// out — both of those keep improving the best, so neither is punished. What cannot improve it is a bot
    /// oscillating between the same two places.
    /// </para>
    /// </summary>
    public static int MaxPlansWithoutCloser { get; set; } = 12;

    /// <summary>
    /// <b>A plan that reached somebody walking its own way is a way, not a plan without getting closer (12.10.2026, w7).</b> On
    /// a walk sent by <c>BotDoing.WalkAfter</c> — a carrier after its buyer, a buyer to its carrier, a guard after its trader —
    /// a plan whose outcome is Reached starts the count above again from where the walker stands. False keeps the old count.
    ///
    /// <para>
    /// <b>Measured over 11–12.10 (builds 432–434):</b> 1 436 deliveries and 100 caravan guards ended "could not get nearer to
    /// &lt;the one they walked after&gt;"; in the forty seconds before all of them the paths page holds 22 searches that came
    /// back without reaching it (every other plan reached it), over 80 % of the deliveries ended inside twelve seconds of
    /// walking, and 618 of them with the carrier three to five tiles off. The count wants each plan to end two tiles (<see cref="FollowSlack"/>) nearer than the best so far,
    /// which nobody walking after a body that walks at its own pace does, and near the end cannot do at all: from four tiles
    /// off "nearer" is one tile off, and the hand-over is at two. The buyer was at work — mining 27 %, its own delivery 17 %,
    /// a supply run, a prowl, a restock (in L2 544 of the 568 called to a meeting had not taken it up) — and it was on a horse often
    /// enough: Falk Ashdown rode off at eight tiles a second from Mirthal on foot (live, 12.10 22:45). Of what the old count
    /// did on top of the failure: the place the buyer stood was filed "no way" for every carrier (<c>BotWayless</c>, 823
    /// answers, 3 365 carrier looks passing a buyer over in L2), and the buyer was marked beyond reach for healers
    /// (<c>BotMend.Beyond</c>). The chase guard of build 382 (<c>BotWalk.ChasePlans</c>) already clears its own count on a
    /// reached plan for the same reason — "reachable is reachable, however far it runs"; this is the journey's count
    /// brought into line with it for the walks whose work has a clock of its own. Partial plans count as before, so a buyer
    /// in a shut room still ends the walk.
    /// </para>
    /// </summary>
    public static bool KeepUp { get; set; } = true;

    /// <summary>Plans of a walk after somebody about its own business that reached it and so started the closing count again. See <see cref="KeepUp"/>.</summary>
    public static long KeptUp { get; private set; }

    /// <summary>
    /// Walks after somebody about its own business that the old count would have given up — <see cref="MaxPlansWithoutCloser"/>
    /// plans without closing by <see cref="FollowSlack"/>, counted on the side — and that went on. Once a walk.
    /// </summary>
    public static long KeptOn { get; private set; }

    internal static void ForgetKeptUp()
    {
        KeptUp = 0;
        KeptOn = 0;
    }

    /// <summary>
    /// How long ground that nearly killed the bot stays out of its plans. Long enough for whatever it was to
    /// move on, short enough that the world does not fill up with ground nobody will cross.
    /// </summary>
    public static int DangerAvoidMs { get; set; } = 120000;

    /// <summary>
    /// How deep the queue may get.
    ///
    /// Four, and the reason to have a limit at all is that an evening of ambushes should not leave a bot with
    /// a to-do list. Something has to give when a fight is interrupted by a fight that is interrupted by a
    /// fight; the thing that gives is the <b>oldest ordinary errand</b>, because the deepest thing in the
    /// queue is the one whose reason is least likely to still be true.
    /// </summary>
    public static int MaxErrands { get; set; } = 4;

    /// <summary>
    /// How far a followed target may drift from where the plan was drawn before the plan is redrawn.
    ///
    /// Not zero, and this is the difference between chasing and thrashing: a monster moves every few hundred
    /// milliseconds, and replanning on each of its steps is one search per tick per pursuing bot. Two tiles
    /// of slack costs a step of overshoot and saves almost all of that.
    /// </summary>
    public const int FollowSlack = 2;

    /// <summary>The queue. The last entry is what the bot is doing now; earlier ones are waiting.</summary>
    private readonly List<BotErrand> _errands = [];

    private readonly List<Point3D> _plan = [];

    private int _step;

    /// <summary>When a step was last actually taken. See <see cref="Moving"/>.</summary>
    private long _steppedTick;

    private bool _stepped;

    /// <summary>Which errand this plan was drawn for, so resuming an older one always redraws.</summary>
    private BotErrand _planErrand;

    private Point3D _planGoal;

    private long _planBuiltTick;

    /// <summary>
    /// Which plan the progress figures belong to. Bumped on every replan, which is what stops a new leg from
    /// reading as movement backwards.
    /// </summary>
    private int _planStamp;

    private int _progressStamp;

    private int _bestRemaining;

    private int _attemptsSinceProgress;

    private int _emptyPlans;

    /// <summary>The closest this errand has ever managed to get, and how many plans ago that was.</summary>
    private int _bestAway = int.MaxValue;

    private int _plansSinceCloser;

    /// <summary>The old closing count and best, kept on the side for a walk that keeps up, so <see cref="KeptOn"/> says what it spared.</summary>
    private int _oldBestAway = int.MaxValue;

    private int _oldSinceCloser;

    private bool _keptOn;

    /// <summary>
    /// Which errand those two figures describe.
    ///
    /// <b>Tied to the errand rather than to the plan, and that distinction is the whole fix.</b> The first
    /// version of this check reset with the plan — and a plan is thrown away constantly: every improvised step
    /// discards one, and improvising is exactly what a bot does when it is wedged. So the bot that most needed
    /// the count was the one bot that could never accumulate it, and it went on walking in circles while
    /// everything else in the population gave up correctly.
    /// </summary>
    private BotErrand _awayErrand;

    private Point3D _avoidTile;

    private Point3D _blockedTile;

    private int _blockedCount;

    private int _dangerX1;
    private int _dangerY1;
    private int _dangerX2;
    private int _dangerY2;

    /// <summary>
    /// Whether there is a dangerous square to keep out of at all.
    ///
    /// A flag rather than "is the deadline still zero", which is this shard's rule for anything in the tick
    /// domain: on some hosts the counter is the physical machine's uptime passed straight through, so it
    /// starts enormous and can wrap negative — zero is a legitimate reading and useless as "never". See
    /// <c>dev-docs/tick-counts.md</c>.
    /// </summary>
    private bool _dangerous;

    private long _dangerUntil;

    /// <summary>What the bot is doing now, or null when it is not going anywhere.</summary>
    public BotErrand Current => _errands.Count > 0 ? _errands[^1] : null;

    /// <summary>
    /// The errand at the bottom of the road - what the bot is fundamentally doing - or null. The decision layer and a
    /// company both keep their errand in this one slot through <c>Rebase</c>, so each has to be able to ask whether it
    /// still does.
    /// </summary>
    public BotErrand Bottom => _errands.Count > 0 ? _errands[0] : null;

    /// <summary>Whether there is anything to do at all.</summary>
    public bool Active => _errands.Count > 0;

    /// <summary>How many errands are stacked up, the current one included.</summary>
    public int Queued => _errands.Count;

    public Map Map => Current?.Map;

    /// <summary>Where to walk this instant. Moves by itself when the current errand is following something.</summary>
    public Point3D Target => Current?.Target ?? Point3D.Zero;

    public BotArrival Arrival => Current?.Arrival ?? BotArrival.Beside;

    public string Reason => Current?.Reason;

    /// <summary>Whether a plan is being walked. An errand without one is not judged for progress.</summary>
    public bool Walking => _plan.Count > 0 && _step < _plan.Count;

    /// <summary>
    /// Whether the bot has actually taken a step lately, as against merely holding a plan.
    ///
    /// <para>
    /// <b>The distinction <see cref="Walking"/> does not make, and something load-bearing was reading
    /// Walking as though it did.</b> A plan with tiles left in it is not motion: a bot whose every step is
    /// refused holds one for as long as it stands there. BotSquads.ShouldYield used Walking to decide who
    /// gives way in a doorway — the one standing still yields to the one who is going somewhere — and its
    /// own comment argued the rule could not deadlock because "two bots both walking are both moving". They
    /// are not. Four bots met west of Britain at (1344-1345, 877-879) on 03.09.2026, each holding a plan
    /// through a tile another was standing on, so all four were Walking, all four refused to give way, and
    /// all four were still there four minutes later.
    /// </para>
    /// </summary>
    public bool Moving => _stepped && Core.TickCount - _steppedTick < MovingMs;

    /// <summary>
    /// How recently a step has to have been taken to count as moving.
    ///
    /// Two seconds, which is several steps at either pace and long enough to cover a bot going round
    /// something. Shorter and a bot gives way while it is mid-detour; longer and a knot takes longer to
    /// come apart than the stall watch takes to notice it.
    /// </summary>
    public static int MovingMs { get; set; } = 2000;

    /// <summary>True when the last plan led as close as the world allows rather than to the goal.</summary>
    public bool Partial { get; private set; }

    /// <summary>Plans drawn for the current errand. High and rising is a bot fighting the world.</summary>
    public int Plans { get; private set; }

    /// <summary>
    /// Whether the searches have stopped offering anywhere to walk. The destination is not refused — that is
    /// a separate and provable thing — it simply cannot be worked towards from here.
    /// </summary>
    public bool Hopeless => _emptyPlans >= MaxEmptyPlans || _plansSinceCloser >= MaxPlansWithoutCloser;

    /// <summary>
    /// Plans in a row that have not got this errand any closer than it has already been.
    ///
    /// <see cref="Hopeless"/> is what this eventually becomes; exposed on its own because there is something
    /// worth doing well before then. A journey that is not closing is the one situation in which asking about
    /// the far side of it is worth the tiles.
    /// </summary>
    public int PlansSinceCloser => _plansSinceCloser;

    /// <summary>
    /// Whether the far side of this errand has already been looked at.
    ///
    /// Once per errand, not once per plan. What the look finds is a fact about the world, filed where the
    /// whole population reads it, so asking again about the same destination would buy nothing and pay twice.
    /// </summary>
    public bool Probed { get; set; }

    /// <summary>
    /// Whether this errand has had its one long search for a way round. See <c>BotWalk.Plan</c>: after the
    /// far side has been looked at and found to be the world, one search at the stranded ceiling either
    /// finds the detour or proves there is none worth twelve more ordinary searches.
    /// </summary>
    public bool Escalated { get; set; }

    /// <summary>The tiles left to walk on the current plan, in order.</summary>
    public IReadOnlyList<Point3D> Plan => _plan;

    /// <summary>A walk nearer than this is planned straight at its target; further, over the navigation graph. See <see cref="Leg"/>.</summary>
    public static int DirectTiles { get; set; } = 24;

    /// <summary>
    /// A walk nearer than <see cref="DirectTiles"/> is routed all the same once its straight plan has stopped closing and the
    /// road map says one end lies this much further round than the other: the far bank of a river, twenty tiles across.
    /// </summary>
    public static int RoundTiles { get; set; } = 40;

    /// <summary>The furthest ahead along the route one leg looks.</summary>
    public static int LegTiles { get; set; } = 24;

    /// <summary>How near a point of the route counts as reached, and how near a leg's plan has to end.</summary>
    public static int LegReached { get; set; } = 3;

    /// <summary>How far from the point it is walking to a bot may be before the route is drawn again from where it is.</summary>
    public static int StrayTiles { get; set; } = 24;

    /// <summary>How many times one errand's route may be drawn again for a failed leg or a stray before it is walked straight.</summary>
    public static int MostReroutes { get; set; } = 4;

    /// <summary>Routes drawn over the navigation graph, over the old chart where the graph was not drawn yet, and found to have no way.</summary>
    public static long Routed { get; private set; }

    /// <summary>Routes drawn to a gate, the target lying in another land. See <see cref="BotGates"/>.</summary>
    public static long Gated { get; private set; }

    /// <summary>A station or a sweep nearer than this is walked straight, as every station was before the gates; further, it is routed, gates and all.</summary>
    public static int StationDirect { get; set; } = 160;

    /// <summary>Whether the leg in hand must be reached exactly: it is a gate's tile. See <see cref="Leg"/>.</summary>
    public bool LegExact { get; private set; }

    public static long Charted { get; private set; }

    public static long NoWay { get; private set; }

    /// <summary>Routes never asked of the planner because the reach map has no open ground at the target (01.10.2026).</summary>
    public static long MapRefused { get; private set; }

    private BotErrand _legErrand;

    /// <summary>
    /// Changes whenever a route is drawn for any errand of this journey, so whoever measures progress along it (the will's
    /// watch, an errand's own) starts its best distance again rather than comparing a road with a straight line.
    /// </summary>
    public int RouteStamp { get; private set; }

    private int _awayStamp = -1;

    /// <summary>Whether the plan in hand walks a leg of the route rather than straight at the target.</summary>
    public bool Legging { get; private set; }

    /// <summary>Where the plan in hand is going: the leg's point while <see cref="Legging"/>, the target otherwise.</summary>
    public Point3D Aim => Legging && _legErrand != null ? _legErrand.Aim : Target;

    /// <summary>Points of the current errand's route still ahead, for the summary line and the door.</summary>
    public int RouteLeft => Current is { } errand ? System.Math.Max(0, errand.Route.Count - errand.Leg) : 0;

    /// <summary>
    /// Tiles still to walk by the route the planner drew for the current errand: from <paramref name="from"/> through every point
    /// of it still ahead to the target, each stretch as the engine measures it (the larger axis). Nought when no route is drawn
    /// (a near target walked straight). The planner's estimate of the road, where the straight line is a guess
    /// (06.10.2026, deliver16: a delivery's hold). A walk of a list a few dozen points long.
    /// </summary>
    public int RoadLeft(Point3D from)
    {
        if (Current is not { } errand || errand.Route.Count == 0 || errand.Leg >= errand.Route.Count)
        {
            return 0;
        }

        var tiles = 0;
        var at = from;

        for (var i = System.Math.Max(0, errand.Leg); i < errand.Route.Count; i++)
        {
            var next = errand.Route[i];

            tiles += System.Math.Max(System.Math.Abs(next.X - at.X), System.Math.Abs(next.Y - at.Y));
            at = next;
        }

        var target = errand.Target;

        return tiles + System.Math.Max(System.Math.Abs(target.X - at.X), System.Math.Abs(target.Y - at.Y));
    }

    /// <summary>Whether the navigation graph has found no way on the ground to the current errand's target.</summary>
    public bool NoWayThere => Current?.Unreachable == true;

    /// <summary>
    /// The point of the route the next plan should walk to, when the target is far, or near but round a river; false to
    /// plan straight at the target, as before the tiers.
    ///
    /// <para>
    /// <b>Patrick's order of 26.09.2026, and the design he sent the same day:</b> "plan coarse, refine lazily, execute
    /// locally". The route comes from the navigation graph (<c>NavigationService</c>, the medium tier: hierarchical A* over
    /// clusters of 16 tiles, floors and bridges kept apart), or, where the graph is not drawn yet, from the old chart round
    /// home (<see cref="BotChart"/>); each leg is the precise search's (Short). A route belongs to its errand, so a fight
    /// on top keeps its own and the road underneath is still there after it. It is drawn again when the goal walks off
    /// by half a leg, when a leg fails and when the bot strays <see cref="StrayTiles"/> from its point; the last two
    /// count, and after <see cref="MostReroutes"/> the errand is walked straight. A company's stations and sweeps, handed
    /// out afresh every beat the anchor moves and never far from it, are never routed.
    /// </para>
    /// </summary>
    public bool Leg(Map map, Point3D at, out Point3D leg)
    {
        leg = Point3D.Zero;
        Legging = false;
        LegExact = false;
        _legErrand = null;

        var errand = Current;

        if (errand == null || map == null || errand.RouteSpent)
        {
            return false;
        }

        var target = errand.Target;

        // A station or a sweep is handed out afresh every beat the anchor moves and is never far from it, so it was never
        // routed — until the anchor could be on the far side of a gate. A leader through a cave mouth leaves its company's
        // stations in the cave; those are routed like any errand, gates and all, and the near ones are walked as before.
        if (errand.Reason is "station" or "sweep" && Away(at, target) <= StationDirect)
        {
            return false;
        }

        // <b>Through the gate: the route is spent and a new one is drawn from where the engine put the walker down.</b>
        // Not a reroute — the walker did not stray, it arrived — so the count of reroutes is left alone.
        if (errand.Gate != Point3D.Zero && Away(at, errand.GateTo) <= BotGates.Through)
        {
            // A way out filed as refused is open again (work17). See BotGates.WalkRefused.
            BotGates.WalkedThrough(map, errand.Gate);

            errand.Gate = Point3D.Zero;
            errand.GateTo = Point3D.Zero;
            errand.RouteGoal = Point3D.Zero;
            errand.Route.Clear();
            errand.Leg = 0;
            BotGates.NotedPassed();
        }

        var live = errand.RouteGoal != Point3D.Zero && errand.Leg < errand.Route.Count;
        var round = _plansSinceCloser > 0 && BotRoads.Farther(map, at, target) >= RoundTiles;

        if (Away(at, target) <= DirectTiles && !live && !round && errand.RouteGoal == Point3D.Zero)
        {
            return false;
        }

        var strayed = live && Away(at, errand.Route[errand.Leg]) > StrayTiles;
        var drifted = errand.RouteGoal != Point3D.Zero && Away(target, errand.RouteGoal) > LegTiles / 2;

        if (errand.RouteGoal == Point3D.Zero || errand.Redraw || strayed || drifted)
        {
            if ((errand.Redraw || strayed) && ++errand.Reroutes > MostReroutes)
            {
                errand.RouteSpent = true;
                errand.Route.Clear();

                return false;
            }

            errand.Redraw = false;
            errand.RouteGoal = target;
            errand.Leg = 0;
            Draw(map, at, target, errand);
        }

        if (errand.Unreachable)
        {
            return false;
        }

        while (errand.Leg < errand.Route.Count && Away(at, errand.Route[errand.Leg]) <= LegReached)
        {
            // The gate's own tile is reached by standing on it, not near it: the engine teleports on the step onto the tile.
            if (errand.Gate != Point3D.Zero && errand.Leg == errand.Route.Count - 1
                && (at.X != errand.Route[errand.Leg].X || at.Y != errand.Route[errand.Leg].Y))
            {
                break;
            }

            errand.Leg++;
        }

        if (errand.Leg >= errand.Route.Count)
        {
            // Standing on the gate and still here: the engine did not put the walker through (a criminal at a guarded
            // gate, a body in a fight where the gate asks). Said once, and the errand is walked straight from here on.
            if (errand.Gate != Point3D.Zero)
            {
                BotGates.NotedRefused();
                errand.Gate = Point3D.Zero;
                errand.GateTo = Point3D.Zero;
                errand.RouteSpent = true;
            }

            return false;
        }

        var pick = errand.Leg;
        var along = Away(at, errand.Route[errand.Leg]);

        for (var j = errand.Leg + 1; j < errand.Route.Count && j <= errand.Leg + 3; j++)
        {
            along += Away(errand.Route[j - 1], errand.Route[j]);

            if (along > LegTiles || Away(at, errand.Route[j]) > LegTiles)
            {
                break;
            }

            pick = j;
        }

        errand.Leg = pick;
        errand.Aim = errand.Route[pick];
        leg = errand.Aim;
        Legging = true;
        LegExact = errand.Gate != Point3D.Zero && pick == errand.Route.Count - 1;
        _legErrand = errand;

        return true;
    }

    /// <summary>Draws the errand's route: the navigation graph first, the old chart where the graph cannot say yet.</summary>
    private void Draw(Map map, Point3D at, Point3D target, BotErrand errand)
    {
        errand.Unreachable = false;
        errand.FromChart = false;
        errand.Gate = Point3D.Zero;
        errand.GateTo = Point3D.Zero;
        RouteStamp++;

        // <b>No route is formed into ground a walk from home never reaches (01.10.2026, Patrick's order of 30.09.2026, night).</b>
        // From open ground, a target with no open tile within its arrival (or two tiles, the most "beside" ever means) is not
        // asked of the planner at all: the errand is marked as having no way, and the walker's own search hears the same answer
        // from the reach ledger and drops it. Aboard a ship the land is refused the same way. See BotReachMap.Refuses.
        if (BotReachMap.Refuses(map, at, target, Math.Max(errand.Arrival.Tiles, 2)))
        {
            BotReachMap.NotePlanner();
            MapRefused++;
            errand.Unreachable = true;
            errand.NoWaySaid = true;
            errand.Route.Clear();

            return;
        }

        switch (Server.Engines.Pathing.Tiered.NavigationService.Route(map, at, target, errand.Route, errand.Arrival.Tiles))
        {
            case Server.Engines.Pathing.Tiered.NavStatus.Ok:
                {
                    Routed++;

                    return;
                }
            case Server.Engines.Pathing.Tiered.NavStatus.Direct:
                {
                    errand.Route.Clear();

                    return;
                }
            case Server.Engines.Pathing.Tiered.NavStatus.Unreachable:
                {
                    // <b>No way on the ground — but perhaps a gate.</b> The target lies in another land; if the gates join
                    // the two, the route is drawn to the first gate on the way and ends on its tile, and the next draw
                    // begins wherever the engine puts the walker down. See BotGates (Patrick's order of 29.09.2026).
                    if (BotGates.Next(map, at, target, out var gate))
                    {
                        var toGate = Server.Engines.Pathing.Tiered.NavigationService.Route(map, at, gate.From, errand.Route, 0);

                        if (toGate is Server.Engines.Pathing.Tiered.NavStatus.Ok or Server.Engines.Pathing.Tiered.NavStatus.Direct)
                        {
                            if (toGate == Server.Engines.Pathing.Tiered.NavStatus.Direct)
                            {
                                errand.Route.Clear();
                            }

                            errand.Route.Add(gate.From);
                            errand.Gate = gate.From;
                            errand.GateTo = gate.To;
                            Gated++;

                            return;
                        }
                    }

                    NoWay++;
                    errand.Unreachable = true;
                    errand.Route.Clear();

                    return;
                }
            default:
                {
                    // Not drawn there yet, over the search's budget, or an end the graph cannot place: the old chart.
                    if (BotChart.Route(map, at, target, errand.Route))
                    {
                        Charted++;
                        errand.FromChart = true;
                    }

                    return;
                }
        }
    }

    /// <summary>
    /// The leg in hand could not be walked. With <paramref name="shun"/> — the search was a fair one, neither starved of
    /// clock nor kept out of ground on purpose — its gate is shunned where the route came from, and the route is drawn
    /// again at the next plan.
    /// </summary>
    public void LegFailed(Map map, bool shun)
    {
        var errand = _legErrand;

        if (!Legging || errand == null)
        {
            return;
        }

        if (shun)
        {
            if (errand.FromChart)
            {
                BotChart.Shun(map, errand.Aim);
            }
            else
            {
                Server.Engines.Pathing.Tiered.NavigationService.Shun(map, errand.Aim);
            }
        }

        errand.Redraw = true;
        Legging = false;
    }

    /// <summary>How far the target is from <paramref name="at"/>: by the rest of its route while one is held, straight otherwise.</summary>
    private int Distance(Point3D at) => RoadLeft(at, Target);

    /// <summary>Whether some errand of this journey holds a live route to within half a leg of <paramref name="where"/>, so <see cref="RoadLeft"/> answers by the road.</summary>
    public bool HoldsRoadTo(Point3D where)
    {
        for (var i = _errands.Count - 1; i >= 0; i--)
        {
            var errand = _errands[i];

            if (!errand.RouteSpent && errand.Route.Count > 0 && errand.Leg < errand.Route.Count && Away(errand.RouteGoal, where) <= LegTiles / 2)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// How far <paramref name="where"/> is from <paramref name="at"/> by the rest of the route of whichever errand holds one
    /// to that place; straight otherwise. For the will's watch over a walk that has stopped closing (BotWill) and the
    /// errands' own, which measured straight would call the road round a river a walk going the wrong way.
    /// </summary>
    public int RoadLeft(Point3D at, Point3D where)
    {
        for (var i = _errands.Count - 1; i >= 0; i--)
        {
            var errand = _errands[i];

            if (errand.RouteSpent || errand.Route.Count == 0 || errand.Leg >= errand.Route.Count
                || Away(errand.RouteGoal, where) > LegTiles / 2)
            {
                continue;
            }

            var left = Away(at, errand.Route[errand.Leg]);

            for (var k = errand.Leg + 1; k < errand.Route.Count; k++)
            {
                left += Away(errand.Route[k - 1], errand.Route[k]);
            }

            return left + Away(errand.Route[^1], where);
        }

        return Away(at, where);
    }

    public int Remaining => _plan.Count - _step;

    /// <summary>
    /// Sets out afresh. Everything queued is dropped: this is the bot choosing what its evening is about, not
    /// adding to a list.
    /// </summary>
    public void Begin(Map map, Point3D where, BotArrival arrival, string reason)
    {
        _errands.Clear();

        Push(new BotErrand { Map = map, Where = where, Arrival = arrival, Reason = reason });
    }

    /// <summary>The same, after something that walks — an escort, a debtor, somebody being followed home.</summary>
    public void Begin(Map map, Mobile follow, BotArrival arrival, string reason)
    {
        _errands.Clear();

        Push(new BotErrand { Map = map, Follow = follow, Arrival = arrival, Reason = reason });
    }

    /// <summary>
    /// Replaces what the bot is fundamentally doing, <b>without disturbing anything stacked on top of it</b>.
    ///
    /// <para>
    /// This exists because a squad tells its members where to stand, and telling somebody where to stand must
    /// not cancel the fight they are in the middle of. <see cref="Begin"/> clears the queue — it is the bot
    /// choosing what its evening is about — and using it for a station would have wiped every interruption the
    /// moment the formation shifted, which is the moment a fight starts. The bottom of the queue is where the
    /// bot belongs; the top is what is happening to it.
    /// </para>
    /// </summary>
    public void Rebase(Map map, Point3D where, BotArrival arrival, string reason)
    {
        if (map == null)
        {
            return;
        }

        Rebase(new BotErrand { Map = map, Where = where, Arrival = arrival, Reason = reason });
    }

    /// <summary>
    /// The same, after something that walks.
    ///
    /// Added for the decision layer, which puts a bot's own undertaking at the bottom of the queue the way a
    /// squad puts a station there — and some undertakings are a creature rather than a place: quarry, an
    /// escort, somebody being followed home. Without it those could only be pushed as interruptions, and an
    /// interruption is by definition the thing happening now, not the thing the bot is fundamentally doing.
    /// </summary>
    public void Rebase(Map map, Mobile follow, BotArrival arrival, string reason, bool keepsUp = false)
    {
        if (map == null || follow == null)
        {
            return;
        }

        Rebase(new BotErrand { Map = map, Follow = follow, Arrival = arrival, Reason = reason, KeepsUp = keepsUp });
    }

    private void Rebase(BotErrand errand)
    {
        if (_errands.Count == 0)
        {
            Push(errand);

            return;
        }

        var wasCurrent = _errands.Count == 1;

        _errands[0] = errand;

        // Only when the thing replaced was also the thing being walked. Otherwise an interruption is in
        // progress and its plan is still perfectly good.
        if (wasCurrent)
        {
            Discard();
        }
    }

    /// <summary>
    /// Puts the current errand aside and does this first. What the road already knew is not lost.
    ///
    /// The whole mechanism the design asks for: point B is remembered, the fight goes on top, and when the
    /// fight ends the bot is already pointed at B again with nobody having had to store it anywhere.
    /// </summary>
    public void Interrupt(Map map, Mobile follow, BotArrival arrival, string reason)
    {
        Push(
            new BotErrand
            {
                Map = map,
                Follow = follow,
                Arrival = arrival,
                Reason = reason,
                Interruption = true
            }
        );
    }

    /// <summary>The same, for an interruption that is a place rather than a creature.</summary>
    public void Interrupt(Map map, Point3D where, BotArrival arrival, string reason)
    {
        Push(
            new BotErrand
            {
                Map = map,
                Where = where,
                Arrival = arrival,
                Reason = reason,
                Interruption = true
            }
        );
    }

    /// <summary>
    /// This errand is done with, however it ended. Returns whether anything was waiting underneath.
    ///
    /// One method for all three endings — the market was reached, the monster is dead, the place turned out to
    /// be unreachable — because from movement's side they are the same event: the thing on top is finished,
    /// and whatever it was covering is next. Which of the three it was belongs in the log, not in the control
    /// flow.
    /// </summary>
    public bool Complete()
    {
        if (_errands.Count == 0)
        {
            return false;
        }

        _errands.RemoveAt(_errands.Count - 1);

        Discard();

        return _errands.Count > 0;
    }

    /// <summary>
    /// Drops errands whose reason has evaporated — a followed creature that died or vanished — from anywhere
    /// in the queue, and says how many went.
    ///
    /// From anywhere rather than just the top, because a queued errand can lapse while something else is
    /// happening: a bot that put aside chasing one skeleton to deal with another may find the first one dead
    /// by the time it looks up, killed by somebody else.
    /// </summary>
    public int Prune()
    {
        var dropped = 0;

        for (var i = _errands.Count - 1; i >= 0; i--)
        {
            if (!_errands[i].Lapsed)
            {
                continue;
            }

            var top = i == _errands.Count - 1;

            _errands.RemoveAt(i);
            dropped++;

            if (top)
            {
                Discard();
            }
        }

        return dropped;
    }

    /// <summary>
    /// Drops every interruption from anywhere in the queue and says how many went; the bot's own errand stays.
    ///
    /// <para>
    /// <b>For a bot that has decided to leave (30.09.2026, build 340).</b> The Will sends a flight's road with
    /// <see cref="Rebase"/>, which keeps whatever is stacked on top — on purpose, for a company's stations — and the
    /// "fight" that <c>BotMobile.Engage</c> pushed follows its creature until that creature is dead. So a bot that had
    /// squared up to a troll and then ran from it was walked to the troll's side, Arrived, and stood there: 11 of 24
    /// fatal flights on build 338 never moved a tile, nine of them from something hand to hand one tile off.
    /// </para>
    /// </summary>
    public int Shed()
    {
        var dropped = 0;

        for (var i = _errands.Count - 1; i >= 0; i--)
        {
            if (!_errands[i].Interruption)
            {
                continue;
            }

            var top = i == _errands.Count - 1;

            _errands.RemoveAt(i);
            dropped++;

            if (top)
            {
                Discard();
            }
        }

        return dropped;
    }

    /// <summary>Everything forgotten. Death, or the bot being told to do something else entirely.</summary>
    public void Finish()
    {
        _errands.Clear();

        Discard();

        _dangerous = false;
    }

    /// <summary>
    /// <b>Standing still for a moment with the road kept (01.10.2026, build next).</b> A leader holding its column — a delve's
    /// march for a straggler or a member crossing the mouth, a sweep's for the same — finished its walk on every held turn and
    /// asked for it again on the next, and each asking was a new errand: the route drawn again over the navigation graph and a
    /// fresh plan, two or three times a second for a column of twelve. On build 386 Imogen's march from Britain to the Orc
    /// Caves was dropped at (1488, 1375) "because it planned 31 roads to one place in 18s" (BotWalk's loop guard), three times
    /// in eighty seconds, and both delves of the session ended "no way through" on the road with 506 holds between them. A
    /// hold is now a hold: the walker takes no step while this lasts (BotWalk.Advance answers <see cref="BotWalkResult.Held"/>),
    /// the plan and the route stay as they are, and the next turn that is not a hold walks on from where the plan left off.
    /// Renewed by every held turn; it lapses by itself. Any new order lifts it (<see cref="Discard"/>).
    /// </summary>
    public void Hold(int ms)
    {
        _heldUntil = Core.TickCount + System.Math.Max(0, ms);
        _holding = true;
    }

    /// <summary>
    /// Whether the walk is being held where it stands. See <see cref="Hold"/>. By subtraction from a deadline seeded by a real
    /// tick, with a flag for "no hold" rather than a nought deadline (12.10.2026, w7: rule 20 of the fork, now asked by every
    /// delivery's buyer and every caravan trader, not a delve leader now and then).
    /// </summary>
    public bool Held => Active && _holding && Core.TickCount - _heldUntil < 0;

    private long _heldUntil;

    private bool _holding;

    /// <summary>Whether standing at <paramref name="at"/> is arriving at what the bot is doing now.</summary>
    /// <summary>
    /// The last plan ended one tile short of the target on ground that will not take the walker: the tile is a tree, a
    /// wall-top, another body. Standing beside it is as near as the world allows, and it counts as arriving. See
    /// <c>BotWalk.Plan</c> (29.09.2026): Bardryn asked the road to a station one tile off 161 times in ten minutes,
    /// Delvira to a creature 172 times, every answer "partial", four to sixty milliseconds each.
    /// </summary>
    private bool _beside;

    public void Beside() => _beside = true;

    /// <summary>How near "beside" is: one tile for an errand, two for a station or a sweep, whose place is a place in a ring.</summary>
    public static int BesideTiles(string reason) => reason is "station" or "sweep" ? 2 : 1;

    public bool Arrived(Point3D at) => Active && (Arrival.Reached(at, Target) || _beside && Away(at, Target) <= BesideTiles(Reason));

    /// <summary>
    /// Whether a fresh plan is needed before the next step: there is none, it belongs to another errand, the
    /// target has walked off, the bot is no longer on it, or it has gone stale.
    /// </summary>
    public bool NeedsPlan(Point3D at)
    {
        var errand = Current;

        if (errand == null)
        {
            return false;
        }

        if (_plan.Count == 0 || _step >= _plan.Count)
        {
            return true;
        }

        // A different errand entirely — an interruption pushed, or an older one resumed.
        if (!ReferenceEquals(_planErrand, errand))
        {
            return true;
        }

        // The target has moved. Slack rather than exact equality, because a plan redrawn on every step of a
        // fleeing skeleton is one search per tick.
        var target = errand.Target;

        // <b>Slack grows with the plan left to walk.</b> Two tiles is right when the bot is nearly there; forty
        // tiles out, the first thirty of the plan are as good as they were whatever the target did since,
        // and redrawing it every two tiles of the target's wandering was a sixty-millisecond search for every
        // couple of its steps - the chase's whole cost. The plan is walked to its end and redrawn then.
        var slack = Math.Max(FollowSlack, (_plan.Count - _step) / 4);

        if (Math.Abs(target.X - _planGoal.X) > slack || Math.Abs(target.Y - _planGoal.Y) > slack)
        {
            return true;
        }

        // Off the plan. A plan is a list of adjacent tiles, so if the next one is not adjacent then the bot is
        // not where the plan thinks it is — it was teleported, shoved, or it improvised its way round
        // something. Walking on regardless means aiming at a tile two or more away and failing the step for
        // ever, which reads as a wall and is not one.
        var next = _plan[_step];

        if (Math.Abs(at.X - next.X) > 1 || Math.Abs(at.Y - next.Y) > 1)
        {
            return true;
        }

        return Core.TickCount - _planBuiltTick >= PlanStaleMs;
    }

    /// <summary>
    /// Takes the outcome of a search. The plan's own bookkeeping and the progress count both restart here,
    /// together, because they are the same event.
    /// </summary>
    public void Planned(BotPathOutcome outcome, List<Point3D> path, Point3D at)
    {
        _plan.Clear();

        if (path != null)
        {
            _plan.AddRange(path);
        }

        _step = 0;
        _planErrand = Current;
        _planGoal = Target;
        _planBuiltTick = Core.TickCount;
        Partial = outcome == BotPathOutcome.Partial;

        Plans++;

        // A plan with nowhere to walk. On its own unremarkable — the bot is boxed in for the moment — and in a
        // row it is the signature of a bot that will never get out.
        if (_plan.Count == 0)
        {
            _emptyPlans++;
        }
        else
        {
            _emptyPlans = 0;
        }

        // Has this errand ever been closer than it is now? Measured here, once per plan, because this is the
        // only moment at which "the last plan achieved nothing" can be told from "the last plan is still
        // being walked".
        // <b>Along the route while one is held</b> (Distance): the road round a river leads away from the target for a
        // thousand steps, and measured straight every one of its plans was "no closer". A new route is a new measure, so the
        // best so far starts again with it.
        var away = Distance(at);

        if (_awayStamp != RouteStamp)
        {
            _awayStamp = RouteStamp;
            _bestAway = int.MaxValue;
            _oldBestAway = int.MaxValue;
        }

        // A different errand entirely — pushed, resumed or rebased. Its own history starts here.
        if (!ReferenceEquals(_awayErrand, Current))
        {
            _awayErrand = Current;
            _bestAway = int.MaxValue;
            _plansSinceCloser = 0;
            _oldBestAway = int.MaxValue;
            _oldSinceCloser = 0;
            _keptOn = false;
            Probed = false;
            Escalated = false;
        }

        // <b>A chase is closer only when the bot closed it, not when the target wandered a tile nearer (01.10.2026, build
        // 382).</b> The measure is the straight distance to a target that walks about, and a shopkeeper paces her shop: every
        // step of hers towards the bot read as the bot getting closer and put this count back to nought, so the far-side look
        // (at 2) and the one long search (after it) that end a walk to somewhere unreachable never came. On build 381 Odile
        // held "restock: after 2 LesserHealPotion" for 11.6 minutes walking at Judith in Jhelom with no way there, planning
        // 1071 roads at about 1.5 a second — the chase share of searches fell to 26 % (2198 of 8398) from ~98 % — and Adela did
        // the same after a sheep (915). A target's own drift is inside FollowSlack by the walker's own definition of a plan
        // worth redrawing, so a chase must close by more than that to count. Only a chase: a fixed place does not move.
        var slack = Current?.Follow != null ? FollowSlack : 0;

        // <b>Except a walk after somebody about its own business, whose reached plan is a way (12.10.2026, w7).</b> See KeepUp: the
        // count starts again from here, and the old count is kept on the side only to say what this spared.
        var keeps = KeepUp && Current is { KeepsUp: true, Follow: not null };

        if (keeps)
        {
            if (away < _oldBestAway - slack)
            {
                _oldBestAway = away;
                _oldSinceCloser = 0;
            }
            else
            {
                _oldSinceCloser++;
            }
        }

        if (keeps && outcome == BotPathOutcome.Reached)
        {
            KeptUp++;
            _bestAway = away;
            _plansSinceCloser = 0;
        }
        else if (away < _bestAway - slack)
        {
            _bestAway = away;
            _plansSinceCloser = 0;
        }
        else
        {
            _plansSinceCloser++;
        }

        if (keeps && !_keptOn && _oldSinceCloser >= MaxPlansWithoutCloser && _plansSinceCloser < MaxPlansWithoutCloser)
        {
            _keptOn = true;
            KeptOn++;
        }

        // A new plan, so what counts as progress starts again. This single line is the fix for the second of
        // the three progress bugs: without it every leg of a long journey looks like going backwards.
        _planStamp++;

        // One tile that somebody was standing on when the last step was refused, kept off this plan and this
        // plan only. Consumed here, so a person who has since walked away stops being avoided.
        _avoidTile = Point3D.Zero;
    }

    /// <summary>
    /// What the next search should keep out of: a person in the way, and ground that nearly killed the bot.
    ///
    /// <para>
    /// The dangerous square is dropped in two cases, and both are the difference between a route that bends
    /// and a bot that cannot move. If the <b>target</b> is inside it, honouring it makes the errand
    /// unplannable for two minutes — and the destination is the one thing nothing here may touch. If the
    /// <b>bot</b> is inside it, every neighbouring tile is excluded too and the search has nowhere to begin:
    /// a bot that was nearly killed is standing where it was nearly killed, which is exactly when it most
    /// needs to be able to leave.
    /// </para>
    /// </summary>
    public BotAvoid Avoid(Point3D at)
    {
        var avoid = BotAvoid.None;

        if (_dangerous && Core.TickCount - _dangerUntil < 0 && !Inside(Target) && !Inside(at))
        {
            avoid = BotAvoid.Square(_dangerX1, _dangerY1, _dangerX2, _dangerY2);
        }

        if (_avoidTile != Point3D.Zero)
        {
            avoid = avoid.And(_avoidTile);
        }

        // The reach of what the bot has seen and cannot beat, priced rather than shut. See BotSight (03.10.2026, sight13).
        if (Sighted is { } reaches)
        {
            avoid = avoid.Reaches(reaches);
        }

        return avoid;
    }

    // ------------------------------------------------------------------------------------------------ what the bot has seen

    private BotReaches _sighted;

    private long _sightedUntil;

    /// <summary>
    /// The reach of creatures the bot has seen and cannot beat, while the sighting stands; null otherwise. Written by
    /// <see cref="BotSight"/> on its look, read by every plan drawn meanwhile (<see cref="Avoid"/>) and by the route's danger
    /// hook (<see cref="BotDanger.For"/>). Kept with the journey rather than the errand: what stands near the bot stands near
    /// whatever it is walking to.
    /// </summary>
    public BotReaches Sighted => _sighted is { Count: > 0 } && Core.TickCount - _sightedUntil < 0 ? _sighted : null;

    /// <summary>What the bot sees and cannot beat, standing until <paramref name="until"/>; null to forget it.</summary>
    public void Sight(BotReaches reaches, long until)
    {
        _sighted = reaches;
        _sightedUntil = until;
    }

    /// <summary>
    /// The first tile of the plan in hand, within <paramref name="ahead"/> steps, that lies within <paramref name="slack"/> tiles of
    /// one of <paramref name="reaches"/>, as a count of steps from where the bot stands; -1 when the plan keeps clear.
    /// </summary>
    public int PlanEnters(BotReaches reaches, int ahead, int slack)
    {
        if (reaches == null)
        {
            return -1;
        }

        var last = Math.Min(_plan.Count, _step + Math.Max(1, ahead));

        for (var i = _step; i < last; i++)
        {
            if (reaches.Within(_plan[i].X, _plan[i].Y, slack))
            {
                return i - _step;
            }
        }

        return -1;
    }

    /// <summary>When the plan in hand was drawn (<c>Core.TickCount</c>); nought when there is none.</summary>
    public long PlanBuilt => _plan.Count > 0 ? _planBuiltTick : 0;

    /// <summary>The tile <paramref name="ahead"/> steps along the plan in hand, or nought when the plan is shorter.</summary>
    public Point3D PlanAt(int ahead) =>
        _step + ahead >= 0 && _step + ahead < _plan.Count ? _plan[_step + ahead] : Point3D.Zero;

    /// <summary>
    /// Whether the live route of the errand in hand passes, within <paramref name="ahead"/> tiles along it, through one of
    /// <paramref name="reaches"/>. Gates are left alone: a route that ends on a gate is drawn to its tile and nowhere else.
    /// </summary>
    public bool RouteEnters(BotReaches reaches, Point3D at, int ahead)
    {
        var errand = Current;

        if (reaches == null || errand == null || errand.RouteSpent || errand.Gate != Point3D.Zero || errand.RouteGoal == Point3D.Zero
            || errand.Leg >= errand.Route.Count)
        {
            return false;
        }

        var along = 0;
        var from = at;

        for (var i = errand.Leg; i < errand.Route.Count && along <= ahead; i++)
        {
            var point = errand.Route[i];

            along += Away(from, point);
            from = point;

            if (reaches.Within(point.X, point.Y, 0))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <b>The route drawn again for what the bot has seen, and not counted as a reroute (03.10.2026, sight13).</b> A reroute is
    /// a leg that failed or a bot that strayed, and four of them walk the errand straight (<see cref="MostReroutes"/>); a
    /// creature standing on the road is neither. The next plan draws the route from where the bot stands, the danger hook
    /// pricing what it saw (<see cref="BotDanger.For"/>), and the plan in hand is thrown away.
    /// </summary>
    public void Resight()
    {
        if (Current is { } errand && errand.Gate == Point3D.Zero && !errand.RouteSpent)
        {
            errand.RouteGoal = Point3D.Zero;
            errand.Route.Clear();
            errand.Leg = 0;
        }

        Discard();
    }

    /// <summary>Somebody is standing on the next tile. Keep it off the next plan only.</summary>
    public void AvoidTile(Point3D tile) => _avoidTile = tile;

    /// <summary>
    /// How many attempts in a row this same tile has refused the bot.
    ///
    /// The reason to count rather than react at once: most things in the way are leaving anyway. A wandering
    /// monster is gone in a step, and redrawing the route for it spends a search to avoid a tile that will be
    /// empty before the plan is finished. Something still there on the second attempt is worth planning
    /// around — that is the difference between a bot standing at an auction and a skeleton walking past.
    /// </summary>
    public int NoteBlocked(Point3D tile)
    {
        if (_blockedTile != tile)
        {
            _blockedTile = tile;
            _blockedCount = 0;
        }

        return ++_blockedCount;
    }

    /// <summary>
    /// This patch of ground nearly killed the bot. Route around it and keep going to the same place.
    ///
    /// The destination is never touched here, and that is the whole of "the goal is untouchable, the way
    /// bends". A bot driven off a road still arrives; it just arrives by another road.
    /// </summary>
    public void AvoidDanger(int x1, int y1, int x2, int y2)
    {
        _dangerX1 = x1;
        _dangerY1 = y1;
        _dangerX2 = x2;
        _dangerY2 = y2;
        _dangerous = true;
        _dangerUntil = Core.TickCount + DangerAvoidMs;

        // The plan in hand was drawn without knowing this, so it may well walk straight back in.
        Discard();
    }

    /// <summary>The next tile to step onto, or false when the plan is done.</summary>
    public bool TryNextTile(out Point3D tile)
    {
        if (_step < _plan.Count)
        {
            tile = _plan[_step];

            return true;
        }

        tile = Point3D.Zero;

        return false;
    }

    /// <summary>
    /// Tiles already behind the bot are dropped. Keeps a plan valid after the bot has been moved by something
    /// other than itself — a diagonal that cut a corner, a shove that happened to help.
    /// </summary>
    public void Catch(Point3D at)
    {
        while (_step < _plan.Count && _plan[_step].X == at.X && _plan[_step].Y == at.Y)
        {
            _step++;
        }
    }

    /// <summary>
    /// An attempt at stepping was made. Called once per try, whether or not it worked, and nowhere else — so
    /// time spent fighting, standing or waiting costs the errand nothing.
    /// </summary>
    public void Attempted() => _attemptsSinceProgress++;

    /// <summary>A step was taken. The only place progress is recorded.</summary>
    public void Stepped(Point3D at)
    {
        _steppedTick = Core.TickCount;
        _stepped = true;

        Catch(at);

        var remaining = Remaining;

        if (_progressStamp != _planStamp || remaining < _bestRemaining)
        {
            _progressStamp = _planStamp;
            _bestRemaining = remaining;
            _attemptsSinceProgress = 0;
            _blockedTile = Point3D.Zero;
            _blockedCount = 0;
        }
    }

    /// <summary>
    /// Whether this errand has stopped going anywhere.
    ///
    /// <b>Only a walking bot is judged.</b> An errand with no plan is a bot standing still on purpose — at a
    /// counter, at a forge, waiting out a cast — and measuring it against a destination it is not currently
    /// travelling to is how the first version banned bots from trading as a punishment for trading.
    /// </summary>
    public bool Stalled() => Active && Walking && _attemptsSinceProgress >= StallAttempts;

    /// <summary>Throws the plan away, keeping the queue. The next tick draws a new one.</summary>
    public void Discard()
    {
        _plan.Clear();
        _step = 0;
        _beside = false;
        _holding = false;
        _planErrand = null;
        _planGoal = Point3D.Zero;
        _planBuiltTick = 0;
        _planStamp++;
        _progressStamp = -1;
        _bestRemaining = int.MaxValue;
        _attemptsSinceProgress = 0;
        _blockedTile = Point3D.Zero;
        _blockedCount = 0;
        _emptyPlans = 0;

        // <b>This line was missing, and its absence made a bot inherit the last errand's despair.</b> Hopeless
        // reads two counters and only one of them was cleared here, so a bot that had spent twelve plans
        // failing to close on somewhere unreachable was declared hopeless about its *next* destination on the
        // first plan it drew — "could not get one tile closer in 1 plans", 148 times in two minutes, on
        // stations and corpses that were perfectly walkable. A reset list is exactly as good as its shortest
        // omission, and nothing about the symptom pointed at the reset.
        _plansSinceCloser = 0;
        _oldSinceCloser = 0;

        Plans = 0;
        Partial = false;
    }

    private void Push(BotErrand errand)
    {
        if (errand?.Map == null)
        {
            return;
        }

        _errands.Add(errand);

        Discard();

        if (_errands.Count <= MaxErrands)
        {
            return;
        }

        // Full. The oldest ordinary errand goes, not the oldest of any kind: an interruption is by definition
        // the thing happening now, and dropping one would leave a bot walking away from a fight it had already
        // decided to have.
        for (var i = 0; i < _errands.Count - 1; i++)
        {
            if (_errands[i].Interruption)
            {
                continue;
            }

            logger.Information("An errand was forgotten to make room: {Errand}", _errands[i]);

            _errands.RemoveAt(i);

            return;
        }

        logger.Information("An errand was forgotten to make room: {Errand}", _errands[0]);

        _errands.RemoveAt(0);
    }

    /// <summary>Distance the way the engine measures adjacency: the larger of the two axis distances.</summary>
    private static int Away(Point3D from, Point3D to)
    {
        var dx = Math.Abs(from.X - to.X);
        var dy = Math.Abs(from.Y - to.Y);

        return dx > dy ? dx : dy;
    }

    private bool Inside(Point3D where) =>
        where.X >= _dangerX1 && where.X <= _dangerX2 && where.Y >= _dangerY1 && where.Y <= _dangerY2;

    public override string ToString() =>
        !Active
            ? "going nowhere"
            : $"{Current}, {Remaining} tiles left of plan {Plans}{(Partial ? ", partial" : "")}{(Queued > 1 ? $", {Queued - 1} waiting" : "")}";
}
