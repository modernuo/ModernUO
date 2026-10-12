using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Multis;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Finding ground a house will actually stand on.
///
/// <para>
/// <b>The engine is the only authority here and it is asked directly.</b> <c>HousePlacement.Check</c> knows
/// the five rules — nothing impassable touching the foundation, five clear tiles front and back, a flat
/// surface, no road underneath, no no-housing region — and it answers with which one was broken. Guessing
/// any part of that here would be a second opinion about the world, and this project has paid for those:
/// the fastest way to a house that cannot be placed is code that believes it knows where houses go.
/// </para>
///
/// <para>
/// <b>Trodden ground is preferred for the length of one look, and never required.</b> A hall in a corner
/// of the island nobody walks through is a hall whose forge no sweep will ever find, so
/// <see cref="BotQuad.Trodden"/> decides between two valid plots found in the same search. It does not
/// decide whether a plot is valid — that would be a veto dressed as a preference — and it does not
/// outlive the search either: holding the untrodden plot back until the whole spiral had been walked meant
/// holding it for an hour, and the shard spent twenty minutes reporting "1 would take a hall" while
/// nothing was ever offered to anybody.
/// </para>
///
/// <para>
/// The search is a spiral outwards from where the population lives, kept between calls, budgeted per call.
/// A full check costs the engine a 7x7 multi's worth of tile and item queries; forty-nine bots taking their
/// turn at that on one beat is a stall, and a stall is the thing this shard is least willing to pay.
/// </para>
/// </summary>
public static class BotPlot
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPlot));

    /// <summary>
    /// Which house goes up: the smallest classic one, <c>SmallOldHouse</c> 0x0064.
    ///
    /// Small because ground this engine will accept is the scarce thing, not money — the rules demand five
    /// clear tiles front and back of the foundation, and every tile of extra width is a worse chance of
    /// finding anywhere at all near a town.
    /// </summary>
    public static int MultiID { get; set; } = 0x0064;

    /// <summary>No nearer to the population's home than this. A hall on top of the muster point is in the way.</summary>
    public static int Near { get; set; } = 14;

    /// <summary>And no further, so the walk to it is part of the day rather than an expedition.</summary>
    public static int Far { get; set; } = 90;

    /// <summary>How far apart two halls must stand. Four guilds in a terrace is not a village.</summary>
    public static int Apart { get; set; } = 40;

    /// <summary>
    /// How far from another guild's hall a plot has to be before it is taken without looking further.
    ///
    /// <para>
    /// <b>Patrick's order of 13.09.2026: build away from the other clans, by preference.</b> A preference and
    /// not a veto, in the shape the trodden-ground rule already has: a valid plot inside this distance of
    /// somebody else's hall is held as the spare, and taken only if the look ends without one further off.
    /// Yards that touch are where the border drift and the wars came from.
    /// </para>
    /// </summary>
    public static int Shy { get; set; } = 120;

    /// <summary>Plots taken although a neighbour stood within <see cref="Shy"/>, for want of any further off.</summary>
    public static long Neighboured { get; private set; }

    /// <summary>
    /// How wide a belt round a no-housing region is kept clear as well.
    ///
    /// <para>
    /// <b>Patrick's order of 08.09.2026, and the reason is what he could see out of the window.</b> The
    /// engine forbids building <em>in</em> Britain's graveyard and says nothing about building against its
    /// railings, so the first hall this shard ever raised went up three tiles from the north fence. Twenty
    /// tiles is his figure. It applies to every <c>NoHousingRegion</c> on the map rather than to that one
    /// graveyard, because the rule he is stating is about what a house should not be pressed up against.
    /// </para>
    /// </summary>
    public static int Clearance { get; set; } = 20;

    /// <summary>How many candidates one call may put through the engine's check.</summary>
    public static int Budget { get; set; } = 60;

    /// <summary>Candidates put through the engine's own check.</summary>
    public static long Tested { get; private set; }

    /// <summary>Candidates the engine would take a house on.</summary>
    public static long Fit { get; private set; }

    /// <summary>Candidates with no floor to stand on — water, a hole, off the edge.</summary>
    public static long Floorless { get; private set; }

    /// <summary>Candidates with something standing on them: an item, a creature, another hall.</summary>
    public static long Occupied { get; private set; }

    /// <summary>Candidates passed over because a town watch stands there. Ours, and it comes before the engine.</summary>
    public static long InTown { get; private set; }

    /// <summary>Candidates passed over for standing too near ground the engine will not build on.</summary>
    public static long Shunned { get; private set; }

    /// <summary>Candidates the engine itself refused on the region: a graveyard, a dungeon, another house.</summary>
    public static long Forbidden { get; private set; }

    /// <summary>Candidates whose ground is not level under the whole footprint. Rule 4, and the one that bites.</summary>
    public static long Uneven { get; private set; }

    /// <summary>Candidates refused for the land: a road, a furrow, a slope against the wall.</summary>
    public static long BadLand { get; private set; }

    /// <summary>Candidates refused for a static: a tree, a rock, a fence, or a yard that is not clear.</summary>
    public static long BadStatic { get; private set; }

    /// <summary>Candidates refused for something immovable lying there.</summary>
    public static long BadItem { get; private set; }

    /// <summary>Candidates where the foundation would rest on nothing. Rule 4, as the engine reports it.</summary>
    public static long Surfaceless { get; private set; }

    /// <summary>How many times the spiral has been walked to its end without a plot.</summary>
    public static long Exhausted { get; private set; }

    /// <summary>The ring of offsets, furthest last, built once — and again if the bounds are dialled.</summary>
    private static List<Point2D> _ring;

    /// <summary>What <see cref="_ring"/> was built with, so a dialled bound is not a dial that does nothing.</summary>
    private static int _builtNear = -1;

    private static int _builtFar = -1;

    /// <summary>Ground kept clear round every no-housing region, grown by <see cref="Clearance"/>.</summary>
    private static List<Rectangle2D> _shunned;

    private static int _hallowedBy = -1;

    /// <summary>
    /// One search, from one origin: how far round the spiral it has got and what it has found.
    ///
    /// <para>
    /// <b>One per origin, because every guild now looks from its own seat.</b> The first cut kept a single
    /// cursor and a single remembered plot for the whole island, which was right while the whole island
    /// looked from one point; with five guilds looking from five seats a shared cursor would be a search
    /// that jumps three hundred tiles between one call and the next and remembers a plot found for the
    /// wrong guild. See <c>BotSeat</c>.
    /// </para>
    /// </summary>
    private sealed class Search
    {
        /// <summary>How far through <see cref="_ring"/> this search has got.</summary>
        public int Cursor;

        /// <summary>A valid plot on ground nobody walks, held in case the look ends without a better one.</summary>
        public Point3D Spare;

        public bool HasSpare;

        /// <summary>
        /// The last piece of ground found to be good, kept until something is built on it.
        ///
        /// <para>
        /// <b>Searching and paying are two different clocks and they were blocking each other.</b> A search
        /// runs only when somebody asks, somebody only asks when their guild can pay, and on this shard
        /// exactly one guild could pay — a guild of two — so the island was being examined at a fiftieth of
        /// the rate the population could have managed. Remembering the answer decouples them: whoever is
        /// nearest to affording a hall does the looking, and whoever can actually pay is handed ground that
        /// was found minutes ago.
        /// </para>
        /// </summary>
        public Point3D Kept;

        public bool HasKept;
    }

    private static readonly Dictionary<(int X, int Y, int Multi), Search> _searches = [];

    /// <summary>
    /// The next place a hall could go near the population's home, or false.
    ///
    /// <paramref name="by"/> is the bot that would place it: the engine's check reads its map and its access
    /// level, and a staff member may build anywhere, which is exactly the answer we do not want.
    /// </summary>
    public static bool Find(Mobile by, out Point3D centre) => Find(by, Point3D.Zero, Point3D.Zero, 0, out centre);

    /// <summary>The same, spiralling out from <paramref name="from"/> — a guild's seat. Nought means home.</summary>
    public static bool Find(Mobile by, Point3D from, out Point3D centre) => Find(by, from, Point3D.Zero, 0, out centre);

    /// <summary>
    /// The same, refusing any ground within <paramref name="clear"/> tiles of <paramref name="shun"/>.
    ///
    /// <para>
    /// <b>For a hall that is being moved rather than raised.</b> A guild that loses a war is put outside the
    /// winner's yard — see <c>BotExile</c> — and "outside" is a distance from one particular building, which
    /// is a thing this search had no way to be told. Passing <c>Point3D.Zero</c> and nought is the ordinary
    /// search and shuns nothing.
    /// </para>
    ///
    /// <para>
    /// The remembered plot is held to the same rule. Ground found for somebody else is perfectly good ground
    /// and it is quite likely to be next door to the winner, which is the one place this caller may not go.
    /// </para>
    /// </summary>
    public static bool Find(Mobile by, Point3D from, Point3D shun, int clear, out Point3D centre) =>
        Find(by, from, shun, clear, MultiID, null, out centre);

    /// <summary>
    /// The same, for a house of another size (<paramref name="multi"/>), and not counting <paramref name="replacing"/> — the
    /// hall a bigger one is going up to replace — as a neighbour. See <c>BotHallKind</c> and <c>BotEnlarge</c>: Patrick's
    /// order of 26.09.2026, a guild's hall grows with the guild. One search per origin and size, because ground that takes a
    /// small house says nothing about a large one.
    /// </summary>
    public static bool Find(Mobile by, Point3D from, Point3D shun, int clear, int multi, BaseHouse replacing, out Point3D centre) =>
        Find(by, from, shun, clear, multi, replacing, Mainland, out centre);

    /// <summary>The same for a bot's own house, which may stand on any land. See <see cref="Mainland"/> and <c>BotAbode</c>.</summary>
    public static bool FindHome(Mobile by, Point3D from, int multi, out Point3D centre) =>
        Find(by, from, Point3D.Zero, 0, multi, null, false, out centre);

    /// <summary>
    /// <b>A guild's buildings stand on the mainland (01.10.2026, build 379).</b> Patrick's order of the night of 30.09: «Guilds
    /// may buy and place halls only on the main island of Britannia». Every guild building found here — the first hall
    /// (<c>BotSteward</c>), a bigger one (<c>BotEnlarge</c>), an outpost, a hall carried away (<c>BotRemove</c>) — is on the
    /// land the population's home lies on (<see cref="BotGates.LandOf"/>, the lands joined by gates and teleporters); a search
    /// begun from a seat on an island begins from the home instead (<see cref="Ashore"/>), and nothing is found before the
    /// gates are read (<see cref="GatesUnread"/>). A bot's own house is not a guild's and keeps any land (<see cref="FindHome"/>).
    /// </summary>
    public static bool Mainland { get; set; } = true;

    /// <summary>Searches begun from an island seat and moved to the home, candidates passed over for lying off the mainland, and searches refused before the gates were read.</summary>
    public static long Ashore { get; private set; }

    public static long OffMainland { get; private set; }

    public static long GatesUnread { get; private set; }

    private static bool Find(
        Mobile by, Point3D from, Point3D shun, int clear, int multi, BaseHouse replacing, bool mainlandOnly, out Point3D centre
    )
    {
        centre = Point3D.Zero;

        var map = BotPopulation.Home;

        if (by == null || map == null || map == Map.Internal)
        {
            return false;
        }

        if (from == Point3D.Zero)
        {
            from = BotPopulation.Where;
        }

        var mainland = -1;

        if (mainlandOnly)
        {
            if (!BotGates.Ready)
            {
                GatesUnread++;

                return false;
            }

            mainland = BotGates.LandOf(map, BotPopulation.Where);

            if (mainland >= 0 && BotGates.LandOf(map, from) != mainland)
            {
                Ashore++;
                from = BotPopulation.Where;
            }
        }

        if (!_searches.TryGetValue((from.X, from.Y, multi), out var search))
        {
            search = new Search();
            _searches[(from.X, from.Y, multi)] = search;
        }

        // Already found, and still good. Re-checked rather than trusted: a woodsman may have dropped a tree
        // across it, or another guild may have built there since.
        if (search.HasKept && (mainland < 0 || BotGates.LandOf(map, search.Kept) == mainland)
            && Sound(map, search.Kept, multi, replacing) && Away(search.Kept, shun, clear)
            && !BotRefused.Refusing(map, search.Kept) && !Foreign(by, map, search.Kept, multi, mainlandOnly)
            && !Round(map, from, search.Kept.X, search.Kept.Y))
        {
            centre = search.Kept;

            return true;
        }

        search.HasKept = false;

        Build();

        for (var spent = 0; spent < Budget; spent++)
        {
            if (search.Cursor >= _ring.Count)
            {
                // Round the whole spiral. Begin again rather than stop: the island is not the same place it
                // was an hour ago — trees fall to woodsmen, a hall goes up and moves its neighbours along —
                // and a search that gives up for ever is a search that answers a stale question.
                search.Cursor = 0;
                Exhausted++;

                break;
            }

            var offset = _ring[search.Cursor++];
            var x = from.X + offset.X;
            var y = from.Y + offset.Y;

            // A body has to be able to get here at all. Asked first because it is the cheapest of the three
            // questions and the only one about walking rather than about building.
            if (!BotStep.Settle(map, x, y, out _))
            {
                Floorless++;

                continue;
            }

            if (BotRefused.Refusing(map, new Point3D(x, y, 0)))
            {
                Unreached++;

                continue;
            }

            if (mainland >= 0 && BotGates.LandOf(map, new Point3D(x, y, 0)) != mainland)
            {
                OffMainland++;

                continue;
            }

            if (Round(map, from, x, y))
            {
                Roundabout++;

                continue;
            }

            // <b>The height is the land average, not the floor a bot would stand on, and the difference was
            // a hard zero.</b> HousePlacement rule 4 sets hasSurface only where <c>landAvgZ == center.Z</c>
            // on every foundation tile, so a centre taken from BotStep.Settle — which answers the standable
            // height, and will happily stand a bot on a static two units up — is refused at every tile it is
            // offered. 113 candidates and 113 refusals, before this line read the number the engine actually
            // compares against.
            map.GetAverageZ(x, y, out _, out var avg, out _);

            var at = new Point3D(x, y, avg);

            if (!Away(at, shun, clear))
            {
                Overshadowed++;

                continue;
            }

            // <b>Not on another guild's ground, under any part of the walls</b> (06.10.2026). Asked of the centre tile alone until
            // now, and The Ash's hall went up at 1470,1412 at 01:16:38 that night with its west wall three tiles deep in The Wolf's
            // square at 1440,1410. See BotBounds.Covers: a guild's building stands wholly in the square of its centre, open or its
            // own, and on squares its guild holds; a bot's house anywhere but on the ground of a guild not its own nor allied.
            if (Foreign(by, map, at, multi, mainlandOnly))
            {
                continue;
            }

            if (Crowded(map, at, replacing))
            {
                Occupied++;

                continue;
            }

            // Not up against a graveyard wall either. See Clearance.
            if (Hallowed(map, at))
            {
                Shunned++;

                continue;
            }

            // Out of town, and this is the engine's rule plus one of ours. The engine refuses housing only
            // inside a NoHousingRegion, which on this island is the graveyard and little else — Britain's
            // own town region would take a house on any square of open ground. A hall in a guarded town is
            // a hall where the watch breaks up anything that happens in it, and what is meant to happen in
            // these is two guildmates knocking each other about for the practice.
            if (Region.Find(at, map)?.IsPartOf<GuardedRegion>() == true)
            {
                InTown++;

                continue;
            }

            // Level under the whole footprint, tested here for a fraction of the price of asking the engine:
            // the full check walks the multi, every static and every item on forty-nine tiles, and the great
            // majority of ground near a town fails on this one rule alone.
            if (!Level(map, at, multi))
            {
                Uneven++;

                continue;
            }

            Tested++;

            var result = HousePlacement.Check(by, multi, at, out var toMove, Direction.South);

            if (result != HousePlacementResult.Valid)
            {
                // One bucket per refusal, because a bucket holding four reasons answers none of them. See
                // a-new-gate-needs-a-new-bucket: this counter was written lumped, reported 113 out of 113
                // refused "for the ground", and said nothing whatever about which rule was doing it.
                switch (result)
                {
                    case HousePlacementResult.BadRegion:
                    case HousePlacementResult.BadRegionHidden:
                    case HousePlacementResult.BadRegionTemp:
                    case HousePlacementResult.BadRegionRaffle:
                        {
                            Forbidden++;

                            break;
                        }
                    case HousePlacementResult.BadLand:
                        {
                            BadLand++;

                            break;
                        }
                    case HousePlacementResult.BadStatic:
                        {
                            BadStatic++;

                            break;
                        }
                    case HousePlacementResult.BadItem:
                        {
                            BadItem++;

                            break;
                        }
                    default:
                        {
                            Surfaceless++;

                            break;
                        }
                }

                continue;
            }

            if (toMove is { Count: > 0 })
            {
                // The engine would shove whatever is there under the house sign. It is entitled to; we are
                // not. Somebody's ore pile is not a building site.
                Occupied++;

                continue;
            }

            Fit++;

            // Trodden ground and clear of the other clans, both, and it is taken at once; anything short of
            // that is the spare, and only the first spare, so a look that finds three near plots and one
            // far one still hands over the far one when it comes.
            if (BotQuad.Trodden(map, at) && Nearest(map, at) >= Shy)
            {
                centre = at;
                search.Kept = at;
                search.HasKept = true;
                Say(at, from, true);

                return true;
            }

            if (!search.HasSpare || Nearest(map, at) > Nearest(map, search.Spare))
            {
                search.Spare = at;
                search.HasSpare = true;
            }
        }

        // The look is over. Whatever it turned up is handed over now rather than kept for a better one.
        if (search.HasSpare)
        {
            if (Nearest(map, search.Spare) < Shy)
            {
                Neighboured++;
            }

            search.HasSpare = false;
            centre = search.Spare;
            search.Kept = centre;
            search.HasKept = true;
            Say(centre, from, false);

            return true;
        }

        return false;
    }

    /// <summary>Whether a spot is far enough from the one place a caller may not build near.</summary>
    private static bool Away(Point3D at, Point3D shun, int clear) =>
        clear <= 0 || shun == Point3D.Zero ||
        Math.Max(Math.Abs(at.X - shun.X), Math.Abs(at.Y - shun.Y)) >= clear;

    /// <summary>
    /// Candidates refused for standing inside the yard of a hall this caller is being moved away from.
    ///
    /// Its own bucket, because it is the only refusal here that is about <em>who is asking</em> rather than
    /// about the ground. A spiral that comes back empty for a loser and full for everybody else is not a
    /// crowded island, and reading it as one would send somebody widening the search.
    /// </summary>
    public static long Overshadowed { get; private set; }

    /// <summary>
    /// Candidates, and remembered ground, passed over because somebody gave up trying to reach that spot lately
    /// (<see cref="BotRefused"/>).
    ///
    /// <para>
    /// <b>The refusal was written and this search did not read it.</b> On 26.09.2026 The Lantern, owing The Blade a move, was
    /// offered 1082,1402 again and again: Dain walked 7.3 minutes, the walk stopped closing 85 tiles short, the square was
    /// filed as refused — and the remembered plot was handed straight back to the next bot (11:32–11:41).
    /// </para>
    /// </summary>
    public static long Unreached { get; private set; }

    /// <summary>Candidates passed over for standing, under any part of their walls, on a square another guild holds. See <see cref="Foreign"/>.</summary>
    public static long Foreigners { get; private set; }

    /// <summary>Candidates for a guild building passed over because their walls would reach out of their own square onto open ground.</summary>
    public static long Straddling { get; private set; }

    /// <summary>Candidates for a bot's own house passed over for lying on the ground of a guild neither its own nor allied.</summary>
    public static long AbodesAbroad { get; private set; }

    /// <summary>Candidates passed over for lying further round by road than <see cref="MostBehind"/>. See <see cref="Round"/>.</summary>
    public static long Roundabout { get; private set; }

    /// <summary>
    /// How much further round by road than the search's origin a plot may lie, in steps. Sixty: the far bank of a river is
    /// hundreds round, a field beside the town a few dozen.
    /// </summary>
    public static int MostBehind { get; set; } = 60;

    /// <summary>
    /// Whether a building of <paramref name="multi"/> on <paramref name="at"/> may not stand there for the ground's holders
    /// (<see cref="BotBounds.Covers(string, Map, Point3D, int, bool, bool, out string)"/>): strict for a guild's building, lenient
    /// with allies welcome for a bot's own house. Counts its own bucket.
    /// </summary>
    private static bool Foreign(Mobile by, Map map, Point3D at, int multi, bool guildBuilding)
    {
        var guild = (by?.Guild as Server.Guilds.Guild)?.Name;

        switch (BotBounds.Covers(guild, map, at, multi, guildBuilding, !guildBuilding, out _))
        {
            case BotBounds.Breach.None:
                {
                    return false;
                }
            case BotBounds.Breach.Straddling:
                {
                    Straddling++;

                    return true;
                }
            default:
                {
                    if (guildBuilding)
                    {
                        Foreigners++;
                    }
                    else
                    {
                        AbodesAbroad++;
                    }

                    return true;
                }
        }
    }

    /// <summary>
    /// Whether the guild's people could not get here: no road from home at all, or a road that goes round further than
    /// <see cref="MostBehind"/> past the search's origin — the far side of a river. Patrick's order of 26.09.2026, "when
    /// moving, check". On 26.09.2026 The Lantern's hall was carried to 1082,1402, 1437 steps from home by road for 358 in
    /// a straight line, while its seat at 1178,1500 was 266 for 262: its company mustered on one bank and its members could
    /// not reach it from the other (12:10–12:45). Asked only where the road map has something to say.
    /// </summary>
    private static bool Round(Map map, Point3D from, int x, int y)
    {
        if (!BotRoads.Covers(map, x, y))
        {
            return false;
        }

        if (BotRoads.FromHome(map, x, y) < 0)
        {
            return true;
        }

        return BotRoads.Behind(map, from, new Point3D(x, y, 0)) > MostBehind;
    }

    /// <summary>
    /// Our own tests of a plot short of the engine's: level under the walls, no other hall, outpost or bot's house too near (not
    /// counting <paramref name="replacing"/>), not against a graveyard, not in a guarded town. For a hall shifted within its own
    /// ground (<c>BotShift</c>, 06.10.2026), whose candidates the spiral never offers: it begins <see cref="Near"/> tiles out.
    /// </summary>
    internal static bool Suits(Map map, Point3D at, int multi, BaseHouse replacing) =>
        Level(map, at, multi) && !Crowded(map, at, replacing) && !Hallowed(map, at)
        && Region.Find(at, map)?.IsPartOf<GuardedRegion>() != true;

    /// <summary>Something has been built on the remembered ground, or it has been proved bad. Forget it, wherever it was found from.</summary>
    public static void Spend()
    {
        foreach (var search in _searches.Values)
        {
            search.HasKept = false;
        }
    }

    /// <summary>
    /// Whether remembered ground is still worth walking to: level, empty, and not next door to a hall that
    /// has gone up since. The engine's full check is left to the moment of building, where it must happen
    /// again anyway.
    /// </summary>
    private static bool Sound(Map map, Point3D at, int multi, BaseHouse replacing) =>
        !Crowded(map, at, replacing) && Level(map, at, multi);

    /// <summary>
    /// Says where the ground is, once per piece of it.
    ///
    /// A hall is raised perhaps four times in the life of an island, so the one line that says where it is
    /// going is worth having in the session log rather than only in a five-minute total.
    /// </summary>
    private static void Say(Point3D at, Point3D from, bool trodden) =>
        logger.Information(
            "Ground a hall could stand on at {X},{Y},{Z}, on {Kind} ground, {Gap} tiles from the seat at {FromX},{FromY}: {Tested} candidates have been put to the engine and {Fit} would take one",
            at.X,
            at.Y,
            at.Z,
            trodden ? "trodden" : "untrodden",
            Math.Max(Math.Abs(at.X - from.X), Math.Abs(at.Y - from.Y)),
            from.X,
            from.Y,
            Tested,
            Fit
        );

    /// <summary>
    /// Whether the land is at one height under the whole footprint.
    ///
    /// The footprint is read off the multi itself rather than assumed to be seven by seven, so dialling
    /// <see cref="MultiID"/> to a bigger house changes what is measured as well as what is built.
    /// </summary>
    private static bool Level(Map map, Point3D at, int multi)
    {
        var mcl = MultiData.GetComponents(multi);

        for (var ix = 0; ix < mcl.Width; ix++)
        {
            for (var iy = 0; iy < mcl.Height; iy++)
            {
                var tiles = mcl.Tiles[ix][iy];
                var foundation = false;

                for (var t = 0; t < tiles.Length && !foundation; t++)
                {
                    // The engine's own definition, word for word: a wall tile at the house's own height is
                    // what has to rest on level land. Written as "every tile in the bounding box" first,
                    // which threw away three candidates in five for the sake of the ground under a floor
                    // nothing stands on.
                    foundation = tiles[t].Z == 0 &&
                                 TileData.ItemTable[tiles[t].ID & TileData.MaxItemValue].Wall;
                }

                if (!foundation)
                {
                    continue;
                }

                map.GetAverageZ(at.X + mcl.Min.X + ix, at.Y + mcl.Min.Y + iy, out _, out var avg, out _);

                if (avg != at.Z)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Whether this spot is inside the belt kept clear round a region the engine forbids housing in.
    ///
    /// <para>
    /// The rectangles are gathered once from the engine's own region list rather than written down here: a
    /// graveyard's bounds are the world's business, and a copy of them in this file would be a second
    /// opinion that goes stale the first time the map is decorated.
    /// </para>
    /// </summary>
    private static bool Hallowed(Map map, Point3D at)
    {
        if (Clearance <= 0)
        {
            return false;
        }

        Hallow(map);

        for (var i = 0; i < _shunned.Count; i++)
        {
            var box = _shunned[i];

            if (at.X >= box.X && at.X <= box.X + box.Width && at.Y >= box.Y && at.Y <= box.Y + box.Height)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Gathers those rectangles, grown by <see cref="Clearance"/>, the first time anybody asks.</summary>
    private static void Hallow(Map map)
    {
        if (_shunned != null && _hallowedBy == Clearance)
        {
            return;
        }

        _shunned = [];
        _hallowedBy = Clearance;

        var regions = Region.Regions;

        for (var i = 0; i < regions.Count; i++)
        {
            var region = regions[i];

            if (region?.Map != map || !region.IsPartOf<NoHousingRegion>() || region.Area == null)
            {
                continue;
            }

            for (var a = 0; a < region.Area.Length; a++)
            {
                var box = region.Area[a];

                _shunned.Add(
                    new Rectangle2D(
                        box.Start.X - Clearance,
                        box.Start.Y - Clearance,
                        box.Width + Clearance * 2,
                        box.Height + Clearance * 2
                    )
                );
            }
        }

        logger.Information(
            "{Count} places on this map will not have a hall within {Clearance} tiles of them",
            _shunned.Count,
            Clearance
        );
    }

    /// <summary>How far the nearest standing hall is from this spot, or a great deal if there is none.</summary>
    private static int Nearest(Map map, Point3D at)
    {
        var nearest = int.MaxValue;

        foreach (var house in BotEstate.Halls)
        {
            if (house is not { Deleted: false } || house.Map != map)
            {
                continue;
            }

            var gap = Math.Max(Math.Abs(house.X - at.X), Math.Abs(house.Y - at.Y));

            if (gap < nearest)
            {
                nearest = gap;
            }
        }

        return nearest;
    }

    /// <summary>Whether a hall already stands near enough that another would be a terrace.</summary>
    private static bool Crowded(Map map, Point3D at, BaseHouse replacing)
    {
        foreach (var house in BotEstate.Halls)
        {
            if (house is { Deleted: false } && house != replacing && house.Map == map && house.Location.GetDistanceToSqrt(at) < Apart)
            {
                return true;
            }
        }

        // And the guilds' outposts, at a hall's distance. See BotOutpost.
        foreach (var house in BotOutpost.Houses)
        {
            if (house is { Deleted: false } && house != replacing && house.Map == map && house.Location.GetDistanceToSqrt(at) < Apart)
            {
                return true;
            }
        }

        // And the houses of the population's own, at their own distance. See BotAbode.
        foreach (var house in BotAbode.Houses)
        {
            if (house is { Deleted: false } && house != replacing && house.Map == map && house.Location.GetDistanceToSqrt(at) < BotAbode.Apart)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The offsets to try, nearest first.
    ///
    /// Built once and walked in order, so successive searches carry on rather than starting again at the
    /// same refused tile — which is how a budgeted search turns into a budgeted loop.
    /// </summary>
    private static void Build()
    {
        // Rebuilt when the bounds move, because they are dials: a ring built once and kept for ever would
        // make "dial BotPlot.Far 300" a line that is accepted, journalled, and does nothing at all.
        if (_ring != null && _builtNear == Near && _builtFar == Far)
        {
            return;
        }

        var ring = new List<Point2D>();

        for (var dx = -Far; dx <= Far; dx += 2)
        {
            for (var dy = -Far; dy <= Far; dy += 2)
            {
                var away = Math.Sqrt(dx * dx + dy * dy);

                if (away >= Near && away <= Far)
                {
                    ring.Add(new Point2D(dx, dy));
                }
            }
        }

        ring.Sort(
            (a, b) => (a.X * a.X + a.Y * a.Y).CompareTo(b.X * b.X + b.Y * b.Y)
        );

        _ring = ring;
        _builtNear = Near;
        _builtFar = Far;

        foreach (var search in _searches.Values)
        {
            search.Cursor = 0;
        }
    }

    /// <summary>Part of the estate's line in the summary.</summary>
    public static string Describe() =>
        $"plots: {Tested} put to the engine and {Fit} would take a hall; {Uneven} passed over as not level, {Floorless} with no floor at all, {Unreached} for ground somebody lately failed to reach, {Foreigners} for standing on another guild's square under any part of their walls, {Straddling} for walls reaching out of their own square onto open ground, {AbodesAbroad} bots' houses for lying on a guild's ground not their own nor allied, {Roundabout} for lying round the far side of something by road, {InTown} for standing in a town, {Shunned} for pressing against a graveyard, {Occupied} with something standing on them, {Neighboured} taken within {Shy} tiles of another clan for want of anything further off; the engine refused {Forbidden} on its own regions, {Surfaceless} for resting on nothing, {BadLand} for the land, {BadStatic} for a static or an unclear yard, {BadItem} for something lying there; the spiral has been walked out {Exhausted} times; guild buildings kept to the mainland: {Ashore} searches begun from an island seat moved to the home, {OffMainland} candidates off it passed over, {GatesUnread} searches refused before the gates were read";

    public static void Forget()
    {
        _ring = null;
        _searches.Clear();
        Tested = 0;
        Fit = 0;
        Floorless = 0;
        Unreached = 0;
        Foreigners = 0;
        Straddling = 0;
        AbodesAbroad = 0;
        Roundabout = 0;
        Occupied = 0;
        InTown = 0;
        Shunned = 0;
        _shunned = null;
        _hallowedBy = -1;
        Forbidden = 0;
        Neighboured = 0;
        Uneven = 0;
        BadLand = 0;
        BadStatic = 0;
        BadItem = 0;
        Surfaceless = 0;
        Exhausted = 0;
        _builtNear = -1;
        _builtFar = -1;
    }
}
