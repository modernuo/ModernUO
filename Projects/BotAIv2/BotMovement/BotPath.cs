using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Engines.Pathing.Cache;
using Server.Logging;
using CalcMoves = Server.Movement.Movement;

namespace Server.BotAI.V2;

/// <summary>What a search concluded. Three answers, and the difference between two of them is load-bearing.</summary>
public enum BotPathOutcome
{
    /// <summary>
    /// Every tile the bot can reach from here was examined, and the goal is not among them.
    ///
    /// <b>A statement about the world, not about the search.</b> There is no way, and nothing whatever is
    /// gained by walking a little way and asking again — the nearest reachable tile to a goal behind a
    /// wall is a tile <em>against the wall</em>, and going there to ask again is exactly the fence-hugging
    /// this whole design exists to end. Returned only when the search can prove it: the open set emptied
    /// and nothing was ever clipped by the search box.
    /// </summary>
    Sealed,

    /// <summary>
    /// The search ran out of its allowance, or out of ground inside its own box, and the path leads to the
    /// best tile it did reach. Walking it is real progress: the next search starts from further along,
    /// with the box re-centred and a fresh allowance.
    /// </summary>
    Partial,

    /// <summary>A walkable path to the goal, tile by tile.</summary>
    Reached
}

/// <summary>What asking about the far side of a journey concluded.</summary>
public enum BotEnclosure
{
    /// <summary>The ground around the destination was walked to its edges. It is a pocket, and it is now filed.</summary>
    Enclosed,

    /// <summary>The ground around the destination kept going past what is worth calling a pocket. Nothing learned.</summary>
    TooBig,

    /// <summary>The probe ran out of its allowance. Nothing learned, and nothing concluded either.</summary>
    NoTime,

    /// <summary>Nothing at or beside the destination will take a body. Nobody can ever arrive, whatever the road.</summary>
    NoFooting,

    /// <summary>Not asked: the population has asked recently enough. Ask again later.</summary>
    Deferred
}

/// <summary>
/// Tile-by-tile A* over the engine's own step masks, bounded by a clock.
///
/// <para>
/// <b>Why a clock and not a tile count.</b> The first version budgeted expansions — twelve thousand of
/// them. Its own measurements say what that costs: 90 006 tiles across 538 searches at 215 ms total is
/// 167 tiles per search at 0.40 ms, so about 2.4 µs a tile, so a search that spent its whole allowance
/// cost something like <b>thirty milliseconds</b>. The average was honest and the worst case was two
/// orders of magnitude worse, which is the shape of a frame that stutters when fifty bots decide to cross
/// the continent in the same tick. Worse, a tile is not a fixed price: on a warm <see cref="StepCache"/>
/// it is a lookup, on a cold one it is a full recompute. Budget the thing that was promised.
/// </para>
///
/// <para>
/// <b>Two properties matter more than speed.</b> A failed search still answers — it has established
/// exactly which tiles it can reach, so finding the gate out of a walled graveyard is not a mechanism
/// with its own ring sweeps and gate memory, it is the ordinary search read correctly. And a refusal is
/// trustworthy, which is what turns "stuck" from a state a bot occupies into an answer it receives.
/// </para>
/// </summary>
public static class BotPath
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPath));

    /// <summary>Cardinal cost, matching the engine's own A* so the two agree on what is shortest.</summary>
    private const int StraightCost = 10;

    private const int DiagonalCost = 14;

    /// <summary>
    /// How far outside the straight line between start and goal the search may look. Wide enough to walk
    /// right round a walled graveyard rather than merely to sidestep a tree, because going the wrong way
    /// first is exactly what leaving an enclosure requires.
    ///
    /// <para>
    /// <b>Raised to the same width as <see cref="MaxMargin"/> on 03.09.2026, by Patrick's order that the
    /// bots be able to cross the whole island and be given knowledge of the rock they cannot pass.</b> The
    /// two are the same request. BotReach learns a pocket only from a search whose ground ran out without
    /// the box ever getting in the way — and the box grew with the length of the journey, so a short one
    /// got twenty-eight tiles. "Behind that rock" is a short journey. The box was smaller than the pocket,
    /// every such search was clipped, and the proof was thrown away every time: on the morning of
    /// 03.09.2026 the shard reported 9013 searches, 6796 of them partial and <b>0 refused outright</b>,
    /// which is to say BotReach had not learned one pocket in a night of running. Bots walked at the same
    /// cliff for hours because nothing on the shard was able to find out that it was a cliff.
    /// </para>
    ///
    /// <para>
    /// The cost is bounded elsewhere and was not the constraint: searches were averaging 5.43ms against a
    /// ceiling of twenty, and the population was spending about 101ms a second of an allowance of 250. A
    /// wider box does not make a search slower — it makes the time already granted buy a better answer, and
    /// where the answer is "there is no way", buys it once for everybody for the life of the shard.
    /// </para>
    /// </summary>
    public static int MinMargin { get; set; } = 256;

    /// <summary>
    /// The widest that box may get, on a long journey.
    ///
    /// <para>
    /// <b>Raised from ninety-six once the cost was actually measured.</b> Ninety-six tiles off the straight
    /// line is enough to round a building and not enough to round a lake, so a bot whose way lay round one
    /// hit the edge of its own search box, came back with a partial plan, walked to the water and asked
    /// again. The box was never the thing keeping the cost down — the clock is, and the clock was idle: 1675
    /// searches came to 256ms over five minutes, which is under one per cent of what the population is
    /// allowed. A wider box does not make a search slower; it makes the time it is given buy a better answer.
    /// </para>
    /// </summary>
    public static int MaxMargin { get; set; } = 256;

    /// <summary>
    /// What one search may cost. The number asked for, and now the number that is actually enforced.
    ///
    /// <para>
    /// Sixty from twenty on 03.09.2026, and by the same order: "their path search works for the whole
    /// island, even if it takes longer to build; they have reflexes to defend themselves on the way". A
    /// search that runs out of clock records nothing, exactly as a clipped one records nothing, so widening
    /// the box without lengthening the clock would only have traded one silent failure for another.
    /// </para>
    /// </summary>
    public static double CeilingMs { get; set; } = 60.0;

    /// <summary>
    /// The largest pocket worth proving from the far side, in standing cells.
    ///
    /// <para>
    /// <b>This is the bound, and it replaces geometry with a count on purpose.</b> A box around the
    /// destination would have to be guessed at, and guessing it small makes every probe clip while guessing
    /// it large makes every failed probe expensive. A count is the thing actually being asked about: a
    /// walled yard is dozens of tiles, a crypt is hundreds, a ledge behind a rock is a handful — and ground
    /// that keeps going past fifteen hundred is not a pocket, it is the world. So a probe that finds the
    /// world stops the moment it has enough tiles to know that, which is the cheapest a wrong guess can be.
    /// </para>
    /// </summary>
    public static int EnclosureCells { get; set; } = 2500;

    /// <summary>
    /// What one look at the far side may cost. Small, because <see cref="EnclosureCells"/> already bounds
    /// the work: the measured price is about forty microseconds an expansion on cold ground, and eight expansions discover up to eight cells apiece, and this
    /// is only here so that a pathological surface cannot make it otherwise.
    /// </summary>
    public static double EnclosureCeilingMs { get; set; } = 30.0;

    /// <summary>
    /// The shortest gap between two looks at the far side, across the whole population.
    ///
    /// A governor rather than a correctness bound. Each look is cheap and each one can buy a refusal that
    /// lasts the life of the shard, so this is deliberately loose — it exists only so that thirty bots all
    /// deciding at once cannot put a spike in a frame.
    /// </summary>
    public static int EnclosureGapMs { get; set; } = 250;

    /// <summary>
    /// The largest pocket worth proving when the bot is standing in it and has been proved stranded.
    ///
    /// <para>
    /// Ten times the ordinary bound, because it is a different question asked on far better evidence. The
    /// ordinary look is a suspicion about somewhere the bot has never been, asked several times a minute, and
    /// it has to be cheap. This one is asked when a dozen roads in a row have been refused from one tile, at
    /// most a few dozen times a night, and what it buys is that no bot is ever sent into that ground again.
    /// </para>
    ///
    /// <para>
    /// It was set at the ordinary bound for an hour and the log says what that cost. Orin the Warrior was
    /// carried out of (1757, 976, 0) on 03.09.2026 at 11:14 and the look reported TooBig — the same ground
    /// that had taken Merrick, Torvin, Kerrin, Perri, Edda 2, Bryn, Ilsa, Calla, Doran and four more in
    /// eighteen minutes that morning. A trap that catches fourteen bots is worth a hundred and fifty
    /// milliseconds once.
    /// </para>
    /// </summary>
    public static int StrandedCells { get; set; } = 25000;

    /// <summary>What that look may cost. Once per rescue, so a tenth of a second is affordable and a night of rescues is not.</summary>
    public static double StrandedCeilingMs { get; set; } = 150.0;

    /// <summary>
    /// The smallest search worth running, for when the population's allowance is spent. Below this a bot
    /// cannot see round anything, and it is better to creep than to stop.
    /// </summary>
    public static double FloorMs { get; set; } = 1.0;

    /// <summary>
    /// What a tile of distance is worth in clock.
    ///
    /// <para>
    /// <b>The ceiling is what a journey across the island may cost, and it was being charged to a chase
    /// three tiles long.</b> A search that reaches its goal is nearly free — it runs almost straight at it —
    /// so the whole bill is the searches that fail, and a failing search spends every millisecond it is
    /// given. With one flat ceiling that is sixty milliseconds whether the goal is over the hill or across
    /// the continent. Measured on 03.09.2026 at 11:05: 69 per cent of searches partial, two thirds of those
    /// going somewhere within thirty-two tiles, half of them ending no nearer than they started, and 469ms a
    /// second against an allowance of five hundred — with 28 per cent of all searches handed less clock than
    /// they asked for, and a starved search gets <see cref="FloorMs"/> and is partial by construction. The
    /// shard was manufacturing its own failures.
    /// </para>
    ///
    /// <para>
    /// Budget the thing that was promised, which is this file's own rule and was being kept only for tiles.
    /// A quarter of a millisecond a tile makes a chase cost four and an island crossing cost sixty, and the
    /// journeys that genuinely need the long search get it back through <see cref="BotWalk.Plan"/>: one that
    /// is not closing asks for the whole ceiling by name.
    /// </para>
    /// </summary>
    public static double MsPerTile { get; set; } = 0.25;

    /// <summary>The least any search gets, however short the journey. Enough to see round a tree.</summary>
    public static double ShortMs { get; set; } = 4.0;

    /// <summary>
    /// What the whole population may spend on searching, per second.
    ///
    /// Not a correctness bound — a governor, and a generous one: the first version measured 215 ms of
    /// searching across ninety seconds, about 2.4 ms a second for fifty bots. Twenty-five times that is
    /// still under a tenth of the game loop, and when it does run out searches shrink to
    /// <see cref="FloorMs"/> and come back <see cref="BotPathOutcome.Partial"/> more often, so the
    /// population walks in shorter hops instead of the world stuttering.
    /// </summary>
    public static double WindowMs { get; set; } = 500.0;

    private const int WindowLengthMs = 1000;

    /// <summary>
    /// How many expansions between glances at the clock. Reading a timestamp is not free, and sixty-four
    /// tiles is a few hundred microseconds at most — fine granularity against a two millisecond ceiling.
    /// </summary>
    private const int ClockEvery = 64;

    private const int InitialNodes = 4096;

    /// <summary>
    /// The smallest pocket worth writing down. Two, because one tile is never useful knowledge and is the
    /// shape an invalid start takes — and a wrong entry in the reach ledger is permanent and silent.
    /// </summary>
    private const int MinPocket = 2;

    // ---- Search state. Reused across searches: one game thread, and a search that allocates is a
    // ---- search that costs more in collection than it does in work.

    private static int[] _nodeX = new int[InitialNodes];
    private static int[] _nodeY = new int[InitialNodes];
    private static sbyte[] _nodeZ = new sbyte[InitialNodes];
    private static int[] _nodeCost = new int[InitialNodes];
    private static int[] _nodeTotal = new int[InitialNodes];
    private static int[] _nodeParent = new int[InitialNodes];
    private static bool[] _nodeClosed = new bool[InitialNodes];

    private static int _nodeCount;

    private static readonly Dictionary<int, int> _lookup = [];

    private static readonly PriorityQueue<int, int> _open = new();

    private static readonly Dictionary<long, bool> _blockedByItems = [];

    private static readonly List<Point3D> _reversed = [];

    /// <summary>A scratch path, for callers who only want a yes or no.</summary>
    private static readonly List<Point3D> _scratch = [];

    // ---- The memo: the very same search asked again within seconds (29.09.2026). ----

    /// <summary>
    /// How long a search's answer is handed out again for the very same question, in milliseconds: the same tile, the same
    /// target, the same arrival, the same ceiling, no exclusions and doors as doors. Two seconds, five to ten beats.
    ///
    /// <para>
    /// <b>Why it is safe.</b> The search is deterministic in what it is given: asked again from the same tile for the same
    /// target with the same ceiling, it expands the same cells in the same order and draws the same path, unless the world
    /// changed under it in those two seconds — a door, a crate — and then the walker's step is refused and handled as any
    /// refusal is (a door opened, an occupant asked, a tile avoided — and an avoided tile is an exclusion, which this memo
    /// never answers). What changes the question changes the key: a step, a moved target, the escalated ceiling after a
    /// plan that got no closer. A search starved of clock by the population's window is not kept, so a full answer is never
    /// replaced by a starved one; a refusal (<see cref="BotPathOutcome.Sealed"/>) is not kept, since the pocket ledger
    /// already answers it for nothing.
    /// </para>
    ///
    /// <para>
    /// <b>Why it is wanted.</b> A walker whose plan comes back with nowhere to walk improvises a step and, if that fails too,
    /// plans again from the same tile: the same search, burned again. The summary of 29.09.2026 18:53 named "the whole
    /// ceiling burned by closing up on Mirthal" 160 times in fifteen minutes, sixty milliseconds each; 263 of 386 partials
    /// "ended no nearer than they started". Set to nought to turn it off.
    /// </para>
    /// </summary>
    public static int MemoMs { get; set; } = 2000;

    /// <summary>Searches answered from the memo, and the milliseconds the searches they repeat had cost.</summary>
    public static long Recalled { get; private set; }

    public static double RecalledMs { get; private set; }

    /// <summary>How many answers the memo holds before the stale ones are swept out.</summary>
    private const int MemoSweep = 256;

    private readonly record struct MemoKey(int Map, Point3D From, Point3D To, int Tiles, double Ceiling);

    private sealed class Memo
    {
        public readonly List<Point3D> Path = [];

        public long At;

        public BotPathOutcome Outcome;

        public double Ms;
    }

    private static readonly Dictionary<MemoKey, Memo> _memo = [];

    private static readonly Stack<Memo> _memoFree = new();

    private static readonly List<MemoKey> _memoStale = [];

    /// <summary>The answer to this very question, if one was given within <see cref="MemoMs"/>.</summary>
    private static bool Recall(in MemoKey key, List<Point3D> path, out BotPathOutcome outcome)
    {
        outcome = BotPathOutcome.Partial;

        if (!_memo.TryGetValue(key, out var memo) || Core.TickCount - memo.At >= MemoMs)
        {
            return false;
        }

        path.AddRange(memo.Path);
        outcome = memo.Outcome;
        Recalled++;
        RecalledMs += memo.Ms;

        return true;
    }

    /// <summary>Keeps a search's answer for <see cref="MemoMs"/>, sweeping out the stale ones when there are many.</summary>
    private static void Keep(in MemoKey key, BotPathOutcome outcome, List<Point3D> path, double ms)
    {
        if (_memo.Count >= MemoSweep)
        {
            _memoStale.Clear();

            foreach (var (k, m) in _memo)
            {
                if (Core.TickCount - m.At >= MemoMs)
                {
                    _memoStale.Add(k);
                }
            }

            for (var i = 0; i < _memoStale.Count; i++)
            {
                if (_memo.Remove(_memoStale[i], out var gone) && _memoFree.Count < MemoSweep)
                {
                    _memoFree.Push(gone);
                }
            }

            // Still full of fresh answers: a crowd; the memo starts over rather than grow.
            if (_memo.Count >= MemoSweep * 4)
            {
                _memo.Clear();
            }
        }

        if (!_memo.TryGetValue(key, out var memo))
        {
            memo = _memoFree.Count > 0 ? _memoFree.Pop() : new Memo();
            _memo[key] = memo;
        }

        memo.Path.Clear();
        memo.Path.AddRange(path);
        memo.At = Core.TickCount;
        memo.Outcome = outcome;
        memo.Ms = ms;
    }

    /// <summary>The flood's frontier, as node indices. Order does not matter: it is exhausting, not aiming.</summary>
    private static readonly List<int> _frontier = [];

    // ---- The population's allowance.

    private static long _windowEnds;

    private static bool _windowStarted;

    private static double _spentThisWindow;

    // ---- What it has all cost. For the summary.

    public static long Searches { get; private set; }

    public static long TilesExamined { get; private set; }

    /// <summary>Steps never examined for landing on ground the reach map says the walker cannot reach, and the searches that left some out. See <see cref="BotReachMap.PrunesSearch"/>.</summary>
    public static long Pruned { get; private set; }

    public static long PrunedSearches { get; private set; }

    public static long Reached { get; private set; }

    public static long PartialRuns { get; private set; }

    /// <summary>Proofs of a pocket thrown away, by the condition that threw them. See the note in the search.</summary>
    public static long LostToClock { get; private set; }

    public static long LostToBox { get; private set; }

    public static long LostToAvoiding { get; private set; }

    public static long LostToDoors { get; private set; }

    public static long LostToSize { get; private set; }

    /// <summary>Whether the goal was found. Named so the veto tally above reads as prose.</summary>
    private static bool Reached2(int reached) => reached >= 0;

    public static long SealedRuns { get; private set; }

    public static double TotalMs { get; private set; }

    public static double WorstMs { get; private set; }

    // ---- What the partials actually are. Four numbers, because "84 per cent partial" is a symptom that has
    // ---- two completely different causes and tuning the wrong one is how a governor gets raised for ever.

    /// <summary>
    /// Searches handed less clock than they asked for, because the population's second was already spent.
    ///
    /// The number that says whether the governor is a ceiling nobody touches or a wall everybody is against.
    /// A starved search gets <see cref="FloorMs"/>, which buys a few hundred tiles, which comes back partial —
    /// so once this starts climbing the shard is making its own partials.
    /// </summary>
    public static long Starved { get; private set; }

    /// <summary>
    /// Whether the last search was handed less clock than it asked for. A starved search is partial by
    /// construction, and a caller about to give a destination up on the strength of one partial has to be
    /// able to tell "no way round" from "no time to look". Single-threaded, so one flag is enough.
    /// </summary>
    public static bool LastStarved { get; private set; }

    /// <summary>What the last search cost, for the event that reports it. Nought for a search answered from the pocket record.</summary>
    public static double LastMs { get; private set; }

    public static long LastExpansions { get; private set; }

    /// <summary>
    /// Searches given the whole ceiling by name, because the journey asking had stopped closing.
    ///
    /// The other half of charging by distance. A goal twenty tiles off whose road runs four hundred tiles
    /// round a lake is a short journey by every measure this file has, and it is exactly the one that needs
    /// the long search — so the cheap search is tried first and the expensive one is bought only where the
    /// cheap one has already been shown to fail.
    /// </summary>
    public static long Lengthened { get; private set; }

    /// <summary>Partial searches to somewhere inside <see cref="Near"/> tiles. A chase, not a journey.</summary>
    public static long PartialNear { get; private set; }

    /// <summary>
    /// Partial searches that ended no nearer the goal than they began.
    ///
    /// The signature of something in the way rather than something far off: A* aims at the goal, so a search
    /// that cannot better its own starting distance has been stopped, not slowed.
    /// </summary>
    public static long PartialStill { get; private set; }

    /// <summary>How far the partials were going, added up, so the average can be printed.</summary>
    public static long PartialSpan { get; private set; }

    /// <summary>
    /// Partials that never began: the bot's own tile forbids all eight directions.
    ///
    /// Counted apart because it is the one partial that is not about the destination at all, and because it
    /// leaves the other three counters — a search that returns before the loop adds nothing to them, so
    /// without this the breakdown quietly fails to add up and the difference is invisible.
    /// </summary>
    public static long PartialUnfooted { get; private set; }

    /// <summary>What counts as near enough that a full search of the box is not what was wanted.</summary>
    public const int Near = 32;

    // ---- What asking about the far side has cost, and bought.

    /// <summary>Looks at the far side of a journey that actually ran.</summary>
    public static long Probes { get; private set; }

    /// <summary>Looks that ended in a pocket being filed.</summary>
    public static long Enclosed { get; private set; }

    /// <summary>Looks that ran out of ground worth calling a pocket. The ordinary answer, and a cheap one.</summary>
    public static long ProbedTooBig { get; private set; }

    /// <summary>Looks that ran out of clock. Should be rare; if it is not, the cell bound is doing nothing.</summary>
    public static long ProbedNoTime { get; private set; }

    /// <summary>
    /// Destinations with nowhere to stand at or beside them.
    ///
    /// Its own kind of hopeless, and until now an invisible one: no road can end at a tile that will not
    /// take a body, so a bot sent to one walks perfectly well for ever and never arrives.
    /// </summary>
    public static long ProbedNoFooting { get; private set; }

    public static double ProbeMs { get; private set; }

    public static long ProbeTiles { get; private set; }

    /// <summary>
    /// Ground the looks found, in standing cells.
    ///
    /// Kept apart from <see cref="ProbeTiles"/> because the two answer different questions and the wrong one
    /// was reported first. Tiles are nodes taken off the frontier; cells are ground discovered, and it is
    /// cells that <see cref="EnclosureCells"/> is measured in — one expansion discovers up to eight of them,
    /// so a look can settle "this is the world" having popped two hundred nodes.
    /// </summary>
    public static long ProbeCells { get; private set; }

    private static long _probedAt;

    private static bool _probeStarted;

    public static void Reset()
    {
        _memo.Clear();
        Recalled = 0;
        RecalledMs = 0.0;
        Searches = 0;
        TilesExamined = 0;
        Reached = 0;
        PartialRuns = 0;
        LostToClock = 0;
        LostToBox = 0;
        LostToAvoiding = 0;
        LostToDoors = 0;
        LostToSize = 0;
        SealedRuns = 0;
        TotalMs = 0.0;
        WorstMs = 0.0;
        Starved = 0;
        Lengthened = 0;
        PartialNear = 0;
        PartialStill = 0;
        PartialSpan = 0;
        PartialUnfooted = 0;
        Probes = 0;
        Enclosed = 0;
        ProbedTooBig = 0;
        ProbedNoTime = 0;
        ProbedNoFooting = 0;
        ProbeMs = 0.0;
        ProbeTiles = 0;
        ProbeCells = 0;
        _probeStarted = false;
        _spentThisWindow = 0.0;
        _windowStarted = false;
    }

    public static string Describe() =>
        Searches == 0
            ? "no searches yet"
            : $"{Searches} searches, {TilesExamined} tiles examined ({Pruned} steps left unexamined for landing on ground the reach map says the walker cannot reach, in {PrunedSearches} searches), {TotalMs:F0}ms total ({TotalMs / Searches:F2}ms each, worst {WorstMs:F2}ms), {Reached} reached, {PartialRuns} partial, {SealedRuns} refused outright; {Starved} were handed less clock than they asked for and {Lengthened} asked for the whole ceiling because they were not closing; of the partials {PartialNear} were going somewhere within {Near} tiles and {PartialStill} ended no nearer than they started and {PartialUnfooted} never began because the bot's own tile forbids every direction, the average one {(PartialRuns > PartialUnfooted ? PartialSpan / (PartialRuns - PartialUnfooted) : 0)} tiles out; proofs of a pocket lost: {LostToClock} to the clock, {LostToBox} to the box, {LostToAvoiding} to avoiding danger, {LostToDoors} to shut doors, {LostToSize} too small to be one; {Probes} looks at the far side costing {ProbeMs:F0}ms over {ProbeTiles} expansions across {ProbeCells} cells of ground: {Enclosed} found a pocket, {ProbedTooBig} found the world, {ProbedNoTime} ran out of clock, {ProbedNoFooting} found nowhere at all to stand; {Recalled} asked again within {MemoMs / 1000.0:0.#}s of the same search and answered from it ({RecalledMs:F0}ms of searching not done again)";

    /// <summary>
    /// Whether there is a way at all, without keeping the path. For vetting a candidate before committing
    /// to it — a place to wander to, a rally point, a spot to retreat onto.
    /// </summary>
    public static bool CanReach(Map map, Point3D from, Point3D to, BotArrival arrival, double ceilingMs = 0.0) =>
        Find(map, from, to, arrival, _scratch, ceilingMs: ceilingMs) == BotPathOutcome.Reached;

    /// <summary>
    /// Whether there is a way there <em>without going through a door</em> — which is how "is that spot out
    /// on the street" gets answered.
    ///
    /// Without it, a caller asking for somewhere outdoors is handed a point in somebody's pantry: a shut
    /// door is passable to the planner by design, so the inside of every building in the world is
    /// reachable and therefore a candidate. The distinction has to be a separate question rather than a
    /// different rule, because both answers are wanted, about the same tile, by different callers.
    ///
    /// Nothing calls this yet. It exists now because the alternative was a parameter on
    /// <see cref="BotStep.BlockedByItems"/> with no way to reach it from a search — plumbing that looks
    /// finished and is not.
    /// </summary>
    public static bool ReachableWithDoorsShut(Map map, Point3D from, Point3D to, BotArrival arrival) =>
        Find(map, from, to, arrival, _scratch, doorsShut: true) == BotPathOutcome.Reached;

    /// <summary>
    /// Finds a way from <paramref name="from"/> to <paramref name="to"/> and writes it into
    /// <paramref name="path"/> as the tiles to walk, the bot's own tile excluded.
    ///
    /// On <see cref="BotPathOutcome.Partial"/> the path leads to the nearest point to the goal that
    /// <em>can</em> be reached, so a caller that simply walks whatever comes back behaves correctly either
    /// way: it arrives, or it gets as close as the world permits and asks again from there — which, from
    /// further out, may well succeed.
    /// </summary>
    public static BotPathOutcome Find(
        Map map,
        Point3D from,
        Point3D to,
        BotArrival arrival,
        List<Point3D> path,
        BotAvoid avoid = default,
        double ceilingMs = 0.0,
        bool doorsShut = false
    )
    {
        path.Clear();
        LastMs = 0.0;
        LastExpansions = 0;

        if (map == null || map == Map.Internal)
        {
            return BotPathOutcome.Sealed;
        }

        if (arrival.Reached(from, to))
        {
            return BotPathOutcome.Reached;
        }

        // Free refusal, from a pocket somebody else already walked to its edges. The most expensive
        // question there is, answered by a dictionary lookup.
        if (BotReach.Ask(map, from, to, arrival) == BotReachVerdict.Sealed)
        {
            // Counted as a search, because it answered one. Left out, the summary could report more
            // refusals than searches and divide the total time by the wrong denominator — and a system
            // whose entire premise is that measurement beats guessing cannot afford a lying counter.
            Searches++;
            SealedRuns++;

            return BotPathOutcome.Sealed;
        }

        // The very same question asked within seconds: the same answer, without the search. See MemoMs.
        var memoKey = new MemoKey(map.MapID, from, to, arrival.Tiles, ceilingMs);
        var memoable = MemoMs > 0 && avoid.Empty && !doorsShut;

        if (memoable && Recall(memoKey, path, out var recalled))
        {
            LastStarved = false;

            return recalled;
        }

        // One pathfind, as far as the step cache's promotion gate is concerned. It counts distinct
        // searches rather than lookups, so without this a single search's thousands of probes read as
        // thousands of separate visits and the gate builds chunks nothing revisits.
        StepCache.Instance.BeginFindGeneration();

        Searches++;

        var started = Stopwatch.GetTimestamp();

        var span = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));

        if (ceilingMs > 0.0)
        {
            Lengthened++;
        }

        var wanted = ceilingMs > 0.0 ? ceilingMs : Math.Clamp(span * MsPerTile, ShortMs, CeilingMs);
        var allowanceMs = Allowance(wanted);
        var deadline = started + (long)(allowanceMs * Stopwatch.Frequency / 1000.0);

        LastStarved = allowanceMs < wanted;

        if (LastStarved)
        {
            Starved++;
        }

        var margin = Math.Clamp(span / 2, MinMargin, MaxMargin);

        var minX = Math.Max(0, Math.Min(from.X, to.X) - margin);
        var minY = Math.Max(0, Math.Min(from.Y, to.Y) - margin);
        var maxX = Math.Min(map.Width - 1, Math.Max(from.X, to.X) + margin);
        var maxY = Math.Min(map.Height - 1, Math.Max(from.Y, to.Y) + margin);

        _lookup.Clear();
        _open.Clear();
        _blockedByItems.Clear();
        _nodeCount = 0;

        var startZ = (sbyte)Math.Clamp(from.Z, sbyte.MinValue, sbyte.MaxValue);

        // Where the bot is standing has to be somewhere it could stand, and the caller is not obliged to
        // have checked.
        //
        // A start whose mask forbids all eight directions makes the search empty its open set on the first
        // expansion — which looks exactly like a pocket of ground walked to its edges, and would be written
        // into the reach ledger as one. A one-tile pocket is never knowledge worth having, and this one
        // would be poison: it seals that cell, and every other height sharing its band, against every
        // journey for the rest of the shard's life. So an immovable start is reported as no progress rather
        // than as a fact about the world, and the walker asks the engine directly instead.
        if (BotStep.Mask(map, from.X, from.Y, startZ).WalkMask == 0)
        {
            PartialRuns++;
            PartialUnfooted++;

            return BotPathOutcome.Partial;
        }

        // <b>No step onto ground the reach map says this walker cannot reach (01.10.2026, build 388; Patrick's plan v2, point 6).</b>
        // From open ground every neighbour that is not open ground — a roof, a sealed yard, an isle across a ford the map knows
        // of, water — is left unexamined rather than expanded and found wanting; from a deck, every neighbour that is not
        // water. Nothing while the map cannot say, or while its own check runs, or for a walker standing somewhere the map
        // calls neither. Counted, so the saving is a number.
        var want = BotReachClass.Unknown;
        var pruning = BotReachMap.PrunesSearch && BotReachMap.Standing(map, from, out want);
        var pruned = 0L;

        var start = AddNode(from.X, from.Y, startZ, 0, -1);

        _nodeTotal[start] = Heuristic(from.X, from.Y, to);
        _lookup[BotStep.Cell(from.X, from.Y, startZ)] = start;
        _open.Enqueue(start, _nodeTotal[start]);

        // The best thing found short of the goal, kept as the search runs. A* explores towards the goal,
        // so this is genuinely the closest reachable point rather than the first one tried.
        var nearest = -1;
        var nearestScore = int.MaxValue;

        // <b>And the best of it outside the reach of what the walker has seen (03.10.2026, sight13).</b> A search that comes back
        // short ends its plan at the node nearest the goal, and with a creature's reach priced (BotAvoid.Reaches) that node is
        // as likely as not inside it: the partial plan would walk the bot to the edge of the very thing it was drawn round. Kept
        // apart, and preferred when there is one.
        var reaching = avoid.HasReaches;
        var nearestOut = -1;
        var nearestOutScore = int.MaxValue;

        var reached = -1;
        var expansions = 0;
        var outOfTime = false;

        // Whether the search box ever stopped the search from looking somewhere.
        //
        // The single most important flag in this file. Without it, "the open set emptied" is ambiguous
        // between "the bot can reach nothing else in the world" and "the bot can reach nothing else
        // inside this rectangle" — and recording the second as a sealed pocket would wall off half a
        // continent, permanently, with nothing in any log to say so.
        var clipped = false;

        while (_open.Count > 0)
        {
            if ((expansions & (ClockEvery - 1)) == 0 && Stopwatch.GetTimestamp() >= deadline)
            {
                outOfTime = true;
                break;
            }

            if (!_open.TryDequeue(out var current, out var priority))
            {
                break;
            }

            // Lazily deleted duplicate: a cheaper route to this cell was found after it was queued.
            if (_nodeClosed[current] || _nodeTotal[current] != priority)
            {
                continue;
            }

            _nodeClosed[current] = true;
            expansions++;

            var cx = _nodeX[current];
            var cy = _nodeY[current];
            var cz = _nodeZ[current];

            if (arrival.Reached(new Point3D(cx, cy, cz), to))
            {
                reached = current;
                break;
            }

            var score = Heuristic(cx, cy, to);

            if (score < nearestScore)
            {
                nearestScore = score;
                nearest = current;
            }

            if (reaching && score < nearestOutScore && !avoid.InReach(cx, cy))
            {
                nearestOutScore = score;
                nearestOut = current;
            }

            var mask = BotStep.Mask(map, cx, cy, cz);
            var walk = mask.WalkMask;

            if (walk == 0)
            {
                continue;
            }

            for (var d = 0; d < 8; d++)
            {
                if ((walk & (1 << d)) == 0)
                {
                    continue;
                }

                // The diagonal rule, and it needs both of its halves.
                //
                // The engine will not let a player cut a corner unless BOTH flanking tiles pass the whole
                // movement check — terrain and items. The first version calls getting this half-right the
                // most expensive mistake in its pathfinder, and it can be got half-right in either
                // direction: check only the terrain and every squeeze past a headstone fails at the moment
                // of stepping; check only the items and the planner emits diagonals through wall corners
                // that the engine then refuses. Both gates, in this order.
                if ((d & 1) == 1)
                {
                    var left = (d + 7) & 7;
                    var right = (d + 1) & 7;

                    // Mask bits first, and not merely for speed: GetWalkZ for a direction the mask forbids
                    // is a landing height for a step that does not exist, and FlankBlocked would then be
                    // asking about the wrong tile at the wrong height.
                    if ((walk & (1 << left)) == 0 || (walk & (1 << right)) == 0)
                    {
                        continue;
                    }

                    if (FlankBlocked(map, cx, cy, left, mask, doorsShut) || FlankBlocked(map, cx, cy, right, mask, doorsShut))
                    {
                        continue;
                    }
                }

                var nx = cx;
                var ny = cy;

                CalcMoves.Offset((Direction)d, ref nx, ref ny);

                if (nx < minX || ny < minY || nx > maxX || ny > maxY)
                {
                    clipped = true;
                    continue;
                }

                if (avoid.Blocks(nx, ny))
                {
                    continue;
                }

                var nz = mask.GetWalkZ((Direction)d);

                if (pruning)
                {
                    var kind = BotReachMap.Class(map, nx, ny, nz);

                    if (kind != BotReachClass.Unknown && kind != want)
                    {
                        pruned++;

                        continue;
                    }
                }

                // Items, which the mask does not know about. This world is built out of them — the
                // graveyard railing, the headstones, the crates are all items — so a planner without this
                // draws confident straight lines through the very fences it exists to see.
                if (Blocked(map, nx, ny, nz, doorsShut))
                {
                    continue;
                }

                var cell = BotStep.Cell(nx, ny, nz);
                var stepCost = (d & 1) == 1 ? DiagonalCost : StraightCost;

                // A step into the reach of something the walker cannot beat costs more, so the way round is taken where it is
                // not dear. Only ever added to, so the estimate below stays a lower bound. See BotAvoid.Reaches.
                var cost = _nodeCost[current] + stepCost + (reaching ? avoid.Penalty(nx, ny) : 0);

                if (_lookup.TryGetValue(cell, out var existing))
                {
                    if (_nodeClosed[existing] || cost >= _nodeCost[existing])
                    {
                        continue;
                    }

                    _nodeCost[existing] = cost;
                    _nodeParent[existing] = current;
                    _nodeTotal[existing] = cost + Heuristic(nx, ny, to);
                    _open.Enqueue(existing, _nodeTotal[existing]);

                    continue;
                }

                var node = AddNode(nx, ny, nz, cost, current);

                _nodeTotal[node] = cost + Heuristic(nx, ny, to);
                _lookup[cell] = node;
                _open.Enqueue(node, _nodeTotal[node]);
            }
        }

        var elapsedMs = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

        Spend(elapsedMs);

        TotalMs += elapsedMs;
        TilesExamined += expansions;
        LastMs = elapsedMs;
        LastExpansions = expansions;

        if (pruned > 0)
        {
            Pruned += pruned;
            PrunedSearches++;
        }

        if (elapsedMs > WorstMs)
        {
            WorstMs = elapsedMs;
        }

        if (reached >= 0)
        {
            Rebuild(reached, path);
            Reached++;

            if (memoable && !LastStarved)
            {
                Keep(memoKey, BotPathOutcome.Reached, path, elapsedMs);
            }

            return BotPathOutcome.Reached;
        }

        // <b>Why a proof was thrown away, named rather than counted as one number.</b> A search that empties
        // its open set has enumerated a pocket and can hand BotReach the most expensive fact on the shard
        // for nothing — but four different conditions veto that, and until 03.09.2026 all four looked
        // identical from outside: "0 refused outright", every window, all night. Widening the box turned a
        // quarter of searches reaching their goal into more than half and still left that nought, so the box
        // was not the only veto. These say which one it is, in the same sentence as the count they explain.
        if (!Reached2(reached))
        {
            if (outOfTime)
            {
                LostToClock++;
            }
            else if (clipped)
            {
                LostToBox++;
            }
            else if (!avoid.Empty)
            {
                LostToAvoiding++;
            }
            else if (doorsShut)
            {
                LostToDoors++;
            }
            else if (_lookup.Count < MinPocket)
            {
                LostToSize++;
            }
        }

        // Ground genuinely exhausted: the open set emptied, the clock had time left, the box never got in
        // the way, and no tactical exclusion was distorting the answer. Only then is this knowledge.
        if (!outOfTime && !clipped && avoid.Empty)
        {
            // A search run with the doors treated as walls has mapped a room, not a pocket of the world.
            // Recording it would seal every building on the shard against everybody outside it.
            if (!doorsShut && _lookup.Count >= MinPocket)
            {
                BotReach.Record(map, _lookup.Keys, from);
            }

            SealedRuns++;

            return BotPathOutcome.Sealed;
        }

        if (reaching && nearestOut >= 0 && nearestOut != start)
        {
            nearest = nearestOut;
            nearestScore = nearestOutScore;
        }

        if (nearest >= 0 && nearest != start)
        {
            Rebuild(nearest, path);
        }

        PartialRuns++;
        PartialSpan += span;

        if (span <= Near)
        {
            PartialNear++;
        }

        // Measured against where it began rather than against the goal: the start's own estimate is the
        // number to beat, and beating it by nothing is the whole finding.
        if (nearest < 0 || nearestScore >= Heuristic(from.X, from.Y, to))
        {
            PartialStill++;
        }

        if (memoable && !LastStarved)
        {
            Keep(memoKey, BotPathOutcome.Partial, path, elapsedMs);
        }

        return BotPathOutcome.Partial;
    }

    /// <summary>
    /// Whether the destination sits in a pocket of ground, asked <b>from the destination's side</b>.
    ///
    /// <para>
    /// <b>Why the question has to be turned round, and why no clock could have answered it the other way.</b>
    /// A search refuses a journey only when its open set empties with nothing clipped — which is to say when
    /// it has enumerated the whole pocket it started in. Started from the bot, that pocket is the mainland.
    /// The box is two hundred and fifty-six tiles beyond the line to the goal, open ground carries on past
    /// it, so a neighbour falls outside the box and <c>clipped</c> is set long before the open set can empty.
    /// The clock happens to run out first today, which is why every lost proof was filed against it; give the
    /// search six times the clock and the same proofs are lost to the box instead. Neither is the reason.
    /// <b>The reason is that the bot is standing on the wrong side of the wall to prove anything about it.</b>
    /// On the morning of 03.09.2026 that showed as 3564 proofs lost, 0 kept, in a ten-minute window — and it
    /// would have shown the same in any window, at any ceiling, for the life of the shard.
    /// </para>
    ///
    /// <para>
    /// From the destination it is a different question and a cheap one. The ledge behind the rock, the walled
    /// yard, the islet, the crypt: each is a few dozen to a few hundred tiles, so the flood either runs out of
    /// ground almost at once — which is the proof, bought for everybody for the life of the shard — or it runs
    /// past <see cref="EnclosureCells"/> and stops, which costs a few milliseconds and settles
    /// that this destination is simply far away rather than walled off.
    /// </para>
    ///
    /// <para>
    /// <b>What it actually proves, said plainly.</b> The flood follows steps the engine would allow, outwards
    /// from the destination, so what it establishes is that <em>nothing standing there can get out</em>.
    /// Stepping in might still be possible where the world allows a drop it will not allow back up. That is
    /// not a hole in the reasoning, it is the answer: somewhere a bot could fall into and never leave is
    /// somewhere it should not be sent, and this whole subsystem exists because bots were being sent to places
    /// they never arrived at.
    /// </para>
    /// </summary>
    public static BotEnclosure Enclose(Map map, Point3D goal, BotArrival arrival, bool urgent = false)
    {
        if (map == null || map == Map.Internal)
        {
            return BotEnclosure.Deferred;
        }

        var now = Core.TickCount;

        // The gap is there to stop thirty bots asking at once about thirty different destinations. It is not
        // there to stop the one question that is asked on proof rather than on suspicion: a bot with a dozen
        // roads refused in a row is standing in the pocket, and that is the cheapest and surest look there is.
        if (!urgent && _probeStarted && now - _probedAt < EnclosureGapMs)
        {
            return BotEnclosure.Deferred;
        }

        _probeStarted = true;
        _probedAt = now;

        Probes++;

        // Somewhere at the destination that would take a body. The Z a caller hands in is where something is
        // standing or floating, not necessarily a floor, and a flood begun at a height with no ground under it
        // maps nothing and would file the emptiness as a pocket.
        if (!Footing(map, goal, arrival, out var start))
        {
            ProbedNoFooting++;

            return BotEnclosure.NoFooting;
        }

        var started = Stopwatch.GetTimestamp();

        // <b>Its own clock, and deliberately not the population's.</b>
        //
        // The first hour of this running had four looks in five minutes and two of them came back having run
        // out of time after two hundred tiles. They had been handed <see cref="FloorMs"/>: Allowance divides
        // what is left of the window, the window was drained, and it was drained by exactly the failing
        // searches this look exists to stop. An instrument paid for out of the waste it ends cannot run when
        // the waste is worst, which is the only time it is wanted.
        //
        // What keeps it bounded instead is <see cref="EnclosureGapMs"/>, and that bound is a harder one: four
        // looks a second at twenty-five milliseconds apiece is a hundred milliseconds a second in the very
        // worst case, no matter what else the population is doing. The cost is still handed to Spend, because
        // it is real and belongs in the window's accounting — it simply is not gated by it.
        var ceiling = urgent ? StrandedCeilingMs : EnclosureCeilingMs;
        var cells = urgent ? StrandedCells : EnclosureCells;
        var deadline = started + (long)(ceiling * Stopwatch.Frequency / 1000.0);

        StepCache.Instance.BeginFindGeneration();

        _lookup.Clear();
        _blockedByItems.Clear();
        _frontier.Clear();
        _nodeCount = 0;

        var startZ = (sbyte)Math.Clamp(start.Z, sbyte.MinValue, sbyte.MaxValue);
        var root = AddNode(start.X, start.Y, startZ, 0, -1);

        _lookup[BotStep.Cell(start.X, start.Y, startZ)] = root;
        _frontier.Add(root);

        var expansions = 0;
        var outcome = BotEnclosure.Enclosed;

        while (_frontier.Count > 0)
        {
            if ((expansions & (ClockEvery - 1)) == 0 && Stopwatch.GetTimestamp() >= deadline)
            {
                outcome = BotEnclosure.NoTime;

                break;
            }

            // The bound, and the only one. Ground that keeps going past this is the world, not a pocket, and
            // the cheapest way to find that out is to stop counting as soon as the count settles it.
            if (_lookup.Count > cells)
            {
                outcome = BotEnclosure.TooBig;

                break;
            }

            var current = _frontier[^1];

            _frontier.RemoveAt(_frontier.Count - 1);
            expansions++;

            var cx = _nodeX[current];
            var cy = _nodeY[current];
            var cz = _nodeZ[current];

            var mask = BotStep.Mask(map, cx, cy, cz);
            var walk = mask.WalkMask;

            if (walk == 0)
            {
                continue;
            }

            for (var d = 0; d < 8; d++)
            {
                if ((walk & (1 << d)) == 0)
                {
                    continue;
                }

                // The same two-part diagonal rule the planner uses, and here it is a safety property rather
                // than an efficiency one. A flood stricter than the engine sees a smaller pocket than there is
                // and would file ground that is genuinely open — the one mistake in this file that is silent,
                // permanent, and worse than the problem it solves.
                if ((d & 1) == 1)
                {
                    var left = (d + 7) & 7;
                    var right = (d + 1) & 7;

                    if ((walk & (1 << left)) == 0 || (walk & (1 << right)) == 0)
                    {
                        continue;
                    }

                    if (FlankBlocked(map, cx, cy, left, mask, doorsShut: false)
                        || FlankBlocked(map, cx, cy, right, mask, doorsShut: false))
                    {
                        continue;
                    }
                }

                var nx = cx;
                var ny = cy;

                CalcMoves.Offset((Direction)d, ref nx, ref ny);

                // The edge of the map is a wall the world itself puts there, so a pocket that ends against it
                // has genuinely ended. No box of our own: EnclosureCells is the bound.
                if (nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height)
                {
                    continue;
                }

                var nz = mask.GetWalkZ((Direction)d);

                if (Blocked(map, nx, ny, nz, doorsShut: false))
                {
                    continue;
                }

                var cell = BotStep.Cell(nx, ny, nz);

                if (_lookup.ContainsKey(cell))
                {
                    continue;
                }

                var node = AddNode(nx, ny, nz, 0, current);

                _lookup[cell] = node;
                _frontier.Add(node);
            }
        }

        var elapsedMs = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

        Spend(elapsedMs);

        ProbeMs += elapsedMs;
        ProbeTiles += expansions;
        ProbeCells += _lookup.Count;

        if (outcome == BotEnclosure.TooBig)
        {
            ProbedTooBig++;

            return outcome;
        }

        if (outcome == BotEnclosure.NoTime)
        {
            ProbedNoTime++;

            return outcome;
        }

        if (_lookup.Count < MinPocket)
        {
            // One tile. Never knowledge worth having, and the shape an invalid footing takes.
            ProbedTooBig++;

            return BotEnclosure.TooBig;
        }

        BotReach.Record(map, _lookup.Keys, start);
        Enclosed++;

        return BotEnclosure.Enclosed;
    }

    /// <summary>
    /// Ground at or beside the destination that would take a body, and the height it would stand at.
    ///
    /// <para>
    /// The destination's own height is preferred whenever a body could stand there, because that is the one
    /// height known to belong to the thing being walked to. Only when it will not does this settle for the
    /// surface underneath, and then only if that surface is within a person's height of what was asked for —
    /// a destination on a bridge whose settled ground is the riverbed twenty units below is not the same
    /// place, and flooding the riverbed would file a pocket that has nothing to do with the journey.
    /// </para>
    /// </summary>
    private static bool Footing(Map map, Point3D goal, BotArrival arrival, out Point3D at)
    {
        var goalZ = (sbyte)Math.Clamp(goal.Z, sbyte.MinValue, sbyte.MaxValue);

        if (BotStep.Mask(map, goal.X, goal.Y, goalZ).WalkMask != 0)
        {
            at = new Point3D(goal.X, goal.Y, goalZ);

            return true;
        }

        if (BotStep.Settle(map, goal.X, goal.Y, out var z) && Math.Abs(z - goal.Z) <= BotArrival.PersonHeight)
        {
            at = new Point3D(goal.X, goal.Y, z);

            return true;
        }

        // Nowhere on the tile itself. The arrival tolerance says where else would have counted as arriving,
        // so those tiles are the rest of the destination and are asked about in rings, nearest first.
        var reach = Math.Min(arrival.Tiles, MaxFootingSweep);

        for (var r = 1; r <= reach; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    if (BotStep.Settle(map, goal.X + dx, goal.Y + dy, out var rz)
                        && Math.Abs(rz - goal.Z) <= BotArrival.PersonHeight)
                    {
                        at = new Point3D(goal.X + dx, goal.Y + dy, rz);

                        return true;
                    }
                }
            }
        }

        at = Point3D.Zero;

        return false;
    }

    /// <summary>How far out of the destination to look for footing. Matches the reach ledger's own sweep.</summary>
    private const int MaxFootingSweep = 2;

    /// <summary>
    /// What this search is allowed, given what the population has already spent this second.
    ///
    /// The window is seeded from a real tick the first time round rather than left at zero: tick counts
    /// can start enormous or negative depending on the host, and a deadline field that defaults to zero is
    /// either already past or unreachably far — the first shuts the window for ever, the second never
    /// closes it.
    /// </summary>
    private static double Allowance(double requested)
    {
        var now = Core.TickCount;

        if (!_windowStarted || now - _windowEnds >= 0)
        {
            _windowStarted = true;
            _windowEnds = now + WindowLengthMs;
            _spentThisWindow = 0.0;
        }

        var left = WindowMs - _spentThisWindow;

        if (left <= 0.0)
        {
            return FloorMs;
        }

        // <b>A caller that names its own ceiling is asking for more than the ordinary one, and clamping it
        // back down to the ordinary one made the request a lie.</b> BotScoutmaster asks for three hundred
        // milliseconds before it commits six bots to a five-hundred-tile walk; clamped to sixty it would
        // refuse every honest journey of that length. The population's window still binds — nobody gets more
        // than is left of the second — and Starved says so when it bites, which is how a caller knows its
        // answer was cut short rather than concluded.
        return Math.Clamp(Math.Min(requested, left), FloorMs, Math.Max(CeilingMs, requested));
    }

    private static void Spend(double ms) => _spentThisWindow += ms;

    /// <summary>Octile distance in the same units as the step costs, so the estimate never overshoots.</summary>
    private static int Heuristic(int x, int y, Point3D goal)
    {
        var dx = Math.Abs(x - goal.X);
        var dy = Math.Abs(y - goal.Y);

        return dx > dy
            ? DiagonalCost * dy + StraightCost * (dx - dy)
            : DiagonalCost * dx + StraightCost * (dy - dx);
    }

    /// <summary>
    /// <see cref="BotStep.BlockedByItems"/>, remembered for the length of one search.
    ///
    /// Without this the diagonal rule costs two item lookups per diagonal candidate — eight a tile, where
    /// the terrain mask costs one — and a tile is flanked by up to four diagonals from four different
    /// neighbours, so nearly all of it would be the same answer fetched again.
    ///
    /// <para>
    /// <b>Keyed on the exact height, not the height band.</b> <see cref="BotStep.Cell"/> folds Z into
    /// twenty-unit bands, which is right for naming a place and wrong for this: whether an item is in the
    /// way is decided by whether its own height overlaps the body standing there, and two heights thirteen
    /// units apart share a band while disagreeing about a crate. Keyed by band, the first query to arrive
    /// would answer for the second — silently, and differently depending on the order the search happened
    /// to reach them. The first version keyed this by band. It is the same planner-versus-engine
    /// disagreement the whole file exists to prevent, hiding in the cache rather than the rules.
    /// </para>
    /// </summary>
    private static bool Blocked(Map map, int x, int y, sbyte z, bool doorsShut)
    {
        var key = ((long)(z + 128) << 26) | ((long)x << 13) | (uint)y;

        if (_blockedByItems.TryGetValue(key, out var blocked))
        {
            return blocked;
        }

        blocked = BotStep.BlockedByItems(map, x, y, z, doorsShut);
        _blockedByItems[key] = blocked;

        return blocked;
    }

    private static bool FlankBlocked(Map map, int x, int y, int dir, in StepMask mask, bool doorsShut)
    {
        var fx = x;
        var fy = y;

        CalcMoves.Offset((Direction)dir, ref fx, ref fy);

        return Blocked(map, fx, fy, mask.GetWalkZ((Direction)dir), doorsShut);
    }

    private static int AddNode(int x, int y, sbyte z, int cost, int parent)
    {
        if (_nodeCount == _nodeX.Length)
        {
            Grow();
        }

        var node = _nodeCount++;

        _nodeX[node] = x;
        _nodeY[node] = y;
        _nodeZ[node] = z;
        _nodeCost[node] = cost;
        _nodeParent[node] = parent;
        _nodeClosed[node] = false;

        return node;
    }

    private static void Grow()
    {
        var size = _nodeX.Length * 2;

        Array.Resize(ref _nodeX, size);
        Array.Resize(ref _nodeY, size);
        Array.Resize(ref _nodeZ, size);
        Array.Resize(ref _nodeCost, size);
        Array.Resize(ref _nodeTotal, size);
        Array.Resize(ref _nodeParent, size);
        Array.Resize(ref _nodeClosed, size);

        logger.Information("Path search grew its node pool to {Size}", size);
    }

    private static void Rebuild(int node, List<Point3D> path)
    {
        _reversed.Clear();

        // The bot's own tile is the root and is not a step, so the walk stops before it.
        while (_nodeParent[node] >= 0)
        {
            _reversed.Add(new Point3D(_nodeX[node], _nodeY[node], _nodeZ[node]));
            node = _nodeParent[node];
        }

        for (var i = _reversed.Count - 1; i >= 0; i--)
        {
            path.Add(_reversed[i]);
        }
    }
}
