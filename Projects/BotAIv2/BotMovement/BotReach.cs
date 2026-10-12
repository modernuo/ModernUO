using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>What the reach ledger can say about a journey before anybody searches for it.</summary>
public enum BotReachVerdict
{
    /// <summary>Nothing is known. Search.</summary>
    Unknown,

    /// <summary>Both ends are in the same sealed pocket of ground. There is a way; search for it.</summary>
    Connected,

    /// <summary>
    /// One end sits in a pocket of ground that has been walked to its edges, and the other end is not in
    /// it. There is no way, and no search will find one.
    /// </summary>
    Sealed
}

/// <summary>
/// Which pockets of ground are closed, learned for nothing out of searches that failed.
///
/// <para>
/// <b>The idea.</b> Proving that somewhere cannot be reached is the most expensive question a bot ever
/// asks — the cheap searches run almost straight at their goal, while a refusal has to examine every tile
/// the bot can reach before it can say no. But a search that runs out of <em>ground</em> has, by
/// definition, just enumerated an entire connected pocket. So the expensive proof is paid <b>once per
/// pocket for the life of the shard</b>, written down, and shared by everybody. A walled yard, a crypt,
/// an island, somebody's back garden: each bills the population exactly once.
/// </para>
///
/// <para>
/// This is why there is no precomputation pass and no lattice. Felucca is 6144 × 4096 — twenty-five
/// million tiles, about a minute of work at the measured cost per tile — and that is a bad price for a
/// boot. It is also unnecessary: the questions worth refusing for free are "behind that railing" and
/// "across that water", and both are pockets. The mainland never becomes one, and should not.
/// </para>
///
/// <para>
/// <b>What makes a pocket trustworthy.</b> Only a search whose open set emptied <em>without ever being
/// clipped by its own search box</em> may record one. A search bounded by a box that ran out of tiles
/// inside the box has learned nothing about the world — and recording that would seal off half a
/// continent. See <see cref="BotPath"/>.
/// </para>
/// </summary>
public static class BotReach
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotReach));

    /// <summary>Standing cell, map folded in, to the pocket it belongs to.</summary>
    private static readonly Dictionary<long, int> _pocketOf = [];

    /// <summary>
    /// Pocket to pocket, for pockets that turned out to be one. A four-line union-find, and it exists
    /// because of <see cref="Contradict"/> — geometry changes when somebody builds a house.
    /// </summary>
    private static readonly Dictionary<int, int> _merged = [];

    /// <summary>How many cells each pocket holds, for the summary. A yard is dozens; a crypt is hundreds.</summary>
    private static readonly Dictionary<int, int> _size = [];

    private static int _next;

    /// <summary>Pockets known. One line in the summary, and the only measure of what this is saving.</summary>
    public static int Pockets { get; private set; }

    /// <summary>
    /// Journeys refused outright, without a search.
    ///
    /// Only those. The walker also asks this question straight after a look at the far side has filed a new
    /// pocket, and that one had a search — so it asks with the tally off. A saving that counts the searches it
    /// did not save is not a measurement.
    /// </summary>
    public static long Refused { get; private set; }

    /// <summary>Times two pockets turned out to be one because a bot walked between them.</summary>
    public static long Healed { get; private set; }

    /// <summary>Journeys refused by the reach map rather than a filed pocket (01.10.2026). See <see cref="Ask"/>.</summary>
    public static long MapRefused { get; private set; }

    /// <summary>Whether the last <see cref="Ask"/> was refused by the reach map, for the walker's reason when it drops a walk.</summary>
    public static bool LastByMap { get; private set; }

    /// <summary>
    /// Surface computations spent answering questions. Counted because they are the real cost of this
    /// ledger and they appear in none of the search figures — an unmeasured saving is a claim, not a
    /// measurement.
    /// </summary>
    public static long Probes { get; private set; }

    /// <summary>
    /// Records a pocket of ground that has been walked to its edges.
    ///
    /// <paramref name="cells"/> must be every standing cell the search closed, and the search must have
    /// emptied its open set with nothing clipped. Anything less and this seals off ground that is
    /// perfectly reachable — the one failure here that would be worse than the problem it solves.
    /// </summary>
    public static void Record(Map map, ICollection<int> cells, Point3D where)
    {
        if (map == null || cells == null || cells.Count == 0)
        {
            return;
        }

        // Already known: the same pocket proved again from a different tile inside it. Nothing to learn,
        // and re-numbering it would orphan the cells recorded the first time.
        foreach (var cell in cells)
        {
            if (_pocketOf.ContainsKey(Fold(map, cell)))
            {
                return;
            }
        }

        var pocket = _next++;

        foreach (var cell in cells)
        {
            _pocketOf[Fold(map, cell)] = pocket;
        }

        _size[pocket] = cells.Count;
        Pockets++;

        // The place is in the line, and it is not decoration. A wrongly filed pocket is silent and permanent
        // — bots simply stop going somewhere and nothing says so — so every entry has to be auditable against
        // the map by somebody reading the log afterwards.
        logger.Information(
            "A pocket of {Count} tiles on {Map} around {Where} has been walked to its edges; journeys in or out of it will be refused without a search",
            cells.Count,
            map,
            where
        );
    }

    /// <summary>
    /// Whether a journey is already known to be impossible, before a tile is examined.
    ///
    /// <para>
    /// Every cell within the arrival tolerance is tried rather than the goal alone. The goal's own Z is
    /// often not a floor at all — it is where a creature is standing, or the middle of a market — and a
    /// verdict of <see cref="BotReachVerdict.Sealed"/> is far too expensive to be wrong. Nine lookups for
    /// the ordinary case is a rounding error against a search.
    /// </para>
    /// </summary>
    public static BotReachVerdict Ask(Map map, Point3D from, Point3D goal, BotArrival arrival, bool tally = true)
    {
        LastByMap = false;

        // <b>The reach map first (01.10.2026, Patrick's order of 30.09.2026, night: "no path may be formed into the
        // unreachable zones").</b> A walker on open ground asking for a place with no open tile within its reach — water, a
        // cliff top, a sealed yard, an island no road reaches — is refused here without a search, and so is a walker aboard a
        // ship asking for land. Beside counts as arriving (BotWalk: "one tile off and no way onto it is arriving", two tiles for
        // a company's station, and the ledger is not told which), so the reach is at least two: a tree, a rock face, a fish by
        // the shore or a station on a crypt's step is still somewhere to walk to. Every chooser that consults the ledger, and
        // the walker's own search, hear it. See BotReachMap.Refuses.
        if (map != null && BotReachMap.Refuses(map, from, goal, Math.Max(arrival.Tiles, 2)))
        {
            LastByMap = true;

            if (tally)
            {
                MapRefused++;
            }

            return BotReachVerdict.Sealed;
        }

        if (map == null || _pocketOf.Count == 0)
        {
            return BotReachVerdict.Unknown;
        }

        // <b>The map's word over the ledger's where the map has one (01.10.2026, build next).</b> The ledger files a pocket by
        // flooding steps, and a flood does not go through a teleporter: every level of a dungeon, and an island a moongate joins,
        // is a "sealed pocket" to it, and a walker outside one was refused every place in it in no time at all. On build 384
        // (00:42–10:20) 266 walks came back Sealed from 41 bots: the Orc Caves' levels (864 tiles round (5136, 1965), 3482 round
        // (5331, 1378)), Skara Brae's 23272 tiles, Jhelom — and the delves from Skara Brae dropped "into the Orc Caves by its
        // mouth" standing on the moongate itself ("Balgrim … refused at (643, 2067, 5)", seven calls). The reach map draws
        // every gate; where it says the walker stands on open ground and the goal's own place is open ground too, home joins
        // both and so does a walk, gates and all, and the ledger is not asked. Only the goal's own place is read (a creature
        // on a crypt's roof stays the ledger's to answer).
        if (BotReachMap.Joins(map, from, goal))
        {
            if (tally)
            {
                MapJoined++;
            }

            return BotReachVerdict.Unknown;
        }

        // The Z that comes in is used as it stands rather than looked up. Callers ask this about somewhere
        // a body is standing or could stand, so its height is already the standing height — and probing
        // for it again would put a surface computation on the hot path for no new information.
        var hasHere = _pocketOf.TryGetValue(Fold(map, Cell(from)), out var here);

        if (hasHere)
        {
            here = Root(here);
        }

        var verdict = Sweep(map, goal, arrival, hasHere, here);

        // <b>A body standing beside a pocket stands in it (01.10.2026, build next).</b> A teleporter's landing, a moongate's tile, a
        // ledge a step or a spell put the body on: none of them is a floor the flood walked, so the body's own cell is in no
        // pocket and every place inside the pocket next to it read Sealed — Lorandil at (5145, 1969, 5) on the Orc Caves' first
        // level failed every errand "no way through" from 10:21 on build 384, Vondal 18 times at the second level's landing
        // (5331–5332, 1380). Before Sealed is said of a body in no pocket, the floors within a tile of it are asked, and the
        // first one filed answers for the body. Only then: nine probes on the way to a refusal, never on the common path.
        if (verdict == BotReachVerdict.Sealed && !hasHere && Beside(map, from, out var beside))
        {
            NearSide++;
            verdict = Sweep(map, goal, arrival, true, beside);
        }

        if (verdict == BotReachVerdict.Sealed && tally)
        {
            Refused++;
        }

        return verdict;
    }

    /// <summary>The ledger's sweep over the arrival round the goal, for a near side already settled. See <see cref="Ask"/>.</summary>
    private static BotReachVerdict Sweep(Map map, Point3D goal, BotArrival arrival, bool hasHere, int here)
    {
        // Both directions are asked, and the second one is not symmetry for its own sake.
        //
        // Only checking "the bot is in a sealed pocket and the goal is not" leaves the commoner case
        // unanswered: a bot out on the mainland with a goal inside a sealed crypt. There, the near side is
        // unknown, so the ledger says nothing, the search runs against the whole continent, hits its time
        // ceiling, comes back partial — and the bot walks hopefully towards a place it can never enter, for
        // as long as it will put up with getting nowhere. The pocket was already paid for. It should
        // answer.
        var reach = Math.Min(arrival.Tiles, MaxSweep);
        var goalZ = (sbyte)Math.Clamp(goal.Z, sbyte.MinValue, sbyte.MaxValue);
        var sealedSeen = false;

        // <b>Sealed is said of the whole arrival, never of the first filed cell in it.</b> The sweep used to return the
        // first verdict it met, from the north-west corner inwards, so a goal on open ground beside a filed pocket was
        // refused whenever the pocket lay first in that order. On 24.09.2026 the zombies at (1376, 1466, 10) in Britain
        // Graveyard stood on ground the far side of which floods to the world ("the far side says TooBig"), and every
        // walk at them was refused in no time at all ("Sealed, 0 tiles of plan, in 0.0ms"), because the tile to their
        // north-west settles on the crypt's roof at thirty — a pocket of 72 tiles, filed at 05:45:30. Six hunts failed
        // "no way through" in fifty minutes, about five an hour since 02:12, and the shun that follows each one sent the
        // next hunter as soon as it lapsed. Now any open cell answers Unknown, and Sealed needs at least one sealed cell
        // and no open one.
        for (var dx = -reach; dx <= reach; dx++)
        {
            for (var dy = -reach; dy <= reach; dy++)
            {
                var x = goal.X + dx;
                var y = goal.Y + dy;
                var goalCell = BotStep.Cell(x, y, goalZ);

                Probes++;

                // <b>Two heights, and asking about only one of them made this ledger unable to answer for
                // the pockets it had just filed itself.</b> The goal's own Z is where the thing being walked
                // to actually is - a skeleton on a crypt roof at thirty - and it is the height BotPath.Enclose
                // floods from, so it is the height the pocket gets written at. Settle finds the ground under
                // that roof, at zero, which is a different cell in a different band and is not in any pocket.
                // So the question came back Unknown, the search ran the full ceiling, the look at the far
                // side flooded the same roof again and BotReach.Record threw it away as already known - six
                // times out of ten on 03.09.2026 at 11:34, with three refusals to show for four pockets.
                //
                // Both are asked and neither is dropped: the goal's own height for somewhere a body is
                // standing, the settled height for a goal whose Z is a market stall or a creature in flight.
                var verdict = Look(map, goalCell, hasHere, here);

                if (verdict == BotReachVerdict.Connected)
                {
                    return verdict;
                }

                if (verdict == BotReachVerdict.Sealed)
                {
                    sealedSeen = true;

                    continue;
                }

                // <b>A floor the arrival could be made on comes before the settled one.</b> The arrival is met within a
                // person's height of the goal (BotArrival.Reached), so a floor that near is the place this cell stands
                // for; the settled height is the terrain's, and beside a building that is its roof. Only where no such
                // floor exists is the settled height asked, as it always was.
                var open = BotStep.Ground(map, x, y, goalZ, BotArrival.PersonHeight - 1, out var z)
                    || BotStep.Settle(map, x, y, out z);

                if (open && BotStep.Cell(x, y, z) != goalCell)
                {
                    verdict = Look(map, BotStep.Cell(x, y, z), hasHere, here);

                    if (verdict == BotReachVerdict.Connected)
                    {
                        return verdict;
                    }

                    if (verdict == BotReachVerdict.Sealed)
                    {
                        sealedSeen = true;

                        continue;
                    }
                }

                // Somewhere near the goal is ground nobody has filed. Whatever else is true, this journey is
                // not provably impossible. Only said when there was ground to speak of: a height with no
                // floor under it is not evidence that the way is open.
                if (!hasHere && open)
                {
                    return BotReachVerdict.Unknown;
                }
            }
        }

        if (!hasHere && !sealedSeen)
        {
            return BotReachVerdict.Unknown;
        }

        return BotReachVerdict.Sealed;
    }

    /// <summary>
    /// The pocket a floor within one tile of <paramref name="at"/> is filed in, for a body standing where no flood walked. See
    /// the note at its use in <see cref="Ask"/>.
    /// </summary>
    private static bool Beside(Map map, Point3D at, out int pocket)
    {
        pocket = 0;

        var z = (sbyte)Math.Clamp(at.Z, sbyte.MinValue, sbyte.MaxValue);

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                var x = at.X + dx;
                var y = at.Y + dy;

                if (!BotStep.Ground(map, x, y, z, BotStep.GroundReach, out var floor) && !BotStep.Settle(map, x, y, out floor))
                {
                    continue;
                }

                if (_pocketOf.TryGetValue(Fold(map, BotStep.Cell(x, y, floor)), out var filed))
                {
                    pocket = Root(filed);

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Asks the reach map answered for the ledger: both ends open ground a walk from home reaches, so no pocket of the ledger
    /// is asked (01.10.2026, build next); and bodies standing off every flood that were placed in the pocket beside them.
    /// </summary>
    public static long MapJoined { get; private set; }

    public static long NearSide { get; private set; }

    /// <summary>
    /// What one filed cell says about this journey, or Unknown when it says nothing.
    ///
    /// Unknown carries two different meanings here and the caller separates them: the cell is in no pocket at
    /// all, or it is in one that neither confirms nor refuses this journey. Both mean "keep looking".
    /// </summary>
    private static BotReachVerdict Look(Map map, int cell, bool hasHere, int here)
    {
        if (!_pocketOf.TryGetValue(Fold(map, cell), out var there))
        {
            return BotReachVerdict.Unknown;
        }

        there = Root(there);

        if (hasHere)
        {
            return there == here ? BotReachVerdict.Connected : BotReachVerdict.Unknown;
        }

        // The far side is sealed and the near side is not in it. Nothing can get in.
        return BotReachVerdict.Sealed;
    }

    /// <summary>
    /// How wide a sweep around the goal is worth doing.
    ///
    /// The sweep is <c>(2n+1)²</c> cells and each one is a <see cref="BotStep.Settle"/> — a terrain height
    /// plus one or two surface computations, not a dictionary lookup. Nine of those for
    /// <see cref="BotArrival.Beside"/> is a rounding error against a search; four hundred and forty-one for
    /// <c>Within(10)</c> is not, and sixteen hundred for <c>Within(20)</c> would cost more than the search
    /// it is trying to avoid. Past this the goal tile alone is asked about, which can only make the ledger
    /// answer <see cref="BotReachVerdict.Unknown"/> more often — never wrongly.
    /// </summary>
    private const int MaxSweep = 2;

    /// <summary>
    /// Two cells that the ledger thought were in different pockets, and a bot has just walked from one to
    /// the other. The ledger was wrong; it is corrected.
    ///
    /// <para>
    /// Cheap insurance rather than a common event. A pocket is only sealed on evidence, but the evidence
    /// has a shelf life: somebody builds a house, a wall comes down, a gate is added. Without this the
    /// ledger would be a permanent hole in the population's behaviour with no way to notice — the worst
    /// shape a bug can take, and one this project has already paid for in other forms. With it, the world
    /// itself is the correction, and it arrives the first time a bot proves the ledger wrong by doing the
    /// thing the ledger says is impossible.
    /// </para>
    /// </summary>
    public static void Contradict(Map map, Point3D left, Point3D right)
    {
        if (map == null || _pocketOf.Count == 0)
        {
            return;
        }

        // Both points are places a bot has just been standing, so their own Z is the standing Z. No probe.
        if (!_pocketOf.TryGetValue(Fold(map, Cell(left)), out var a)
            || !_pocketOf.TryGetValue(Fold(map, Cell(right)), out var b))
        {
            return;
        }

        a = Root(a);
        b = Root(b);

        if (a == b)
        {
            return;
        }

        _merged[b] = a;
        Pockets--;
        Healed++;

        logger.Information(
            "Two pockets of ground on {Map} turned out to be one; a bot walked from {Left} to {Right}",
            map,
            left,
            right
        );
    }

    /// <summary>
    /// Everything forgotten.
    ///
    /// <para>
    /// <b>This knowledge is deliberately not written to disk yet</b>, unlike the map of ground and the map
    /// of shops in the first version. The argument for persisting is the same and it is a good one: a
    /// proof thrown away at restart is a proof bought twice. The argument against is that a wrong
    /// "impossible" is invisible and permanent — a bot simply never goes somewhere, for ever, and nothing
    /// in any log says so. One shard run's worth of savings is already the whole point; the file can come
    /// later, once this has been watched behaving.
    /// </para>
    /// </summary>
    public static void Reset()
    {
        _pocketOf.Clear();
        _merged.Clear();
        _size.Clear();
        _next = 0;
        Pockets = 0;
        Refused = 0;
        Healed = 0;
        Probes = 0;
        MapRefused = 0;
        MapJoined = 0;
        NearSide = 0;
    }

    public static string Describe() =>
        $"{Pockets} pockets of ground walked to their edges, {Refused} journeys refused without a search at a cost of {Probes} surface probes, {Healed} pockets that turned out to be one; {MapRefused} refused by the reach map; {MapJoined} asks the reach map answered for the ledger, both ends open ground a walk from home reaches through the gates, and {NearSide} bodies standing off every flood placed in the pocket beside them";

    private static int Root(int pocket)
    {
        while (_merged.TryGetValue(pocket, out var parent))
        {
            pocket = parent;
        }

        return pocket;
    }

    /// <summary>
    /// Cell plus map in one long. A cell fits in thirty bits — thirteen each for x and y, four for the
    /// height band — so the map index has the top of the word to itself.
    /// </summary>
    private static long Fold(Map map, int cell) => ((long)map.MapIndex << 32) | (uint)cell;

    /// <summary>The cell a point names, taking its own height as the standing height.</summary>
    private static int Cell(Point3D at) =>
        BotStep.Cell(at.X, at.Y, (sbyte)Math.Clamp(at.Z, sbyte.MinValue, sbyte.MaxValue));
}
