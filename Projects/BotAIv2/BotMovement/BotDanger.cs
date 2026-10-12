using System;
using Server.Engines.Pathing.Tiered;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// <b>The road goes round the danger (29.09.2026, Patrick's second answer of the morning).</b> "Guilds have the right to
/// stay in other towns; if the road is dangerous, through the bog for instance, they can go by the north where the road
/// is not so dangerous." The tiered planner routed by distance alone, so The Needle's bots walked from Cove to Britain
/// through the plague bog every time, and the delve's march went past the orc fort. This is the hook the planners ask
/// at every node (<c>NavigationService.Danger</c>): a node on a quadrant the map reads as bloodied (<c>BotQuad.Muscle</c>)
/// costs <see cref="BloodCost"/> tiles more, one inside barred or quarantined ground <see cref="BarredCost"/> more, so a
/// detour up to that many tiles is taken before the blood is crossed. The knowledge is the same the samplers and the
/// travellers already read; this is the walk reading it too.
///
/// <para>
/// <b>Priced for the walker, not for the median bot (01.10.2026, build 388; Patrick's plan v2, point 5: "bots know how to
/// go round the zones, with a margin or without, to pass without trouble").</b> On build 386, 71 % of 701 deaths fell inside
/// a deadly zone and 124 of 139 prowl deaths more than a hundred tiles from the prowl's target: the road killed, not the
/// goal. The zones' price on a node was read against the median bot (<c>BotZone.VsBot</c>), the same for a novice alone
/// and for a company of eight, and the goal's ground was the only ground any proposer weighed. Now the walk sets who is
/// walking before a route is asked (<see cref="For"/>): a zone's fight is read against that walker's own strength — its
/// company's when it is in one — so a weak bot pays the fringe and keeps the margin while a company whose strength the
/// fight does not reach <see cref="DeadlyVsWalker"/> times is charged the hostile price and may cut through; and the
/// straight road to far work is weighed before the work is offered (<see cref="Road"/>), by the record of deaths along it
/// and by the patrols it crosses. Costs stay bounded: no node costs more than <see cref="BarredCost"/> tiles, and a node
/// the reach map says no walk from home reaches is refused outright (<see cref="NavCost.Unaffordable"/>, which the planners
/// decline to relax) rather than priced — Patrick's plan v2, point 6.
/// </para>
///
/// <para>
/// <b>And where the straight line refuses, the road the planner would draw is judged instead (06.10.2026, road15).</b> The
/// line refused 88 % of all far roads on build 400 for squares nobody would walk through. See <see cref="Judge"/> in
/// <c>BotDangerRoute.cs</c>.
/// </para>
/// </summary>
public static partial class BotDanger
{
    /// <summary>
    /// Off between 12:28 and 12:40 on 29.09.2026 after two hangs of the loop inside <c>StrategicPlanner.Corridor</c> with the
    /// hook on; on again since the planners bound their parent-chain walks (build 306). See DECISIONS 302, 305, 306.
    /// </summary>
    public static bool Running { get; set; } = true;

    /// <summary>Tiles of detour a route accepts to keep off one bloodied node. Sixty.</summary>
    public static int BloodCost { get; set; } = 60;

    /// <summary>
    /// Tiles of detour a route accepts to keep off one barred or quarantined node. Eight hundred. Two hundred for the
    /// first hour, and ten of the twenty-nine deaths of that hour were to the trolls inside the standing bar north of
    /// Britain, on the road to the shops of the north — three nodes through it cost six hundred, and the road round the
    /// mountains cost more. Five thousand for four minutes at 11:34, and the loop hung at 11:38 with the process spinning:
    /// a cost of five hundred thousand a node over a bar two hundred tiles across overflows the planners' int costs, and
    /// an overflowed cost is a node that always relaxes. The planners now refuse a negative cost (NavCost.Unaffordable).
    /// </summary>
    public static int BarredCost { get; set; } = 800;

    /// <summary>
    /// <b>The zones' price on a node (30.09.2026, build 373).</b> Patrick's order that evening: where no creature patrols, a
    /// bot routes through without a second thought; where a patrol or its aggro reach could touch a walker, the ground is
    /// potentially dangerous. <see cref="BotZones.LevelAt(Map, int, int, out float, out float)"/> answers per node from the
    /// zones' raster: a node in an aggro fringe costs <see cref="EdgePerBot"/> tiles for each time the zone outweighs the
    /// walker, up to <see cref="EdgeMost"/>; inside a hostile patrol <see cref="HostilePerBot"/> a time, between
    /// <see cref="HostileLeast"/> and <see cref="HostileMost"/>; a deadly zone <see cref="DeadlyCost"/>. Clear ground and
    /// hunting grounds cost nothing. Bounded on purpose (see <see cref="BarredCost"/>'s overflow), and the larger of this and
    /// the blood is taken, not the sum.
    /// </summary>
    public static bool ZonesPriced { get; set; } = true;

    public static int EdgePerBot { get; set; } = 10;

    public static int EdgeMost { get; set; } = 40;

    public static int HostilePerBot { get; set; } = 30;

    public static int HostileLeast { get; set; } = 20;

    public static int HostileMost { get; set; } = 150;

    public static int DeadlyCost { get; set; } = 300;

    // ------------------------------------------------------------------------------------------------ by the walker

    /// <summary>Whether a route is priced against the walker that asked for it, when the walk says who that is. See the class note.</summary>
    public static bool ByWalker { get; set; } = true;

    /// <summary>
    /// A deadly zone whose fight is at least this many times the walker's strength is priced deadly for it; one whose fight
    /// falls short of that is priced as a hostile patrol — the company cuts through. Three: the smallest whole "several
    /// times", the same number <c>BotPeril.Overwhelm</c> keeps work out of ground by.
    /// </summary>
    public static double DeadlyVsWalker { get; set; } = 3.0;

    /// <summary>
    /// Tiles of detour a route accepts to keep off one bloodied node whose record asks more than <see cref="RoadDare"/> times
    /// the walker's strength: the square has killed things stronger than this walker. The plain <see cref="BloodCost"/> for a
    /// square it outweighs.
    /// </summary>
    public static int BloodBeyond { get; set; } = 300;

    /// <summary>
    /// Whether a node on ground the reach map says no walk from home reaches is refused to a walker standing on open ground
    /// (and a node on land to a walker aboard a ship), so no route point is ever built on it and no search spends budget
    /// there. Patrick's plan v2, point 6. The planners decline a cost past <see cref="NavCost.Unaffordable"/>.
    /// </summary>
    public static bool RefusesNodes { get; set; } = true;

    private static Mobile _walker;

    private static Map _walkerMap;

    private static double _strength;

    private static bool _standsOpen;

    private static bool _afloat;

    /// <summary>The reach of what this walker has seen and cannot beat, priced on its route. See <see cref="SightCost"/>.</summary>
    private static BotReaches _sighted;

    /// <summary>
    /// <b>Tiles of detour a route accepts to keep one node out of the reach of a creature the walker has seen and cannot beat
    /// (03.10.2026, sight13; Patrick: "the bot has sight around itself — why can it not see the enemy before it aggroes on it,
    /// and go round it?").</b> Two hundred: past the hostile patrol's most (<see cref="HostileMost"/>, 150), under the deadly
    /// zone's 300, because the zones are a forecast and this is a creature standing there now. Only for the walker that saw it
    /// (<see cref="For"/>); the larger of this and the zones' price is taken, never the sum. Nought turns it off.
    /// </summary>
    public static int SightCost { get; set; } = 200;

    /// <summary>Route nodes priced for lying in the reach of a creature the walker had seen.</summary>
    public static long InSight { get; private set; }

    /// <summary>Routes priced for a walker, and nodes of a deadly zone priced hostile because the walker's strength reached it.</summary>
    public static long ForWalkers { get; private set; }

    public static long CutThrough { get; private set; }

    /// <summary>Bloodied nodes charged <see cref="BloodBeyond"/> for a record past the walker's strength.</summary>
    public static long BeyondWalker { get; private set; }

    /// <summary>Nodes refused outright for lying on ground the reach map says the walker cannot reach. See <see cref="RefusesNodes"/>.</summary>
    public static long RefusedNodes { get; private set; }

    /// <summary>
    /// Who is about to ask for a route. Set by the walk round its draw and cleared after it; routes asked with nobody set
    /// (the travel book, the delve's seat-to-gate, the door) are priced against the median bot as before.
    /// </summary>
    public static void For(Mobile walker)
    {
        _walker = null;
        _sighted = null;

        if (!ByWalker || walker?.Map == null || walker.Map == Map.Internal)
        {
            return;
        }

        _sighted = SightCost > 0 ? (walker as BotMobile)?.Journey?.Sighted : null;

        _walker = walker;
        _walkerMap = walker.Map;
        _strength = Math.Max(1.0, BotQuad.Strength(walker));
        _standsOpen = BotReachMap.Standing(walker.Map, walker.Location, out var want) && want == BotReachClass.Open;
        _afloat = want == BotReachClass.Water;
        ForWalkers++;
    }

    public static void Clear()
    {
        _walker = null;
        _sighted = null;
    }

    /// <summary>Hands the hook to the planners. Called once at the module's start.</summary>
    public static void Hook()
    {
        NavigationService.Danger = Cost;
        Booted();
    }

    /// <summary>Nodes priced by the zones, by level.</summary>
    public static long InFringe { get; private set; }

    public static long InHostile { get; private set; }

    public static long InDeadly { get; private set; }

    /// <summary>Nodes asked about, and how many read bloodied or barred.</summary>
    public static long Asked { get; private set; }

    public static long Bloodied { get; private set; }

    public static long Barred { get; private set; }

    /// <summary>The extra cost of the node at (x, y), in the planner's units. See the class note.</summary>
    public static int Cost(int x, int y)
    {
        if (!Running)
        {
            return 0;
        }

        var walker = _walker;
        var map = walker != null ? _walkerMap : BotPopulation.Home ?? Map.Felucca;

        if (map == null)
        {
            return 0;
        }

        Asked++;

        // <b>No route point on ground no walk reaches (plan v2, point 6).</b> A walker on open ground is never routed through a
        // pocket, a roof, water or rock; one aboard a ship never over land. Unaffordable, not expensive: the planners refuse
        // to relax such a node at all, so the search does not expand into it.
        if (RefusesNodes && walker != null && (_standsOpen || _afloat))
        {
            var kind = BotReachMap.Class(map, x, y);

            if (kind != BotReachClass.Unknown && (_afloat ? kind != BotReachClass.Water : kind != BotReachClass.Open))
            {
                RefusedNodes++;

                return NavCost.Unaffordable;
            }
        }

        var at = new Point3D(x, y, 0);

        if (BotBarred.Holds(map, at))
        {
            Barred++;

            return BarredCost * NavCost.Step;
        }

        var tiles = 0;
        var record = BotQuad.Muscle(map, at);

        if (record > 0.0)
        {
            Bloodied++;
            tiles = BloodCost;

            if (walker != null && record > _strength * RoadDare)
            {
                BeyondWalker++;
                tiles = Math.Max(tiles, BloodBeyond);
            }
        }

        if (ZonesPriced)
        {
            var level = BotZones.LevelAt(map, x, y, out var vsBot, out var threat);

            // Against this walker when there is one — its company's strength, when it is in one — and the median bot otherwise.
            var vs = walker != null ? threat / _strength : vsBot;

            var zoned = level switch
            {
                BotZoneLevel.Deadly => walker != null && vs < DeadlyVsWalker ? Hostile(vs) : DeadlyCost,
                BotZoneLevel.Hostile => Hostile(vs),
                BotZoneLevel.Edge => Math.Clamp((int)(EdgePerBot * vs), 0, EdgeMost),
                _ => 0
            };

            if (zoned > 0)
            {
                switch (level)
                {
                    case BotZoneLevel.Deadly:
                        InDeadly++;

                        if (zoned < DeadlyCost)
                        {
                            CutThrough++;
                        }

                        break;
                    case BotZoneLevel.Hostile:
                        InHostile++;
                        break;
                    default:
                        InFringe++;
                        break;
                }

                tiles = Math.Max(tiles, zoned);
            }
        }

        // Something the walker has seen standing near and cannot beat: this node is in its reach now, whatever the zones say.
        if (_sighted != null && walker != null && _sighted.Within(x, y, 0))
        {
            InSight++;
            tiles = Math.Max(tiles, SightCost);
        }

        // Dearer at night (07.10.2026, events17): the same ground, priced BotNight.NightDanger times. See BotNight.
        tiles = BotNight.Node(tiles);

        return Math.Min(tiles, BarredCost) * NavCost.Step;
    }

    private static int Hostile(double vs) => Math.Clamp((int)(HostilePerBot * vs), HostileLeast, HostileMost);

    // ------------------------------------------------------------------------------------------------ the road, before the work

    /// <summary>Whether the straight road to far work is weighed before the work is offered. See <see cref="Road"/>.</summary>
    public static bool Roads { get; set; } = true;

    /// <summary>
    /// How many times what the walker brings a square on its road may ask before the road is refused. One and a half: the
    /// herb woods' and the seams' <c>MineOdds</c>, and the traders' <c>RoadDare</c> (build 387).
    /// </summary>
    public static double RoadDare { get; set; } = 1.5;

    /// <summary>Tiles between two looks at the road. Thirty-two: a quadrant is thirty, and a patrol cell is four.</summary>
    public static int RoadStep { get; set; } = 32;

    /// <summary>
    /// The share of a patrol zone's fight a walker crossing it is charged with. A half: the fight is the strongest of the zone
    /// in full and whatever can close on it at once, which is what a bot that stops there meets; one that passes meets some
    /// of it. Nought reads no zone on the road, only the record of deaths.
    /// </summary>
    public static double ZoneRoadShare { get; set; } = 0.5;

    /// <summary>Roads weighed, and roads refused: by the record of deaths on a square, and by a patrol crossed.</summary>
    public static long RoadsAsked { get; private set; }

    public static long RoadsRefused { get; private set; }

    public static long RoadsByRecord { get; private set; }

    public static long RoadsByZone { get; private set; }

    /// <summary>
    /// Loop time the road check has cost, in ticks of the stopwatch, and the samples it took: on 388 it was asked 11 million
    /// times in nine hours (340 a second) and refused 92 %, and nobody had measured what that cost (02.10.2026).
    /// </summary>
    private static long _roadTicks;

    public static long RoadSamples { get; private set; }

    public static double RoadMs => _roadTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

    /// <summary>
    /// Whether the straight road from <paramref name="from"/> to <paramref name="to"/> crosses ground that asks more than
    /// <paramref name="brought"/> × <see cref="RoadDare"/> of strength: every <see cref="RoadStep"/> tiles, the quadrant record
    /// (<see cref="BotQuad.Muscle"/>: what lives and has killed there, learned from deaths and forgotten as they age) and, at
    /// <see cref="ZoneRoadShare"/>, the fight of a patrol zone the road crosses — never the zone the walker already stands in,
    /// which is where it lives, nor the zone of the goal, which is the proposer's own question (<c>BotHunter.Zoned</c>,
    /// <c>BotHerbs.Perilous</c>); never a square inside a guarded town. The ends are not read. True when refused, with the
    /// strength asked, the square that asked it and which record said so.
    ///
    /// <para>
    /// <b>Why the straight line and not the route.</b> The route is drawn when the walk begins, after the work is taken, and
    /// goes round what it can; this is asked before the work is offered, of every candidate the proposers weigh, so it has to
    /// be a few array reads. A road refused here that the planner could have taken round is a walk not taken; on 386 the walks
    /// taken died at 2.1 a bot-hour.
    /// </para>
    ///
    /// <para>
    /// <b>Since 06.10.2026 the line is the first question and not the last.</b> A road it refuses is judged by the road the
    /// planner would draw, for a walker standing still, within a budget (<see cref="RouteJudged"/>, <c>BotDangerRoute.cs</c>):
    /// on build 400 the line refused 88 % of 64 million far roads. <paramref name="trade"/> names the work for the tally by
    /// trade, <paramref name="busy"/> that the walker holds work (its searches are rationed); <see cref="RoadHow"/> says afterwards
    /// which of the two refused.
    /// </para>
    /// </summary>
    public static bool Road(Mobile body, Map map, Point3D from, Point3D to, double brought, out double asked, out Point3D at, out string what, string trade = null, bool busy = false)
    {
        asked = 0.0;
        at = Point3D.Zero;
        what = null;
        RoadHow = null;

        var step = Math.Max(8, RoadStep);

        // A walker dares less of a road at night (07.10.2026, events17): the road's danger ×BotNight.NightDanger. See BotNight.
        var dare = BotNight.Dare(RoadDare);

        if (!Running || !Roads || dare <= 0.0 || body == null || map == null || map == Map.Internal)
        {
            return false;
        }

        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Max(Math.Abs(dx), Math.Abs(dy));

        if (length <= step)
        {
            return false;
        }

        RoadsAsked++;

        var tally = trade == null ? null : TallyOf(trade);

        if (tally != null)
        {
            tally.Asked++;
        }

        var began = System.Diagnostics.Stopwatch.GetTimestamp();
        var refused = Line(map, from, to, brought, step, dare, out asked, out at, out what, out var byZone);

        _roadTicks += System.Diagnostics.Stopwatch.GetTimestamp() - began;

        // The road the planner would draw, where the line refuses (and, by RouteAlways, where it does not). See Judge.
        if (RouteJudged && (refused || RouteAlways))
        {
            refused = Judge(body, map, from, to, brought, step, dare, refused, trade, busy, tally, ref asked, ref at, ref what, ref byZone);
        }
        else if (refused)
        {
            LineRefused++;
            RoadHow = "on the straight line";

            if (tally != null)
            {
                tally.Line++;
            }
        }

        if (refused)
        {
            RoadsRefused++;

            if (byZone)
            {
                RoadsByZone++;
            }
            else
            {
                RoadsByRecord++;
            }
        }

        return refused;
    }

    /// <summary>
    /// The straight line's own answer, as <see cref="Road"/> gave it alone until 06.10.2026: nothing counted here. True when
    /// refused.
    /// </summary>
    private static bool Line(Map map, Point3D from, Point3D to, double brought, int step, double dare, out double asked, out Point3D at, out string what, out bool byZone)
    {
        asked = 0.0;
        at = Point3D.Zero;
        what = null;
        byZone = false;

        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Max(Math.Abs(dx), Math.Abs(dy));

        var bar = Math.Max(1.0, brought) * dare;
        var standing = BotZones.ZoneAt(map, from.X, from.Y)?.Id ?? 0;
        var ending = BotZones.ZoneAt(map, to.X, to.Y)?.Id ?? 0;
        var samples = length / step;

        for (var i = 1; i < samples; i++)
        {
            var here = new Point3D(from.X + dx * i / samples, from.Y + dy * i / samples, 0);

            RoadSamples++;

            // The town is the town: guarded ground on the way asks nothing, whatever a zone's paint says of it.
            if (Region.Find(here, map)?.IsPartOf<GuardedRegion>() == true)
            {
                continue;
            }

            var record = BotQuad.Muscle(map, here);

            if (record > bar)
            {
                asked = record;
                at = here;
                what = "the record of what has killed there";

                return true;
            }

            if (ZoneRoadShare <= 0.0)
            {
                continue;
            }

            // The fight at this very tile since build 389 (the fight raster), not the zone's worst place: a road along the
            // quiet side of a camp is charged with the quiet side.
            var level = BotZones.LevelAt(map, here.X, here.Y, out _, out var threat);

            if (level is not (BotZoneLevel.Deadly or BotZoneLevel.Hostile))
            {
                continue;
            }

            var zone = BotZones.ZoneAt(map, here.X, here.Y)?.Id ?? 0;

            if (zone == 0 || zone == standing || zone == ending)
            {
                continue;
            }

            var fight = threat * ZoneRoadShare;

            if (fight > bar)
            {
                asked = fight;
                at = here;
                what = $"the patrol of zone {zone}";
                byZone = true;

                return true;
            }
        }

        return false;
    }

    public static string Describe() =>
        !Running
            ? "the roads do not read the danger"
            : $"the roads read the danger: {Asked} nodes asked, {Bloodied} bloodied (+{BloodCost} tiles each, {BeyondWalker} of them +{BloodBeyond} for a record past the walker's strength ×{RoadDare:F1}) and {Barred} barred (+{BarredCost}); "
              + (ZonesPriced
                  ? $"the zones priced {InFringe} in an aggro fringe (+{EdgePerBot} a bot, at most {EdgeMost}), {InHostile} in a hostile patrol (+{HostilePerBot} a bot, {HostileLeast}–{HostileMost}) and {InDeadly} deadly (+{DeadlyCost}; {CutThrough} of them at the hostile price for a walker the fight did not reach ×{DeadlyVsWalker:F0}); "
                  : "the zones are not priced; ")
              + $"{InSight} nodes in the reach of a creature the walker had seen and could not beat (+{SightCost}); "
              + $"{ForWalkers} routes priced for the walker that asked ({(ByWalker ? "on" : "off")}), {RefusedNodes} nodes refused outright on ground the reach map says the walker cannot reach ({(RefusesNodes ? "on" : "off")}); "
              + $"roads weighed before far work was offered: {RoadsAsked} asked, {RoadsRefused} refused ({RoadsByRecord} by the record of deaths on a square, {RoadsByZone} by a patrol crossed at ×{ZoneRoadShare:F2} of its fight, against ×{RoadDare:F1} of what the work brings{(Roads ? "" : "; off")}), {RoadSamples} samples read in {RoadMs:F0}ms of the loop ({(RoadsAsked == 0 ? 0 : RoadMs * 1000.0 / RoadsAsked):F1}µs an ask); "
              + DescribeRoutes();

    public static void Forget()
    {
        Asked = 0;
        Bloodied = 0;
        Barred = 0;
        InFringe = 0;
        InHostile = 0;
        InDeadly = 0;
        ForWalkers = 0;
        CutThrough = 0;
        BeyondWalker = 0;
        RefusedNodes = 0;
        InSight = 0;
        RoadsAsked = 0;
        RoadsRefused = 0;
        RoadsByRecord = 0;
        RoadsByZone = 0;
        RoadSamples = 0;
        _roadTicks = 0;
        ForgetRoutes();
    }
}
