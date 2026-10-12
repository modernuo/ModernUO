using System;
using System.Collections.Generic;

namespace Server.Engines.Pathing.Tiered;

/// <summary>What the route memory made of a request. See <see cref="NavRouteMemory.Recall"/>.</summary>
public enum NavRecall
{
    /// <summary>Not the memory's to answer: off, an end the graph cannot place, or the two ends too near to be worth it.</summary>
    Skipped,

    /// <summary>No remembered road from any node the start reaches to the goal's cluster that the goal joins.</summary>
    Missed,

    /// <summary>A remembered road, quiet all along: the points are in the list.</summary>
    Served,

    /// <summary>A remembered way round the danger, whose reason still stands: the points are in the list.</summary>
    Detoured,

    /// <summary>The remembered road stands and crosses ground the danger hook charges now: plan round it.</summary>
    Bloodied
}

/// <summary>What a remembered road's clusters said when it was proved against the graph. See NavRouteMemory.Prove.</summary>
internal enum NavProof
{
    /// <summary>Every cluster as it was, by version or by signature.</summary>
    Valid,

    /// <summary>A cluster it crosses is being drawn again: not now, and not forgotten.</summary>
    Waiting,

    /// <summary>A cluster it crosses came out different: the road is forgotten.</summary>
    Changed
}

/// <summary>What became of a route offered to the memory. See <see cref="NavRouteMemory.Offer"/>.</summary>
public enum NavOffer
{
    /// <summary>Not taken: the memory is off, the ends are too near, or the route could not be priced from the graph.</summary>
    Declined,

    /// <summary>Kept for places nothing was remembered from before.</summary>
    Stored,

    /// <summary>Cheaper, from at least one place, than what was remembered there: it replaced it there.</summary>
    Improved,

    /// <summary>No cheaper from anywhere than what was remembered: the memory is as it was.</summary>
    Kept
}

/// <summary>
/// Routes the tiered navigation has found, remembered and handed out again — Patrick's order of 29.09.2026: "paths and graphs
/// must be remembered; they can only be optimised."
///
/// <para>
/// <b>A road is remembered node by node, not pair by pair.</b> Every node of a planned route is a place to join it, keyed
/// by where the node stands and the cluster the road ends in. A walker asking from anywhere in a cluster the road passes
/// through joins it at the cheapest node its own cluster's flood reaches, and walks the rest: a suffix of a best road is a
/// best road from where it begins. So the walker that strayed, the second bot leaving the same yard, and the re-draw half
/// way along all find the road without a search. What the memory hands back is exactly what a plan would: one point a
/// cluster, a gate's near side left out (<see cref="HierarchicalPlanner.LastPath"/>).
/// </para>
///
/// <para>
/// <b>Only ever cheaper.</b> A newly planned route is offered (<see cref="Offer"/>) and takes a place to join from the
/// remembered road there only when what is left of it from that place, to the same goal, costs less; never a dearer one.
/// A road whose every place has been taken by cheaper ones is dropped. The comparison is made at the moment of the offer,
/// against the same ends, over the graph as it stands.
/// </para>
///
/// <para>
/// <b>The ground changes; the memory checks, cheaply.</b> A road keeps, for every run of its nodes in one cluster, the
/// cluster's version when it was last proved (<see cref="NavGraph.ClusterVersion"/>) and a signature of the cluster's
/// nodes and edges. Same version: nothing changed. A different version with the same signature: the cluster was drawn
/// again the same — every cluster after a boot, the clusters beside a house redrawn at boot — so the node ids are looked up
/// again from where they stand and the road stands. A different signature: the ground changed, and the road is forgotten
/// and planned afresh. A cluster being drawn again right now makes the road wait, not die.
/// </para>
///
/// <para>
/// <b>The danger moves; the memory is of the ground (29.09.2026).</b> The danger hook (<see cref="NavigationService.Danger"/>,
/// BotDanger: sixty tiles a bloodied quadrant's node, eight hundred a barred one) changes by the minute: sightings stand
/// five minutes, a quarantine two hours. A road planned round the blood and remembered for good would be a detour served
/// for ever after the blood dried. So the roads remembered are ground roads, planned with the hook off, and at every
/// recall the road's nodes are read against the danger as it is now: a road that crosses none is served, and it is then
/// the road the danger-aware planner would find — it was the cheapest on the ground and nothing along it is charged, so
/// every other road costs at least its ground price, which is no less. A road that crosses blood is planned round
/// (<see cref="NavRecall.Bloodied"/>), and the way round is kept beside it for <see cref="DetourMs"/> as a detour: served
/// while it is no bloodier than when it was drawn and the ground road no less bloody — the reason for it still stands.
/// </para>
///
/// <para>
/// <b>Bounded, and kept.</b> At most <see cref="Capacity"/> roads and <see cref="NodeCapacity"/> nodes; the road least
/// lately used goes first. The roads outlive a restart in a file beside the graph's (<see cref="NavRouteFile"/>), written
/// by a worker from arrays that never change once a road is made; read back, a road is proved against the graph by its
/// signatures the first time it is used.
/// </para>
/// </summary>
public sealed class NavRouteMemory
{
    /// <summary>
    /// Roads kept, at most. Two thousand and forty-eight: the shard asked 2244 routes in fifteen minutes of 29.09.2026
    /// (18:38–18:53), most from a few dozen places — the halls, the banks, the mouths — to a few dozen more.
    /// </summary>
    public int Capacity { get; set; } = 2048;

    /// <summary>
    /// Nodes kept over all roads, at most: a quarter of a million, some twelve megabytes with the places to join them. A road
    /// from Britain to Yew is some two hundred nodes; most are under a hundred.
    /// </summary>
    public int NodeCapacity { get; set; } = 262144;

    /// <summary>
    /// Ends fewer clusters apart than this (in the larger of the two axes) are left to the planner: a route of two clusters is
    /// a few dozen expansions, well under the price of proving a remembered one, and a road joined from the wrong side of its
    /// start cluster would be dear in proportion. Three.
    /// </summary>
    public int NearClusters { get; set; } = 3;

    /// <summary>
    /// How long a way round the danger is trusted, in milliseconds. Five minutes: the time a sighting of creatures stands on
    /// the quadrant map (BotQuad.SightMs), the quickest part of the danger to change.
    /// </summary>
    public int DetourMs { get; set; } = 300000;

    /// <summary>Ways round the danger kept, one a pair of clusters. Five hundred and twelve.</summary>
    public int DetourCapacity { get; set; } = 512;

    /// <summary>Cluster signatures kept between proofs, before the lot is dropped and signed again.</summary>
    public int SignatureCapacity { get; set; } = 65536;

    public bool Enabled { get; set; } = true;

    /// <summary>The clock the detours are timed by. <c>Core.TickCount</c>; replaced by the tests.</summary>
    public Func<long> Clock { get; set; } = static () => Core.TickCount;

    private readonly Dictionary<long, Place> _index = new();

    private readonly List<NavRememberedRoute> _routes = [];

    private readonly Dictionary<long, NavRememberedRoute> _detours = new();

    private readonly Dictionary<long, (int Version, ulong Sig)> _signatures = new();

    private readonly List<Candidate> _candidates = [];

    private readonly List<Point3D> _lastGround = [];

    private readonly NavWindow _ownStart = new();

    private readonly NavWindow _ownGoal = new();

    private readonly int[] _startCost = new int[NavWindow.States];

    private readonly int[] _goalCost = new int[NavWindow.States];

    private int _sc;

    private int _gc;

    // The ends flooded last, so the offer that follows a missed recall — the same start, and the goal the planner settled on,
    // which is the same cell as a rule — does not flood them again. A cluster's flood is fixed by its version.
    private int _startGraph;

    private long _startEnd;

    private int _startVersion;

    private int _goalGraph;

    private long _goalEnd;

    private int _goalVersion;

    private long _clock;

    private int _offer;

    /// <summary>A place to join a road: the road, and the node's index along it.</summary>
    private readonly record struct Place(NavRememberedRoute Route, int At);

    private readonly record struct Candidate(NavRememberedRoute Route, int From, int To, long Cost);

    public int Count => _routes.Count;

    public int Nodes { get; private set; }

    public int Places => _index.Count;

    public int Detours => _detours.Count;

    /// <summary>Whether anything was kept or forgotten since the file was last written.</summary>
    public bool Dirty { get; set; }

    /// <summary>The ground road's danger at the last recall that answered <see cref="NavRecall.Bloodied"/>.</summary>
    public long LastGroundDanger { get; private set; }

    /// <summary>
    /// The ground road itself at the last recall that answered <see cref="NavRecall.Bloodied"/>, into <paramref name="points"/>: for the
    /// walker whose way round the planners cannot find within their budgets, which walks the ground road rather than the old chart.
    /// </summary>
    public bool LastGround(List<Point3D> points)
    {
        points.Clear();
        points.AddRange(_lastGround);

        return points.Count > 0;
    }

    public long Asked { get; private set; }

    public long Served { get; private set; }

    /// <summary>Of those served, joined somewhere after the road's first node: the walker was already on it.</summary>
    public long Middles { get; private set; }

    public long Detoured { get; private set; }

    public long Missed { get; private set; }

    /// <summary>A place matched but the goal could not be joined to the road's end from where it was asked.</summary>
    public long Unjoined { get; private set; }

    /// <summary>A road crossed a cluster being drawn again: not served, not forgotten.</summary>
    public long Waiting { get; private set; }

    public long Bloodied { get; private set; }

    public long Offered { get; private set; }

    public long Stored { get; private set; }

    public long Improved { get; private set; }

    public long Kept { get; private set; }

    public long Declined { get; private set; }

    /// <summary>Forgotten because a cluster they cross came out different.</summary>
    public long Invalidated { get; private set; }

    /// <summary>Clusters drawn again the same, whose roads were kept and their node ids looked up afresh.</summary>
    public long Rejoined { get; private set; }

    /// <summary>Roads planned round a shunned gate, forgotten when the shun ran out. See <see cref="Offer"/>.</summary>
    public long Expired { get; private set; }

    /// <summary>Roads every place of which was taken by a cheaper one.</summary>
    public long Superseded { get; private set; }

    public long Evicted { get; private set; }

    /// <summary>Forgotten by hand: a walker could not pass a gate on them, or a house went up or down beside them.</summary>
    public long Forgotten { get; private set; }

    public long DetoursStored { get; private set; }

    /// <summary>Ways round dropped: past their time, the ground road quieter, or the way round bloodier than when drawn.</summary>
    public long DetoursDropped { get; private set; }

    /// <summary>Roads read from the file at boot.</summary>
    public long Read { get; private set; }

    // ------------------------------------------------------------------------------------------------ recall

    /// <summary>
    /// A remembered road from <paramref name="from"/> to <paramref name="to"/>, into <paramref name="points"/> (cleared
    /// first) when the answer is <see cref="NavRecall.Served"/> or <see cref="NavRecall.Detoured"/>. <paramref name="danger"/>
    /// is the hook the road is read against, null for none. <paramref name="anchorage"/> is the planner's own, so an end
    /// stands where the planner would stand it.
    /// </summary>
    public NavRecall Recall(
        NavGraph graph, NavWindowCache cache, int anchorage, Point3D from, Point3D to, List<Point3D> points, Func<int, int, int> danger
    )
    {
        points.Clear();
        _lastGround.Clear();
        LastGroundDanger = 0;

        if (!Enabled || graph == null || !Fits(graph) || Near(from, to) || !Ends(graph, cache, anchorage, from, to))
        {
            return NavRecall.Skipped;
        }

        Asked++;

        if (!Ground(graph, out var road))
        {
            Missed++;

            return NavRecall.Missed;
        }

        var bloodied = Danger(road.Route, road.From, road.To, danger);

        if (bloodied == 0)
        {
            Points(road.Route, road.From, road.To, points);
            Used(road.Route);
            Served++;

            if (road.From > 0)
            {
                Middles++;
            }

            return NavRecall.Served;
        }

        LastGroundDanger = bloodied;
        Points(road.Route, road.From, road.To, _lastGround);

        if (Round(graph, bloodied, danger, points))
        {
            Detoured++;

            return NavRecall.Detoured;
        }

        Bloodied++;

        return NavRecall.Bloodied;
    }

    /// <summary>The cheapest valid remembered ground road the two ends of the request in hand join.</summary>
    private bool Ground(NavGraph graph, out Candidate best)
    {
        best = default;
        _candidates.Clear();

        var nodes = graph.NodesOf(_sc);

        for (var i = 0; nodes != null && i < nodes.Count; i++)
        {
            var n = nodes[i];
            var start = _startCost[StateIn(graph, _sc, graph.X(n), graph.Y(n), graph.Stratum(n))];

            if (start < 0 || !_index.TryGetValue(Key(graph.Id, graph.X(n), graph.Y(n), graph.Stratum(n), _gc), out var place))
            {
                continue;
            }

            if (!JoinGoal(graph, place.Route, place.At, out var to, out var rest))
            {
                Unjoined++;

                continue;
            }

            Insert(new Candidate(place.Route, place.At, to, start + rest));
        }

        // Cheapest first; the first that proves valid is the answer.
        for (var i = 0; i < _candidates.Count; i++)
        {
            var c = _candidates[i];

            if (c.Route.Slot < 0)
            {
                // Forgotten by an earlier candidate's proof in this same call.
                continue;
            }

            // Planned round a gate a walker could not pass, and the gate is no longer shunned: planned again, and the new road
            // offered in its place. A way round a shunned gate kept for good would be a detour served for ever.
            if (c.Route.Expires && Clock() - c.Route.Until >= 0)
            {
                Expired++;
                Remove(c.Route);

                continue;
            }

            switch (Prove(graph, c.Route, c.From, c.To))
            {
                case NavProof.Valid:
                    {
                        best = c;

                        return true;
                    }
                case NavProof.Waiting:
                    {
                        Waiting++;

                        break;
                    }
                default:
                    {
                        Invalidated++;
                        Remove(c.Route);

                        break;
                    }
            }
        }

        return false;
    }

    /// <summary>The way round the danger kept for this pair of clusters, when its reason still stands.</summary>
    private bool Round(NavGraph graph, long groundDanger, Func<int, int, int> danger, List<Point3D> points)
    {
        var key = DetourKey(graph.Id, _sc, _gc);

        if (!_detours.TryGetValue(key, out var detour))
        {
            return false;
        }

        var drop = Clock() - detour.Until >= 0;

        if (!drop && JoinStart(graph, detour, out var from, out _) && JoinGoal(graph, detour, from, out var to, out _))
        {
            var proof = Prove(graph, detour, from, to);

            if (proof == NavProof.Waiting)
            {
                return false;
            }

            // No bloodier than when it was drawn, and still quieter than the ground road for whoever is asking now: the reason to
            // go round still stands. (Until 01.10.2026 this asked that the ground road be no quieter than when the detour was
            // drawn; with the hook priced for the walker that asks — a company reads a road quieter than a bot alone — that
            // dropped and redrew the same detour at every change of walker.)
            var detourDanger = Danger(detour, 0, detour.Count - 1, danger);

            if (proof == NavProof.Valid && detourDanger <= detour.DangerThen && detourDanger < groundDanger)
            {
                Points(detour, from, to, points);
                detour.Used = ++_clock;
                detour.Uses++;

                return true;
            }

            drop = true;
        }

        if (drop)
        {
            _detours.Remove(key);
            DetoursDropped++;
        }

        return false;
    }

    /// <summary>
    /// Whether a planned route's nodes are all quiet under <paramref name="danger"/> (the start node excepted, as the
    /// planners charge it), and the danger along it when not.
    /// </summary>
    public static bool Quiet(NavGraph graph, IReadOnlyList<int> path, Func<int, int, int> danger, out long along)
    {
        along = 0;

        if (danger == null)
        {
            return true;
        }

        for (var i = 1; i < path.Count; i++)
        {
            along += danger(graph.X(path[i]), graph.Y(path[i]));
        }

        return along == 0;
    }

    // ------------------------------------------------------------------------------------------------ offer

    /// <summary>
    /// A ground route just planned from <paramref name="from"/> to <paramref name="goal"/> (the cell the planner settled on),
    /// as <paramref name="path"/> (<see cref="HierarchicalPlanner.LastPath"/>). Kept wherever it is cheaper than what is
    /// remembered, or nothing is; never where it is dearer.
    /// </summary>
    public NavOffer Offer(
        NavGraph graph, NavWindowCache cache, int anchorage, Point3D from, Point3D goal, IReadOnlyList<int> path, int expiresMs = 0
    )
    {
        if (!Enabled || graph == null || path == null || path.Count == 0 || !Fits(graph) || Near(from, goal)
            || !Ends(graph, cache, anchorage, from, goal))
        {
            return NavOffer.Declined;
        }

        Offered++;

        var route = Make(graph, path, _gc);

        if (route == null || graph.ClusterOfNode(path[0]) != _sc)
        {
            Declined++;

            return NavOffer.Declined;
        }

        if (expiresMs > 0)
        {
            route.Expires = true;
            route.Until = Clock() + expiresMs;
        }

        var last = route.Count - 1;
        var end = _goalCost[StateIn(graph, _gc, route.X[last], route.Y[last], route.S[last])];

        if (end < 0)
        {
            Declined++;

            return NavOffer.Declined;
        }

        _offer++;

        var improved = false;

        for (var t = 0; t <= last; t++)
        {
            var key = Key(graph.Id, route.X[t], route.Y[t], route.S[t], _gc);
            var mine = (long)route.Cum[last] - route.Cum[t] + end;

            if (!_index.TryGetValue(key, out var place))
            {
                _index[key] = new Place(route, t);
                route.Keys++;

                continue;
            }

            var held = place.Route;

            // What is left of the remembered road from this node to the same goal, over the graph as it stands.
            if (held.Offer != _offer)
            {
                held.Offer = _offer;
                held.Standing = Prove(graph, held, 0, held.Count - 1);
            }

            if (held.Standing == NavProof.Changed || held.Expires && Clock() - held.Until >= 0)
            {
                if (held.Standing == NavProof.Changed)
                {
                    Invalidated++;
                }
                else
                {
                    Expired++;
                }

                Remove(held);
                _index[key] = new Place(route, t);
                route.Keys++;

                continue;
            }

            if (held.Standing == NavProof.Waiting || !JoinGoal(graph, held, place.At, out _, out var theirs) || theirs <= mine)
            {
                continue;
            }

            _index[key] = new Place(route, t);
            route.Keys++;
            improved = true;

            if (--held.Keys <= 0)
            {
                Superseded++;
                Remove(held);
            }
        }

        if (route.Keys == 0)
        {
            Kept++;

            return NavOffer.Kept;
        }

        route.Used = ++_clock;
        Add(route);

        if (improved)
        {
            Improved++;
        }
        else
        {
            Stored++;
        }

        Trim();

        return improved ? NavOffer.Improved : NavOffer.Stored;
    }

    /// <summary>
    /// A way round the danger just planned from <paramref name="from"/> to <paramref name="goal"/>, kept for this pair of
    /// clusters for <see cref="DetourMs"/> beside the ground road, whose danger was <paramref name="groundDanger"/>.
    /// </summary>
    public bool OfferDetour(
        NavGraph graph, NavWindowCache cache, int anchorage, Point3D from, Point3D goal, IReadOnlyList<int> path, long groundDanger,
        Func<int, int, int> danger
    )
    {
        if (!Enabled || graph == null || path == null || path.Count == 0 || groundDanger <= 0 || !Fits(graph)
            || Near(from, goal) || !Ends(graph, cache, anchorage, from, goal))
        {
            return false;
        }

        var route = Make(graph, path, _gc);

        if (route == null || graph.ClusterOfNode(path[0]) != _sc)
        {
            return false;
        }

        route.DangerThen = Danger(route, 0, route.Count - 1, danger);
        route.GroundDangerThen = groundDanger;
        route.Expires = true;
        route.Until = Clock() + DetourMs;
        route.Used = ++_clock;

        if (_detours.Count >= DetourCapacity && !_detours.ContainsKey(DetourKey(graph.Id, _sc, _gc)))
        {
            var oldest = 0L;
            var oldestUse = long.MaxValue;

            foreach (var (k, d) in _detours)
            {
                if (d.Used < oldestUse)
                {
                    oldestUse = d.Used;
                    oldest = k;
                }
            }

            _detours.Remove(oldest);
            DetoursDropped++;
        }

        _detours[DetourKey(graph.Id, _sc, _gc)] = route;
        DetoursStored++;

        return true;
    }

    // ------------------------------------------------------------------------------------------------ forgetting

    /// <summary>
    /// The ground under a rectangle changed — a house raised or taken down. Every road and detour within a cluster of it is
    /// forgotten: those that cross it would be found out by their signatures, but a house taken down may open a shortcut
    /// beside a road that crosses nothing that changed, and a road that is no longer the cheapest is not worth keeping.
    /// </summary>
    public void GroundChanged(NavGraph graph, int x1, int y1, int x2, int y2)
    {
        var minX = Math.Min(x1, x2) - NavGraph.Side;
        var minY = Math.Min(y1, y2) - NavGraph.Side;
        var maxX = Math.Max(x1, x2) + NavGraph.Side;
        var maxY = Math.Max(y1, y2) + NavGraph.Side;

        for (var i = _routes.Count - 1; i >= 0; i--)
        {
            var r = _routes[i];

            if (r.GraphId == graph.Id && r.MinX <= maxX && r.MaxX >= minX && r.MinY <= maxY && r.MaxY >= minY)
            {
                Forgotten++;
                Remove(r);
            }
        }

        DropDetours(graph, r => r.MinX <= maxX && r.MaxX >= minX && r.MinY <= maxY && r.MaxY >= minY);
    }

    /// <summary>A walker could not pass the gate at (x, y): every road and detour through it is forgotten.</summary>
    public void Shun(NavGraph graph, int x, int y)
    {
        for (var i = _routes.Count - 1; i >= 0; i--)
        {
            var r = _routes[i];

            if (r.GraphId == graph.Id && r.Through(x, y))
            {
                Forgotten++;
                Remove(r);
            }
        }

        DropDetours(graph, r => r.Through(x, y));
    }

    private void DropDetours(NavGraph graph, Predicate<NavRememberedRoute> drop)
    {
        List<long> gone = null;

        foreach (var (k, d) in _detours)
        {
            if (d.GraphId == graph.Id && drop(d))
            {
                (gone ??= []).Add(k);
            }
        }

        for (var i = 0; gone != null && i < gone.Count; i++)
        {
            _detours.Remove(gone[i]);
            DetoursDropped++;
        }
    }

    public void Clear()
    {
        _index.Clear();
        _routes.Clear();
        _detours.Clear();
        _signatures.Clear();
        Nodes = 0;
        Dirty = true;
    }

    // ------------------------------------------------------------------------------------------------ the file

    /// <summary>The roads of one graph as the file keeps them: arrays that never change once a road is made, shared, not copied.</summary>
    public List<NavRouteRecord> Records(NavGraph graph)
    {
        var records = new List<NavRouteRecord>();

        for (var i = 0; i < _routes.Count; i++)
        {
            var r = _routes[i];

            // A road round a shunned gate is not kept past the boot: the shuns are not.
            if (r.GraphId == graph.Id && !r.Expires)
            {
                records.Add(r.Record());
            }
        }

        // Most lately used first, so a file read back orders the memory's clock the same way.
        records.Sort(static (a, b) => b.Used.CompareTo(a.Used));

        return records;
    }

    /// <summary>
    /// Roads read from a file, laid into the memory for <paramref name="graph"/>. Nothing is proved here: each road is proved
    /// by its clusters' signatures the first time it is used, whenever the graph is ready for it. A place already held is
    /// kept; the file is ordered most lately used first.
    /// </summary>
    public int Adopt(NavGraph graph, IReadOnlyList<NavRouteRecord> records)
    {
        if (!Enabled || graph == null || records == null || !Fits(graph))
        {
            return 0;
        }

        var adopted = 0;
        var top = _clock + records.Count;

        for (var i = 0; i < records.Count; i++)
        {
            var route = NavRememberedRoute.From(graph, records[i]);

            if (route == null)
            {
                continue;
            }

            for (var t = 0; t < route.Count; t++)
            {
                var key = Key(graph.Id, route.X[t], route.Y[t], route.S[t], route.Goal);

                if (_index.TryAdd(key, new Place(route, t)))
                {
                    route.Keys++;
                }
            }

            if (route.Keys == 0)
            {
                continue;
            }

            route.Used = top - i;
            Add(route);
            adopted++;
        }

        _clock = Math.Max(_clock, top);
        Read += adopted;
        Trim();

        return adopted;
    }

    // ------------------------------------------------------------------------------------------------ the pieces

    /// <summary>
    /// Whether the clusters a road crosses between two of its nodes are as they were: by version, or failing that by
    /// signature, when the node ids are looked up again from where the nodes stand.
    /// </summary>
    private NavProof Prove(NavGraph graph, NavRememberedRoute route, int from, int to)
    {
        for (var k = route.RunOf[from]; k <= route.RunOf[to]; k++)
        {
            var c = route.RunCluster[k];

            if (c < 0 || c >= graph.ClustersX * graph.ClustersY)
            {
                return NavProof.Changed;
            }

            if (!graph.IsJoined(c))
            {
                return NavProof.Waiting;
            }

            var version = graph.ClusterVersion(c);

            if (route.RunStamp[k] == version)
            {
                continue;
            }

            if (Signature(graph, c) != route.RunSig[k])
            {
                return NavProof.Changed;
            }

            for (var t = route.RunStart[k]; t < route.RunStart[k + 1]; t++)
            {
                var id = graph.NodeAt(route.X[t], route.Y[t], route.S[t]);

                if (id < 0 || graph.ClusterOfNode(id) != c)
                {
                    return NavProof.Changed;
                }

                route.Ids[t] = id;
            }

            route.RunStamp[k] = version;
            Rejoined++;
        }

        return NavProof.Valid;
    }

    /// <summary>
    /// The cheapest node of the road's last run that the goal is reached from, at or after <paramref name="from"/>, and what
    /// the road costs from <paramref name="from"/> to the goal by it.
    /// </summary>
    private bool JoinGoal(NavGraph graph, NavRememberedRoute route, int from, out int to, out long rest)
    {
        to = -1;
        rest = long.MaxValue;

        for (var t = route.Count - 1; t >= from && route.RunCluster[route.RunOf[t]] == _gc; t--)
        {
            var cost = _goalCost[StateIn(graph, _gc, route.X[t], route.Y[t], route.S[t])];

            if (cost < 0)
            {
                continue;
            }

            var total = (long)route.Cum[t] - route.Cum[from] + cost;

            if (total < rest)
            {
                rest = total;
                to = t;
            }
        }

        return to >= 0;
    }

    /// <summary>The cheapest node of the road's first run that the start reaches, for a detour, and what it costs from there.</summary>
    private bool JoinStart(NavGraph graph, NavRememberedRoute route, out int from, out long cost)
    {
        from = -1;
        cost = long.MaxValue;

        for (var t = 0; t < route.Count && route.RunCluster[route.RunOf[t]] == _sc; t++)
        {
            var start = _startCost[StateIn(graph, _sc, route.X[t], route.Y[t], route.S[t])];

            if (start < 0)
            {
                continue;
            }

            var total = start + (long)route.Cum[route.Count - 1] - route.Cum[t];

            if (total < cost)
            {
                cost = total;
                from = t;
            }
        }

        return from >= 0;
    }

    /// <summary>What the danger hook charges along a road between two of its nodes, the first excepted as the planners except it.</summary>
    private static long Danger(NavRememberedRoute route, int from, int to, Func<int, int, int> danger)
    {
        if (danger == null)
        {
            return 0;
        }

        var along = 0L;

        for (var t = from + 1; t <= to; t++)
        {
            along += danger(route.X[t], route.Y[t]);
        }

        return along;
    }

    /// <summary>The points of a road between two of its nodes, as <c>HierarchicalPlanner.Unwind</c> gives them.</summary>
    private static void Points(NavRememberedRoute route, int from, int to, List<Point3D> points)
    {
        for (var t = from; t <= to; t++)
        {
            // The near side of a gate is a step before its far side, and the far side is the one worth walking to.
            if (t < to && route.RunOf[t + 1] != route.RunOf[t])
            {
                continue;
            }

            points.Add(new Point3D(route.X[t], route.Y[t], route.Z[t]));
        }
    }

    /// <summary>
    /// Joins the two ends of a request to their clusters as the planner joins them: each anchored within
    /// <paramref name="anchorage"/>, the start's costs to every state of its cluster and the goal's from every state of its.
    /// </summary>
    private bool Ends(NavGraph graph, NavWindowCache cache, int anchorage, Point3D from, Point3D to)
    {
        if (!graph.Contains(from.X, from.Y) || !graph.Contains(to.X, to.Y))
        {
            return false;
        }

        var sc = graph.ClusterOf(from.X, from.Y);
        var gc = graph.ClusterOf(to.X, to.Y);

        if (sc == gc || !graph.IsJoined(sc) || !graph.IsJoined(gc))
        {
            return false;
        }

        var startEnd = End(from, anchorage);

        if (_startGraph != graph.Id || _startEnd != startEnd || _startVersion != graph.ClusterVersion(sc))
        {
            _startGraph = 0;

            var ws = Window(graph, cache, sc, _ownStart);
            var sx = from.X;
            var sy = from.Y;

            if (!HierarchicalPlanner.Anchor(ws, ref sx, ref sy, from.Z, anchorage, out var ss))
            {
                return false;
            }

            Array.Copy(ws.Flood(sx, sy, ss, false), _startCost, NavWindow.States);
            _startGraph = graph.Id;
            _startEnd = startEnd;
            _startVersion = graph.ClusterVersion(sc);
        }

        var goalEnd = End(to, anchorage);

        if (_goalGraph != graph.Id || _goalEnd != goalEnd || _goalVersion != graph.ClusterVersion(gc))
        {
            _goalGraph = 0;

            var wg = Window(graph, cache, gc, _ownGoal);
            var gx = to.X;
            var gy = to.Y;

            if (!HierarchicalPlanner.Anchor(wg, ref gx, ref gy, to.Z, anchorage, out var gs))
            {
                return false;
            }

            Array.Copy(wg.Flood(gx, gy, gs, true), _goalCost, NavWindow.States);
            _goalGraph = graph.Id;
            _goalEnd = goalEnd;
            _goalVersion = graph.ClusterVersion(gc);
        }

        _sc = sc;
        _gc = gc;

        return true;
    }

    private static long End(Point3D at, int anchorage) =>
        ((long)(at.X & 0xFFFF) << 32) | ((long)(at.Y & 0xFFFF) << 16) | ((long)(byte)(sbyte)Math.Clamp(at.Z, -128, 127) << 8)
        | (byte)anchorage;

    private static NavWindow Window(NavGraph graph, NavWindowCache cache, int cluster, NavWindow own)
    {
        if (cache != null)
        {
            return cache.Get(graph, cluster);
        }

        own.Reset(graph.Terrain, cluster, cluster % graph.ClustersX * NavGraph.Side, cluster / graph.ClustersX * NavGraph.Side);

        return own;
    }

    /// <summary>A road from a planned path: its nodes where they stand, the ground cost along it, and its runs of clusters.</summary>
    private NavRememberedRoute Make(NavGraph graph, IReadOnlyList<int> path, int goal)
    {
        var n = path.Count;
        var route = new NavRememberedRoute(graph.Id, goal, n);
        var runs = 0;

        for (var t = 0; t < n; t++)
        {
            var id = path[t];

            route.Ids[t] = id;
            route.X[t] = (short)graph.X(id);
            route.Y[t] = (short)graph.Y(id);
            route.Z[t] = (sbyte)graph.Z(id);
            route.S[t] = (byte)graph.Stratum(id);

            if (t > 0)
            {
                var step = Edge(graph, path[t - 1], id);

                if (step < 0)
                {
                    return null;
                }

                route.Cum[t] = route.Cum[t - 1] + step;
            }

            if (t == 0 || graph.ClusterOfNode(id) != graph.ClusterOfNode(path[t - 1]))
            {
                runs++;
            }
        }

        if (graph.ClusterOfNode(path[n - 1]) != goal)
        {
            return null;
        }

        route.Runs(runs);

        var k = -1;

        for (var t = 0; t < n; t++)
        {
            var c = graph.ClusterOfNode(path[t]);

            if (t == 0 || c != graph.ClusterOfNode(path[t - 1]))
            {
                k++;
                route.RunCluster[k] = c;
                route.RunStart[k] = t;
                route.RunSig[k] = Signature(graph, c);
                route.RunStamp[k] = graph.ClusterVersion(c);
            }

            route.RunOf[t] = k;
        }

        route.RunStart[runs] = n;
        route.Bound();

        return route;
    }

    /// <summary>The cheapest edge from one node to another, or -1 when the graph has none.</summary>
    private static int Edge(NavGraph graph, int a, int b)
    {
        if (!graph.EdgesOf(a, out var to, out var cost, out var from, out var until))
        {
            return -1;
        }

        var best = -1;

        for (var e = from; e < until; e++)
        {
            if (to[e] == b && (best < 0 || cost[e] < best))
            {
                best = cost[e];
            }
        }

        return best;
    }

    private ulong Signature(NavGraph graph, int c)
    {
        var key = ((long)graph.Id << 32) | (uint)c;
        var version = graph.ClusterVersion(c);

        if (_signatures.TryGetValue(key, out var known) && known.Version == version)
        {
            return known.Sig;
        }

        var sig = Sign(graph, c);

        if (_signatures.Count >= SignatureCapacity)
        {
            _signatures.Clear();
        }

        _signatures[key] = (version, sig);

        return sig;
    }

    /// <summary>
    /// A cluster's nodes and edges as one number, independent of node ids and of the order they were made in: two clusters
    /// drawn from the same ground sign the same. Each node by where it stands; each edge by where it leads and its cost.
    /// </summary>
    public static ulong Sign(NavGraph graph, int c)
    {
        var nodes = graph.NodesOf(c);

        if (nodes == null)
        {
            return 0;
        }

        unchecked
        {
            var sum = 0UL;

            for (var i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                var h = Mix(((ulong)(ushort)graph.X(n) << 32) | ((ulong)(ushort)graph.Y(n) << 16) | ((ulong)(byte)graph.Z(n) << 8) | (byte)graph.Stratum(n));
                var edges = 0UL;

                if (graph.EdgesOf(n, out var to, out var cost, out var from, out var until))
                {
                    for (var e = from; e < until; e++)
                    {
                        var m = to[e];

                        edges += Mix(
                            ((ulong)(ushort)graph.X(m) << 48) | ((ulong)(ushort)graph.Y(m) << 32) | ((ulong)(byte)graph.Stratum(m) << 24)
                            | ((uint)cost[e] & 0xFFFFFF)
                        );
                    }
                }

                sum += Mix(h + edges * 0x9E3779B97F4A7C15UL);
            }

            return Mix(sum ^ (ulong)nodes.Count);
        }
    }

    private static ulong Mix(ulong z)
    {
        unchecked
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;

            return z ^ (z >> 31);
        }
    }

    private void Insert(Candidate c)
    {
        _candidates.Add(c);

        for (var j = _candidates.Count - 1; j > 0 && _candidates[j].Cost < _candidates[j - 1].Cost; j--)
        {
            (_candidates[j], _candidates[j - 1]) = (_candidates[j - 1], _candidates[j]);
        }
    }

    private void Used(NavRememberedRoute route)
    {
        route.Used = ++_clock;
        route.Uses++;
    }

    private void Add(NavRememberedRoute route)
    {
        route.Slot = _routes.Count;
        _routes.Add(route);
        Nodes += route.Count;
        Dirty = true;
    }

    private void Remove(NavRememberedRoute route)
    {
        if (route.Slot < 0)
        {
            return;
        }

        for (var t = 0; t < route.Count; t++)
        {
            var key = Key(route.GraphId, route.X[t], route.Y[t], route.S[t], route.Goal);

            if (_index.TryGetValue(key, out var place) && ReferenceEquals(place.Route, route))
            {
                _index.Remove(key);
            }
        }

        var slot = route.Slot;
        var tail = _routes[^1];

        _routes[slot] = tail;
        tail.Slot = slot;
        _routes.RemoveAt(_routes.Count - 1);
        route.Slot = -1;
        route.Keys = 0;
        Nodes -= route.Count;
        Dirty = true;
    }

    /// <summary>The least lately used roads go until the memory is inside both its bounds.</summary>
    private void Trim()
    {
        while (_routes.Count > 0 && (_routes.Count > Capacity || Nodes > NodeCapacity))
        {
            var oldest = _routes[0];

            for (var i = 1; i < _routes.Count; i++)
            {
                if (_routes[i].Used < oldest.Used)
                {
                    oldest = _routes[i];
                }
            }

            Evicted++;
            Remove(oldest);
        }
    }

    /// <summary>Whether the map is small enough for the places' keys: 8192 tiles a side and 131072 clusters.</summary>
    private static bool Fits(NavGraph graph) =>
        graph.Terrain.Width <= 8192 && graph.Terrain.Height <= 8192 && graph.ClustersX * graph.ClustersY <= 131072;

    private bool Near(Point3D a, Point3D b) =>
        Math.Max(Math.Abs(a.X / NavGraph.Side - b.X / NavGraph.Side), Math.Abs(a.Y / NavGraph.Side - b.Y / NavGraph.Side)) < NearClusters;

    private static int StateIn(NavGraph graph, int cluster, int x, int y, int s) =>
        ((((y - cluster / graph.ClustersX * NavGraph.Side) << 4) | (x - cluster % graph.ClustersX * NavGraph.Side)) * NavWindow.MaxStrata) + s;

    private static long Key(int graph, int x, int y, int s, int goal) =>
        ((long)(graph & 0xFFFF) << 45) | ((long)(x & 0x1FFF) << 32) | ((long)(y & 0x1FFF) << 19) | ((long)(s & 3) << 17) | (uint)(goal & 0x1FFFF);

    private static long DetourKey(int graph, int start, int goal) => ((long)graph << 40) | ((long)start << 20) | (uint)goal;

    /// <summary>
    /// One line: what is kept, what was served, what was offered and what was forgotten. The price of a plan is the caller's to
    /// say; see <c>NavigationService.Describe</c>.
    /// </summary>
    public string Describe() =>
        !Enabled
            ? "routes are not remembered"
            : $"{Count} roads remembered over {Nodes} nodes ({Places} places to join them) and {Detours} ways round the danger; "
              + $"{Asked} asked of it: {Served} served ({Middles} joined in the middle), {Detoured} round the danger, {Missed} not remembered, "
              + $"{Bloodied} through blood now, {Waiting} across ground being drawn again, {Unjoined} places whose road the goal could not join; "
              + $"{Offered} offered: {Stored} kept new, {Improved} cheaper than what was held, {Kept} no cheaper, {Declined} declined; "
              + $"forgotten: {Invalidated} as the ground changed ({Rejoined} clusters drawn again the same and kept), {Superseded} superseded, "
              + $"{Expired} planned round a shunned gate and past it, {Evicted} for room, {Forgotten} beside a house or a gate that would not pass; ways round: {DetoursStored} kept, "
              + $"{DetoursDropped} dropped; {Read} read from disk";
}

/// <summary>
/// One remembered road. The arrays the file writes (<see cref="NavRouteRecord"/>) are filled once when the road is made and
/// never changed after, so a worker may write them while the loop goes on; the node ids and the clusters' stamps are the
/// loop's, and are looked up again whenever a cluster is proved by its signature.
/// </summary>
internal sealed class NavRememberedRoute
{
    public readonly int GraphId;

    /// <summary>The cluster the road ends in.</summary>
    public readonly int Goal;

    public readonly int Count;

    public readonly short[] X;

    public readonly short[] Y;

    public readonly sbyte[] Z;

    public readonly byte[] S;

    /// <summary>The ground cost from the first node to each, in <see cref="NavCost"/> units.</summary>
    public readonly int[] Cum;

    /// <summary>The run each node belongs to: a run is the nodes the road has in one cluster, one after another.</summary>
    public readonly int[] RunOf;

    public int[] RunCluster = [];

    /// <summary>The first node of each run, and one more entry: the node count.</summary>
    public int[] RunStart = [];

    public ulong[] RunSig = [];

    /// <summary>The loop's: the version each run's cluster had when last proved; -1 for never.</summary>
    public int[] RunStamp = [];

    /// <summary>The loop's: node ids, looked up again from X, Y and S whenever a cluster is proved by signature.</summary>
    public readonly int[] Ids;

    public int MinX;

    public int MinY;

    public int MaxX;

    public int MaxY;

    public long Used;

    public int Uses;

    /// <summary>Places the index holds for this road; nought and it is dropped.</summary>
    public int Keys;

    /// <summary>Where it stands in the memory's list; -1 once forgotten.</summary>
    public int Slot = -1;

    /// <summary>The offer this road was last proved for, and what the proof said. See NavRouteMemory.Offer.</summary>
    internal int Offer;

    internal NavProof Standing;

    public long DangerThen;

    public long GroundDangerThen;

    /// <summary>A detour always, a ground road only when planned while a gate was shunned: when it is to be forgotten.</summary>
    public bool Expires;

    public long Until;

    public NavRememberedRoute(int graphId, int goal, int count)
    {
        GraphId = graphId;
        Goal = goal;
        Count = count;
        X = new short[count];
        Y = new short[count];
        Z = new sbyte[count];
        S = new byte[count];
        Cum = new int[count];
        RunOf = new int[count];
        Ids = new int[count];
    }

    public void Runs(int runs)
    {
        RunCluster = new int[runs];
        RunStart = new int[runs + 1];
        RunSig = new ulong[runs];
        RunStamp = new int[runs];
    }

    public void Bound()
    {
        MinX = int.MaxValue;
        MinY = int.MaxValue;
        MaxX = int.MinValue;
        MaxY = int.MinValue;

        for (var t = 0; t < Count; t++)
        {
            MinX = Math.Min(MinX, X[t]);
            MinY = Math.Min(MinY, Y[t]);
            MaxX = Math.Max(MaxX, X[t]);
            MaxY = Math.Max(MaxY, Y[t]);
        }
    }

    public bool Through(int x, int y)
    {
        if (x < MinX || x > MaxX || y < MinY || y > MaxY)
        {
            return false;
        }

        for (var t = 0; t < Count; t++)
        {
            if (X[t] == x && Y[t] == y)
            {
                return true;
            }
        }

        return false;
    }

    public NavRouteRecord Record() =>
        new()
        {
            Goal = Goal,
            X = X,
            Y = Y,
            Z = Z,
            S = S,
            Cum = Cum,
            RunCluster = RunCluster,
            RunStart = RunStart,
            RunSig = RunSig,
            Uses = Uses,
            Used = Used
        };

    /// <summary>
    /// A road read from the file: nothing proved yet, every stamp -1 and every id unknown. Null when the record does not hold
    /// together — a node outside the cluster its run names, a stratum the windows do not have — since the file is not trusted.
    /// </summary>
    public static NavRememberedRoute From(NavGraph graph, NavRouteRecord record)
    {
        var n = record.X?.Length ?? 0;
        var runs = record.RunCluster?.Length ?? 0;

        if (n == 0 || runs == 0 || record.Y.Length != n || record.Z.Length != n || record.S.Length != n || record.Cum.Length != n
            || record.RunStart.Length != runs + 1 || record.RunSig.Length != runs || record.RunStart[0] != 0 || record.RunStart[runs] != n
            || record.RunCluster[runs - 1] != record.Goal)
        {
            return null;
        }

        var route = new NavRememberedRoute(graph.Id, record.Goal, n);

        Array.Copy(record.X, route.X, n);
        Array.Copy(record.Y, route.Y, n);
        Array.Copy(record.Z, route.Z, n);
        Array.Copy(record.S, route.S, n);
        Array.Copy(record.Cum, route.Cum, n);
        route.Runs(runs);
        Array.Copy(record.RunCluster, route.RunCluster, runs);
        Array.Copy(record.RunStart, route.RunStart, runs + 1);
        Array.Copy(record.RunSig, route.RunSig, runs);
        Array.Fill(route.RunStamp, -1);
        Array.Fill(route.Ids, -1);

        for (var k = 0; k < runs; k++)
        {
            if (record.RunStart[k + 1] <= record.RunStart[k])
            {
                return null;
            }

            for (var t = record.RunStart[k]; t < record.RunStart[k + 1]; t++)
            {
                if (record.X[t] < 0 || record.Y[t] < 0 || !graph.Contains(record.X[t], record.Y[t])
                    || graph.ClusterOf(record.X[t], record.Y[t]) != record.RunCluster[k] || record.S[t] >= NavWindow.MaxStrata)
                {
                    return null;
                }

                route.RunOf[t] = k;
            }
        }

        route.Uses = record.Uses;
        route.Bound();

        return route;
    }
}

/// <summary>A remembered road as the file keeps it. See <see cref="NavRouteFile"/>.</summary>
public sealed class NavRouteRecord
{
    public int Goal;

    public short[] X;

    public short[] Y;

    public sbyte[] Z;

    public byte[] S;

    public int[] Cum;

    public int[] RunCluster;

    public int[] RunStart;

    public ulong[] RunSig;

    public int Uses;

    /// <summary>The memory's clock when last used; not written, only for ordering what is.</summary>
    public long Used;
}
