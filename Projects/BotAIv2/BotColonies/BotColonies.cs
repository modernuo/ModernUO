using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Server.Engines.Harvest;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// The guilds' colonies on the islands: who has one and where, the parties at the docks, the store each colony fills, and
/// the line, the page and the door that say so.
///
/// <para>
/// <b>Patrick, 07.10.2026: "Ships stand idle. A guild fits out an expedition: the chest pays for the ship and stores, a party
/// of 5–8 with a scout boards, sails to an island (Nujel'm, Magincia, Moonglow, Ocllo) and raises an outpost-colony: its own
/// mine on untouched veins, a store, a trade route home on the same ship. A colony is guild ground by the claim rules."</b>
/// </para>
///
/// <para>
/// <b>The parts.</b> The expedition (<see cref="BotExpedition"/>) is the captain's: a ship it holds, adopts or buys through the
/// shipwright as any ship is bought (<see cref="BotSea.Owns"/>, money15), the walk to the dock, the muster, the stores paid
/// out of the chest, the voyage with the party aboard (<see cref="BotVoyage"/>'s company), and on the island the store set down
/// and the square claimed by <see cref="BotClaim.Open"/> like any other square. The members sign on
/// (<see cref="BotSignOn"/>), stand by the captain at the berth, ride, and stand on the square while it is claimed. On the
/// island the colonists dig with the miner's own trip (<see cref="BotDig"/>), and the metal goes into the colony's store
/// rather than their banks (<see cref="StoreOf"/>). The run home (<see cref="BotColonyRun"/>) loads the store, takes home the
/// colonists whose shift is over, sells over the mainland's counters and pays the chest, the crew and itself. A relief
/// (<see cref="BotExpedition"/> again, not founding) takes the ship and fresh hands back.
/// </para>
///
/// <para>
/// <b>Money.</b> The stores are paid out of the guild's chest into the city's purse (<see cref="BotCity.Tax"/>), nothing made
/// or lost. A ship is bought only from a shipwright, from the captain's purse and the guild's (<see cref="BotProvision.Fund"/>),
/// and only for an expedition, which pays: a colony's goods are sold. A ship adopted is one of the population's own, left at
/// anchor with no owner; nothing is paid and nothing is made, and the key the harbour cuts for it is counted. The only new
/// coin is what the mainland's shopkeepers pay for the goods.
/// </para>
/// </summary>
public static class BotColonies
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotColonies));

    /// <summary>Whether guilds send expeditions and keep colonies at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The most a party is, the captain counted. Patrick: five to eight.</summary>
    public static int PartySize { get; set; } = 8;

    /// <summary>The fewest an expedition that founds a colony sails with, the captain counted.</summary>
    public static int LeastParty { get; set; } = 5;

    /// <summary>What the stores of an expedition cost the guild's chest, paid into the city's purse when the ship puts to sea.</summary>
    public static int Cost { get; set; } = 1500;

    /// <summary>The stores of a relief. Nought: a relief carries the ship back and whoever musters.</summary>
    public static int RelieveCost { get; set; }

    /// <summary>How long the captain waits at the berth for the party once it is there.</summary>
    public static int MusterMs { get; set; } = 240000;

    /// <summary>How far from the dock a member is still asked to sign on.</summary>
    public static int MusterReach { get; set; } = 400;

    /// <summary>The colonies' clock: the shift, the claim, the loss, the yield.</summary>
    public static int LookMs { get; set; } = 30000;

    /// <summary>How often one guild is weighed for an expedition.</summary>
    public static int ConsiderMs { get; set; } = 600000;

    /// <summary>How long a colonist works the island before the ship may take it home. Two hours.</summary>
    public static int ShiftMs { get; set; } = 7200000;

    /// <summary>The least time between two runs home.</summary>
    public static int RunEveryMs { get; set; } = 2700000;

    /// <summary>Units of goods in the store that make a run home worth sailing, shift or no shift.</summary>
    public static int CargoAt { get; set; } = 60;

    /// <summary>How full the captain loads its pack with the store's goods, of the weight it can carry.</summary>
    public static double CargoLoad { get; set; } = 0.85;

    /// <summary>The captain's share of what the goods fetch on the mainland.</summary>
    public static double CaptainShare { get; set; } = 0.1;

    /// <summary>The share shared among the hands who came home with it, into their banks.</summary>
    public static double CrewShare { get; set; } = 0.3;

    /// <summary>How far round the site the colony's ground runs: its seams are the population's ground (<see cref="Within"/>).</summary>
    public static int Reach { get; set; } = 45;

    /// <summary>How many rings of squares round an island's dock are looked at for a site.</summary>
    public static int SiteRings { get; set; } = 5;

    /// <summary>The fewest workable rock tiles a site's square must show, sampled every <see cref="SiteStride"/> tiles.</summary>
    public static int MinRocks { get; set; } = 4;

    /// <summary>How finely a square is sampled for rock: every fourth tile, sixty-four looks a square.</summary>
    public static int SiteStride { get; set; } = 4;

    /// <summary>The islands, by their docks' names, in the order Patrick named them.</summary>
    public static string Islands { get; set; } = "Nujel'm,Magincia,Moonglow,Ocllo";

    /// <summary>How long a colony may stand with nobody of its guild on its land before it is given up. Three hours.</summary>
    public static int AbandonMs { get; set; } = 10800000;

    /// <summary>Whether an expedition with no ship may buy one at the shipwright (the guild standing what the captain lacks).</summary>
    public static bool BuyShip { get; set; } = true;

    /// <summary>Whether a guild with no ship may adopt one of the population's own lying at anchor with no owner.</summary>
    public static bool Salvage { get; set; } = true;

    /// <summary>Whether the colony's square is priced at the claim's plain price rather than the step past ten squares.</summary>
    public static bool FlatClaim { get; set; } = true;

    /// <summary>How often a colony whose square is not yet claimed tries again.</summary>
    public static int ClaimRetryMs { get; set; } = 300000;

    /// <summary>
    /// The door's verbs, by hand only (Argus's <c>BotHand</c> accepts them beside its own list): <c>expeditions</c> — every colony,
    /// its store, its yield, its runs home and what The Shadow took, and every party at a dock or at sea (<see cref="Tell"/>);
    /// <c>colony &lt;guild&gt;</c> — fit out an expedition for the guild now, its clock ignored and its money not, or a relief when it
    /// has a colony and the ship is on the mainland (<see cref="Launch"/>).
    /// </summary>
    public static readonly string[] Verbs = ["expeditions", "colony"];

    /// <summary>
    /// An order by hand, through the door: <c>dial BotColonies.Order The Hammer</c> (or <c>do colony The Hammer</c>) presses the
    /// expedition on the guild now, its clock ignored, its money not. Read and cleared by the colonies' clock.
    /// </summary>
    public static string Order { get; set; } = "";

    // ---- Counters ------------------------------------------------------------------------------------------------------

    public static long Considered { get; private set; }

    public static long Offered { get; private set; }

    public static long NoShip { get; private set; }

    public static long Poor { get; private set; }

    public static long NoIsland { get; private set; }

    public static long Founded { get; private set; }

    public static long Lost { get; private set; }

    public static long Abandoned { get; private set; }

    public static long Sailed { get; private set; }

    public static long Landed { get; private set; }

    public static long Unmustered { get; private set; }

    public static long VoyageLost { get; private set; }

    public static long Relieved { get; private set; }

    public static long StoresGold { get; private set; }

    public static long ShipsBought { get; private set; }

    public static long ShipsGold { get; private set; }

    public static long Salvaged { get; private set; }

    public static long SalvageRefused { get; private set; }

    public static long ClaimsOpened { get; private set; }

    public static long ClaimsRefused { get; private set; }

    public static long ClaimsWon { get; private set; }

    public static long DepositsOre { get; private set; }

    public static long DepositsBars { get; private set; }

    public static long DepositsWood { get; private set; }

    public static long Deposits { get; private set; }

    public static long Ordered { get; private set; }

    private static readonly List<BotColony> _colonies = [];

    private static readonly List<BotSailing> _passages = [];

    private static readonly Dictionary<string, long> _considered = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, (long Tick, Point3D Site, int Rocks)> _sites = new(StringComparer.OrdinalIgnoreCase);

    private static long _looked;

    private static bool _resolved;

    public static IReadOnlyList<BotColony> All => _colonies;

    public static IReadOnlyList<BotSailing> Passages => _passages;

    /// <summary>The colonies module's part of the sea's start: the proposers offered and the store's colonies resolved.</summary>
    public static void Start()
    {
        BotWill.Offer(new BotExpeditioner());
        BotWill.Offer(new BotSigner());
        BotWill.Offer(new BotColonyRunner());

        _resolved = false;

        logger.Information(
            "Colonies are {State}: a guild whose chest holds {Cost}gp of stores and which holds, adopts{Buy} a small ship sends a party of {Least}–{Most} to {Islands}; a colonist's shift is {Shift} minutes, the ship runs home at {Cargo} units of goods or a shift's end, no oftener than every {Every} minutes, the goods' price {Captain:P0} to the captain, {Crew:P0} to the hands, the rest to the chest; {Read} colonies read back from the save",
            Running ? "on" : "off",
            Cost,
            BuyShip ? " or buys" : "",
            LeastParty,
            PartySize,
            Islands,
            ShiftMs / 60000,
            CargoAt,
            RunEveryMs / 60000,
            CaptainShare,
            CrewShare,
            _colonies.Count
        );
    }

    // ---- Questions the rest of the shard asks --------------------------------------------------------------------------

    /// <summary>This guild's colony, or null.</summary>
    public static BotColony Of(string guild)
    {
        if (guild == null)
        {
            return null;
        }

        for (var i = 0; i < _colonies.Count; i++)
        {
            if (_colonies[i].Guild == guild)
            {
                return _colonies[i];
            }
        }

        return null;
    }

    /// <summary>Whether two places are on the same land: the walkers' graph, one lookup each, cached.</summary>
    public static bool SameLand(Map map, Point3D a, Point3D b)
    {
        if (map == null || map == Map.Internal)
        {
            return false;
        }

        var la = BotGates.LandOf(map, a);

        return la >= 0 && la == BotGates.LandOf(map, b);
    }

    /// <summary>The colony of this bot's guild when the bot stands on its land, or null.</summary>
    public static BotColony OnLand(Mobile body)
    {
        if (!Running || _colonies.Count == 0 || body is not { Deleted: false } || body.Guild is not Guild guild)
        {
            return null;
        }

        var colony = Of(guild.Name);

        return colony != null && colony.Map == body.Map && SameLand(body.Map, body.Location, colony.Site) ? colony : null;
    }

    /// <summary>
    /// Whether a place is colony ground: within <see cref="Reach"/> of a colony's site. Asked by <c>BotPopulation.Within</c>, so
    /// that the seams round a colony are ground a colonist may be sent to though they lie two thousand tiles from home.
    /// </summary>
    public static bool Within(Map map, Point3D where)
    {
        if (!Running || _colonies.Count == 0 || map == null)
        {
            return false;
        }

        for (var i = 0; i < _colonies.Count; i++)
        {
            var colony = _colonies[i];

            if (colony.Map == map && Math.Abs(colony.Site.X - where.X) <= Reach && Math.Abs(colony.Site.Y - where.Y) <= Reach)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The store a colonist puts its metal in instead of its bank: its colony's, when it stands on the colony's land and the
    /// store stands. Asked by <c>BotDig</c>.
    /// </summary>
    public static Container StoreOf(Mobile body)
    {
        var colony = OnLand(body);

        return colony?.Store is { Deleted: false } store ? store : null;
    }

    /// <summary>Whether this bot is a colonist on its colony's land: the way home by sea is not offered to it (<see cref="BotSeafarer"/>).</summary>
    public static bool OnShift(Mobile body) => OnLand(body) != null;

    /// <summary>The colonies' sites as roots of their guilds' ground, for <c>BotClaim.Connect</c>.</summary>
    public static IEnumerable<(string Guild, Map Map, Point3D At)> Roots()
    {
        for (var i = 0; i < _colonies.Count; i++)
        {
            var colony = _colonies[i];

            if (Running && colony.Map != null)
            {
                yield return (colony.Guild, colony.Map, colony.Site);
            }
        }
    }

    /// <summary>Whether this square is a colony's, for the claim's flat price. See <see cref="FlatClaim"/>.</summary>
    public static bool Site(string guild, (int Map, int X, int Y) key)
    {
        if (!Running || !FlatClaim || guild == null)
        {
            return false;
        }

        for (var i = 0; i < _colonies.Count; i++)
        {
            if (_colonies[i].Guild == guild && _colonies[i].Key == key)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The expedition this guild is mustering now, for a scout to join: its dock, its captain, where it is bound. Null when
    /// there is none. Exposed for the officers' scout (officers20): see <see cref="Enlist"/>.
    /// </summary>
    public static BotSailing Expedition(Guild guild)
    {
        for (var i = 0; i < _passages.Count; i++)
        {
            var passage = _passages[i];

            if (passage.Guild == guild && passage.Heading == BotSailing.Way.Out && passage.State == BotSailing.Tide.Mustering)
            {
                return passage;
            }
        }

        return null;
    }

    /// <summary>
    /// Signs a bot onto its guild's mustering expedition by the guild's order — a scout sent with it. True when it was pressed.
    /// The party's ceiling still holds.
    /// </summary>
    public static bool Enlist(BotMobile bot, string why)
    {
        if (bot?.Guild is not Guild guild || Expedition(guild) is not { } passage || passage.Signed(bot)
            || passage.Roster.Count >= Math.Max(1, PartySize - 1))
        {
            return false;
        }

        return BotWill.Press(bot, new BotSignOn(passage, bot), why ?? $"sent with {guild.Name}'s expedition");
    }

    /// <summary>A passage of this guild in this heading that is not over, or null.</summary>
    public static BotSailing Passage(Guild guild, BotSailing.Way heading)
    {
        for (var i = 0; i < _passages.Count; i++)
        {
            if (_passages[i].Guild == guild && _passages[i].Heading == heading && _passages[i].State != BotSailing.Tide.Over)
            {
                return _passages[i];
            }
        }

        return null;
    }

    internal static void Open(BotSailing passage)
    {
        if (passage != null && !_passages.Contains(passage))
        {
            _passages.Add(passage);
        }
    }

    internal static void Close(BotSailing passage, string why)
    {
        if (passage == null)
        {
            return;
        }

        passage.State = BotSailing.Tide.Over;
        passage.Why ??= why;
        _passages.Remove(passage);
    }

    /// <summary>The guild may be weighed for an expedition now: its clock has run. Starts the clock.</summary>
    internal static bool Due(Guild guild)
    {
        var now = Core.TickCount;

        if (_considered.TryGetValue(guild.Name, out var at) && now - at < ConsiderMs)
        {
            return false;
        }

        _considered[guild.Name] = now;
        Considered++;

        return true;
    }

    internal static void Count(string what)
    {
        switch (what)
        {
            case "offered":
                {
                    Offered++;

                    break;
                }
            case "noship":
                {
                    NoShip++;

                    break;
                }
            case "poor":
                {
                    Poor++;

                    break;
                }
            case "noisland":
                {
                    NoIsland++;

                    break;
                }
            case "sailed":
                {
                    Sailed++;

                    break;
                }
            case "landed":
                {
                    Landed++;

                    break;
                }
            case "unmustered":
                {
                    Unmustered++;

                    break;
                }
            case "lostatsea":
                {
                    VoyageLost++;

                    break;
                }
            case "relieved":
                {
                    Relieved++;

                    break;
                }
        }
    }

    internal static void PaidStores(int gold) => StoresGold += Math.Max(0, gold);

    internal static void Bought(int gold)
    {
        ShipsBought++;
        ShipsGold += Math.Max(0, gold);
    }

    // ---- The islands and the sites ---------------------------------------------------------------------------------------

    /// <summary>The islands' docks named in <see cref="Islands"/>, in order.</summary>
    public static List<BotDock> IslandDocks()
    {
        List<BotDock> docks = [];
        var names = (Islands ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (var i = 0; i < names.Length; i++)
        {
            var dock = BotDocks.Find(names[i]);

            if (dock != null && !docks.Contains(dock))
            {
                docks.Add(dock);
            }
        }

        return docks;
    }

    /// <summary>Whether a colony stands on this island already, whoever's.</summary>
    public static bool Taken(BotDock island)
    {
        for (var i = 0; i < _colonies.Count; i++)
        {
            if (string.Equals(_colonies[i].Island, island?.Name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The square to found a colony on, near an island's dock: walked to from the dock's shore, off the town's guarded ground,
    /// held by nobody and claimed by nobody, with standing ground in its middle, and the most workable rock in it — the
    /// island's untouched veins. Sampled every third tile; kept half an hour per island.
    /// </summary>
    public static bool SiteOn(Map map, BotDock island, out Point3D site, out int rocks)
    {
        site = Point3D.Zero;
        rocks = 0;

        if (map == null || island == null)
        {
            return false;
        }

        var now = Core.TickCount;

        if (_sites.TryGetValue(island.Name, out var cached) && now - cached.Tick < 1800000)
        {
            site = cached.Site;
            rocks = cached.Rocks;

            return site != Point3D.Zero && BotClaim.Owner(map, site) == null && BotClaim.On(map, site) == null;
        }

        var system = Mining.System;
        var home = BotQuad.Key(map, island.Shore);
        var bestRocks = 0;
        var best = Point3D.Zero;
        var bestAway = int.MaxValue;

        for (var dx = -SiteRings; dx <= SiteRings; dx++)
        {
            for (var dy = -SiteRings; dy <= SiteRings; dy++)
            {
                var key = (home.Map, home.X + dx, home.Y + dy);
                var x0 = key.Item2 * BotQuad.Side;
                var y0 = key.Item3 * BotQuad.Side;

                if (x0 < 0 || y0 < 0 || x0 >= map.Width || y0 >= map.Height)
                {
                    continue;
                }

                if (BotTownGround.Guarded(map, key, out _, out _))
                {
                    continue;
                }

                var middle = new Point3D(x0 + BotQuad.Side / 2, y0 + BotQuad.Side / 2, 0);

                if (BotClaim.Owner(map, middle) != null || BotClaim.On(map, middle) != null)
                {
                    continue;
                }

                var stand = BotClaim.Middle(map, middle);

                if (stand == Point3D.Zero || !SameLand(map, island.Shore, stand))
                {
                    continue;
                }

                var count = 0;

                var stride = Math.Max(1, SiteStride);

                for (var x = x0; x < x0 + BotQuad.Side && system != null; x += stride)
                {
                    for (var y = y0; y < y0 + BotQuad.Side; y += stride)
                    {
                        if (BotOre.Examine(map, x, y, system) != null)
                        {
                            count++;
                        }
                    }
                }

                var away = Math.Max(Math.Abs(dx), Math.Abs(dy));

                if (count < MinRocks || count < bestRocks || count == bestRocks && away >= bestAway)
                {
                    continue;
                }

                bestRocks = count;
                best = stand;
                bestAway = away;
            }
        }

        _sites[island.Name] = (now, best, bestRocks);
        site = best;
        rocks = bestRocks;

        logger.Information(
            "Colonies: {Island}'s best site is {Site}{Rocks}",
            island.Name,
            best == Point3D.Zero ? "nowhere" : $"({best.X}, {best.Y})",
            best == Point3D.Zero ? $": no square within {SiteRings} of its dock off the town's ground, free, walked to from the shore, with {MinRocks} rock tiles sampled" : $", {bestRocks} rock tiles sampled in its square"
        );

        return best != Point3D.Zero;
    }

    /// <summary>
    /// The island a guild founds on: the first named with no colony, a site, and a lane from a dock its captain can walk to —
    /// one no gate joins to home first (the sea is its only way, so its colonists stay), then the nearest by sea.
    /// </summary>
    public static bool Island(BotMobile captain, out BotDock island, out Point3D site, out BotDock from, out BotSeaLane lane)
    {
        island = null;
        site = Point3D.Zero;
        from = null;
        lane = null;

        var map = captain?.Map;

        if (map == null || !BotSea.Ready)
        {
            return false;
        }

        var docks = IslandDocks();
        var bestCost = int.MaxValue;

        for (var i = 0; i < docks.Count; i++)
        {
            var dock = docks[i];

            if (Taken(dock) || BotGates.Joined(map, captain.Location, dock.Shore) && SameLand(map, captain.Location, dock.Shore))
            {
                continue;
            }

            if (!BotSea.Plan(map, captain.Location, dock, out var start, out var way, out var tiles, true))
            {
                continue;
            }

            if (!SiteOn(map, dock, out var at, out _))
            {
                continue;
            }

            // An island no gate joins to the captain's land first: its colonists cannot walk away from it.
            var cost = tiles + (BotGates.Joined(map, captain.Location, dock.Shore) ? 100000 : 0);

            if (cost < bestCost)
            {
                bestCost = cost;
                island = dock;
                site = at;
                from = start;
                lane = way;
            }
        }

        return island != null;
    }

    /// <summary>
    /// The way home from a colony: of the docks the guild's home is on the land of, the one that makes the lane and the walk
    /// from it shortest, as the seafarer chooses.
    /// </summary>
    public static bool HomeWay(BotMobile body, out BotDock from, out BotDock to, out BotSeaLane lane, out Point3D home)
    {
        from = null;
        to = null;
        lane = null;
        home = body == null ? Point3D.Zero : BotSeat.Home(body);

        var map = body?.Map;

        if (map == null || home == Point3D.Zero)
        {
            return false;
        }

        var best = int.MaxValue;
        var docks = BotDocks.All;

        for (var i = 0; i < docks.Count; i++)
        {
            var dock = docks[i];

            if (!BotGates.Joined(map, dock.Shore, home) || !BotSea.Plan(map, body.Location, dock, out var start, out var way, out var tiles, true))
            {
                continue;
            }

            var cost = tiles + BotHelm.Away(dock.Shore, home);

            if (cost < best)
            {
                best = cost;
                from = start;
                to = dock;
                lane = way;
            }
        }

        return to != null;
    }

    /// <summary>The way out from the mainland to a colony's island, from a dock this bot can walk to.</summary>
    public static bool OutWay(BotMobile body, BotColony colony, out BotDock from, out BotDock to, out BotSeaLane lane)
    {
        from = null;
        lane = null;
        to = colony == null ? null : BotDocks.Find(colony.Island);

        return body?.Map != null && to != null && BotSea.Plan(body.Map, body.Location, to, out from, out lane, out _, true);
    }

    // ---- Ships: held, adopted, bought ------------------------------------------------------------------------------------

    /// <summary>The member of this guild holding a ship, the given one first, or null.</summary>
    public static BotMobile ShipHolder(Guild guild, BotMobile first = null)
    {
        if (first != null && BotHelm.ShipOf(first) != null)
        {
            return first;
        }

        var members = guild?.Members;

        for (var i = 0; members != null && i < members.Count; i++)
        {
            if (members[i] is BotMobile { Deleted: false } member && BotHelm.ShipOf(member) != null)
            {
                return member;
            }
        }

        return null;
    }

    /// <summary>Whether a ship this captain could adopt lies at anchor: asked when an expedition is weighed, nothing moved.</summary>
    public static bool Adoptable(BotMobile captain)
    {
        if (!Salvage || captain?.Map == null)
        {
            return false;
        }

        var boats = BaseBoat.Boats;

        for (var i = boats.Count - 1; i >= 0; i--)
        {
            if (boats[i] is not SmallBoat { Deleted: false } ship || ship.Map != captain.Map)
            {
                continue;
            }

            var owner = ship.Owner;

            if (owner == null || owner.Deleted || owner is BotMobile { Deleted: false } bot && bot.Guild == captain.Guild && BotHelm.Of(bot) == null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A ship of the population's own lying in the water with nobody to own it — its owner gone from the world, or of this
    /// guild and not sailing it — dry-docked into the captain's pack with a key the harbour cuts for it. The thirteen ships
    /// money15 found at anchor on the way to Nujel'm were exactly these: lost when the key went with a corpse. Nothing is paid
    /// and nothing is made; a small ship only, anchored, its deck and hold empty. True with the ship in the pack.
    /// </summary>
    public static bool Adopt(BotMobile captain, out string why)
    {
        why = null;

        if (!Salvage || captain?.Backpack == null || captain.Map == null)
        {
            why = "adoption is off";

            return false;
        }

        var boats = BaseBoat.Boats;

        for (var i = boats.Count - 1; i >= 0; i--)
        {
            var boat = boats[i];

            if (boat is not SmallBoat { Deleted: false } ship || ship.Map != captain.Map)
            {
                continue;
            }

            var owner = ship.Owner;
            var ownerless = owner == null || owner.Deleted;
            var ours = owner is BotMobile { Deleted: false } bot && bot.Guild == captain.Guild && BotHelm.Of(bot) == null;

            if (!ownerless && !ours)
            {
                continue;
            }

            if (owner is BotMobile sailing && BotHelm.Of(sailing) is { } voyage && voyage.Boat == ship)
            {
                continue;
            }

            if (!ship.Anchored)
            {
                ship.LowerAnchor(false);
            }

            var plank = ship.PPlank ?? ship.SPlank;
            var key = plank == null ? null : new Key(KeyType.Iron, plank.KeyValue);

            if (key == null || !captain.Backpack.TryDropItem(captain, key, false))
            {
                key?.Delete();
                SalvageRefused++;
                why = "no room in the pack for the key";

                continue;
            }

            var result = ship.CheckDryDock(captain);

            if (result != BaseBoat.DryDockResult.Valid)
            {
                key.Delete();
                SalvageRefused++;
                why = $"the ship at ({ship.X}, {ship.Y}) could not be docked: {result}";

                continue;
            }

            var at = ship.Location;

            ship.EndDryDock(captain);

            if (!key.Deleted)
            {
                key.Delete();
            }

            var docked = BotHelm.ShipOf(captain);

            if (docked == null)
            {
                SalvageRefused++;
                why = "the docked ship did not reach the pack";

                continue;
            }

            BotHelm.Keep(captain, docked);
            Salvaged++;

            logger.Information(
                "{Name} of {Guild} adopted a ship left at anchor at ({X}, {Y}){Whose}: the harbour cut it a key, and it is dry-docked into its pack",
                captain.Name,
                captain.Guild?.Name,
                at.X,
                at.Y,
                ownerless ? " with no owner" : $" by {owner?.Name} of its own guild"
            );

            return true;
        }

        why ??= "no ship of the population's lies at anchor without an owner";

        return false;
    }

    // ---- Founding, the store, the loss --------------------------------------------------------------------------------------

    /// <summary>The colony set down: its record, and its store on the site. Null when the guild has one already.</summary>
    public static BotColony Found(Guild guild, BotDock island, Map map, Point3D site, string by)
    {
        if (guild == null || island == null || map == null || Of(guild.Name) != null)
        {
            return null;
        }

        var colony = new BotColony
        {
            Guild = guild.Name,
            Island = island.Name,
            Map = map,
            Site = site,
            FoundedAt = DateTime.UtcNow,
            Since = Core.TickCount,
            LastSeen = Core.TickCount,
            LastRun = Core.TickCount,
            Last = $"founded by {by}"
        };

        colony.Store = PlaceStore(colony);
        colony.StoreSerial = colony.Store?.Serial ?? Serial.Zero;

        _colonies.Add(colony);
        Founded++;

        logger.Warning(
            "{Guild} has founded a colony on {Island} at ({X}, {Y}), led ashore by {By}: its store stands on the site and its square is to be claimed",
            guild.Name,
            island.Name,
            site.X,
            site.Y,
            by
        );

        return colony;
    }

    /// <summary>A chest on the site, fixed to the ground, named for the guild: what the colonists dig goes in, what the ship carries comes out.</summary>
    private static Container PlaceStore(BotColony colony)
    {
        var chest = new WoodenChest { Name = $"{colony.Guild} colony store", Movable = false };

        chest.MoveToWorld(colony.Site, colony.Map);

        return chest;
    }

    /// <summary>
    /// The colonist's metal, ore and wood out of its pack into the store, onto the store's own stacks. Says how many units of
    /// each went in. Counted against the colony for its yield.
    /// </summary>
    public static (int Bars, int Ore, int Wood) Deposit(Mobile body, Container store)
    {
        var pack = body?.Backpack;
        var colony = OnLand(body);

        if (pack == null || store is not { Deleted: false })
        {
            return (0, 0, 0);
        }

        var bars = 0;
        var ore = 0;
        var wood = 0;
        List<Item> carried = [.. pack.Items];

        for (var i = 0; i < carried.Count; i++)
        {
            var item = carried[i];

            if (item == null || item.Deleted || !item.Movable || item is not (BaseIngot or BaseOre or Log or Board))
            {
                continue;
            }

            var amount = Math.Max(1, item.Amount);

            switch (item)
            {
                case BaseIngot:
                    {
                        bars += amount;

                        break;
                    }
                case BaseOre:
                    {
                        ore += amount;

                        break;
                    }
                default:
                    {
                        wood += amount;

                        break;
                    }
            }

            BotSurplus.Put(body, store, item);
        }

        if (bars + ore + wood > 0)
        {
            Deposits++;
            DepositsBars += bars;
            DepositsOre += ore;
            DepositsWood += wood;

            if (colony != null)
            {
                colony.IngotsIn += bars;
                colony.OreIn += ore;
                colony.LogsIn += wood;
            }

            logger.Information(
                "{Name} put {Bars} bars, {Ore} ore and {Wood} wood into {Guild}'s colony store on {Island}; it holds {Stock} units",
                body.Name,
                bars,
                ore,
                wood,
                colony?.Guild ?? body.Guild?.Name,
                colony?.Island ?? "the island",
                colony?.Stock() ?? store.TotalItems
            );
        }

        return (bars, ore, wood);
    }

    /// <summary>The colony given up: its record gone, its store taken off the site with whatever was in it, said and counted.</summary>
    private static void End(BotColony colony, string why, bool lost)
    {
        _colonies.Remove(colony);

        var held = colony.Stock();

        if (colony.Store is { Deleted: false } store)
        {
            store.Delete();
        }

        if (lost)
        {
            Lost++;
        }
        else
        {
            Abandoned++;
        }

        logger.Warning(
            "{Guild}'s colony on {Island} at ({X}, {Y}) is {How}: {Why}; {Held} units of goods went with its store, after {Runs} runs home that fetched {Gold}gp",
            colony.Guild,
            colony.Island,
            colony.Site.X,
            colony.Site.Y,
            lost ? "lost" : "given up",
            why,
            held,
            colony.Runs,
            colony.CargoGold
        );

        BotVoice.Say(BotGuilds.Head(BotGuilds.Named(colony.Guild)), "guild", $"We have lost our colony on {colony.Island}: {why}.", true);
    }

    // ---- The clock -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The colonies' look, every <see cref="LookMs"/> from the sea's survey: the store read back after a restart, who is on
    /// each colony's land and for how long, the claim made or retried, a colony lost or given up, and the order by hand.
    /// </summary>
    public static void Look()
    {
        if (!Running || !BotSea.Ready)
        {
            return;
        }

        var now = Core.TickCount;

        if (now - _looked < LookMs)
        {
            return;
        }

        var elapsed = _looked == 0 ? 0 : Math.Min(600000, now - _looked);

        _looked = now;

        if (!_resolved)
        {
            _resolved = true;
            Resolve();
        }

        Obey();
        BotPiracy.Look();

        for (var i = _colonies.Count - 1; i >= 0; i--)
        {
            var colony = _colonies[i];

            try
            {
                Watch(colony, now, elapsed);
            }
            catch (Exception e)
            {
                logger.Error(e, "Colonies: looking at {Colony} threw", colony);
            }
        }

        // A passage whose captain is gone or whose clock has long run out is closed, so its members are let go.
        for (var i = _passages.Count - 1; i >= 0; i--)
        {
            var passage = _passages[i];

            if (passage.Captain is not { Deleted: false } || now - passage.Began > 3600000)
            {
                Close(passage, "its captain is gone or it ran an hour");
            }
        }
    }

    private static readonly List<Serial> _gone = [];

    private static void Watch(BotColony colony, long now, long elapsed)
    {
        var guild = BotGuilds.Named(colony.Guild);

        if (guild == null || guild.Disbanded)
        {
            End(colony, "its guild is gone", false);

            return;
        }

        if (colony.Store is not { Deleted: false })
        {
            End(colony, "its store is gone", true);

            return;
        }

        // Who is on the land: the shift's clock starts when a member is first seen there.
        var here = 0;
        _gone.Clear();

        foreach (var serial in colony.Shift.Keys)
        {
            _gone.Add(serial);
        }

        var members = guild.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i] is not BotMobile { Deleted: false, Alive: true } member || member.Map != colony.Map
                || !SameLand(colony.Map, member.Location, colony.Site))
            {
                continue;
            }

            here++;
            _gone.Remove(member.Serial);
            colony.Shift.TryAdd(member.Serial, now);
        }

        for (var i = 0; i < _gone.Count; i++)
        {
            colony.Shift.Remove(_gone[i]);
        }

        colony.Colonists = here;
        colony.ColonistMs += here * elapsed;

        if (here > 0)
        {
            colony.LastSeen = now;
        }
        else if (now - colony.LastSeen > AbandonMs && Passage(guild, BotSailing.Way.Out) == null)
        {
            End(colony, $"nobody of {colony.Guild} has been on its land for {AbandonMs / 60000} minutes", false);

            return;
        }

        // The ground: a square once the guild's and the guild's no more is a colony lost by the claim rules — taken, struck
        // off, cut off, or swept. A square not yet claimed is claimed, again every ClaimRetryMs.
        var owner = BotClaim.Owner(colony.Map, colony.Site);

        if (owner == colony.Guild)
        {
            if (!colony.Claimed)
            {
                colony.Claimed = true;
                ClaimsWon++;
                colony.Last = "its square is the guild's";

                logger.Information("{Guild}'s colony on {Island}: its square at ({X}, {Y}) is the guild's ground", colony.Guild, colony.Island, colony.Site.X, colony.Site.Y);
            }

            return;
        }

        if (colony.Claimed)
        {
            End(colony, owner == null ? "its square was struck off and belongs to nobody" : $"its square was taken by {owner}", true);

            return;
        }

        if (BotClaim.On(colony.Map, colony.Site) != null || BotClaim.Making(guild) != null || now - colony.ClaimTried < ClaimRetryMs)
        {
            return;
        }

        Claim(colony, guild);
    }

    /// <summary>Lays the guild's claim to the colony's square, by the claim's own rules: free while the guild has free squares, then the price.</summary>
    internal static bool Claim(BotColony colony, Guild guild)
    {
        colony.ClaimTried = Core.TickCount;
        colony.ClaimTries++;

        // Another guild's square is not claimed from here: a colony settles open ground. Taking held ground is a war's business.
        if (BotClaim.Owner(colony.Map, colony.Site) is { } other && other != colony.Guild)
        {
            ClaimsRefused++;
            colony.Last = $"its square is {other}'s";

            return false;
        }

        var bid = BotClaim.Open(guild, colony.Map, colony.Site, BotClaim.Kind.Settle);

        if (bid == null)
        {
            ClaimsRefused++;
            colony.Last = "its claim could not be laid (the guild could not pay, or claims elsewhere)";

            return false;
        }

        ClaimsOpened++;
        colony.Last = "its square is being claimed";

        return true;
    }

    /// <summary>After a restart: each colony's store found again by its serial, or the colony given up.</summary>
    private static void Resolve()
    {
        for (var i = _colonies.Count - 1; i >= 0; i--)
        {
            var colony = _colonies[i];

            colony.Map ??= BotPopulation.Home;
            colony.Since = Core.TickCount;
            colony.LastSeen = Core.TickCount;
            colony.LastRun = Core.TickCount;

            if (colony.Store is { Deleted: false })
            {
                continue;
            }

            colony.Store = World.FindItem(colony.StoreSerial) as Container;

            if (colony.Store is not { Deleted: false })
            {
                // The site is the guild's still if the claim stands: a new store is set down rather than the colony lost.
                colony.Store = PlaceStore(colony);
                colony.StoreSerial = colony.Store.Serial;

                logger.Information("Colonies: {Colony}'s store was not in the world and is set down again, empty", colony);
            }
        }
    }

    /// <summary>The order by hand, if one was written. See <see cref="Order"/>.</summary>
    private static void Obey()
    {
        var order = Order;

        if (string.IsNullOrWhiteSpace(order))
        {
            return;
        }

        Order = "";
        logger.Information("Colonies: the order \"{Order}\": {Answer}", order, Launch(order));
    }

    /// <summary>
    /// Presses an expedition on a guild now, the clock ignored and the money not: its ship holder, or its head when the guild
    /// can buy or adopt one. Says what came of it. <c>do colony &lt;guild&gt;</c>.
    /// </summary>
    public static string Launch(string name)
    {
        if (!Running || !BotSea.Ready)
        {
            return "the colonies are off, or the sea is not charted yet";
        }

        var guild = GuildNamed(name?.Trim());

        if (guild == null)
        {
            return $"there is no guild called \"{name}\"";
        }

        if (Passage(guild, BotSailing.Way.Out) is { } going)
        {
            return $"{guild.Name} is already at it: {going}";
        }

        var captain = ShipHolder(guild) ?? BotGuilds.Head(guild);

        if (captain == null || !captain.Alive)
        {
            return $"{guild.Name} has nobody alive to captain a ship";
        }

        // The ship at the colony: the run home is what can be sailed now, its own clocks ignored.
        if (OnLand(captain) is { } colony && BotHelm.ShipOf(captain) != null)
        {
            if (Passage(guild, BotSailing.Way.Home) != null)
            {
                return $"{guild.Name}'s ship is already on its way home";
            }

            if (!HomeWay(captain, out var from, out var to, out var lane, out var home))
            {
                return $"{captain.Name} has no lane home from {colony.Island}";
            }

            if (!BotWill.Press(captain, new BotColonyRun(colony, guild, captain, from, to, lane, home), "ordered home with the colony's goods by hand"))
            {
                return $"{captain.Name} could not be pressed";
            }

            Ordered++;

            return $"{captain.Name} of {guild.Name} is ordered to run the colony's goods home from {colony.Island} to {to.Name} ({colony.Stock()} units in the store)";
        }

        var deed = BotExpeditioner.Fit(captain, guild, true, out var why);

        if (deed == null)
        {
            return $"{guild.Name} cannot send one now: {why}";
        }

        if (!BotWill.Press(captain, deed, "ordered to sea with an expedition by hand"))
        {
            return $"{captain.Name} could not be pressed";
        }

        Ordered++;
        Offered++;

        return $"{captain.Name} of {guild.Name} is ordered to fit out {deed.Describe()}";
    }

    /// <summary>A guild by its name as typed at the door, case aside: the guilds the population belongs to.</summary>
    private static Guild GuildNamed(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (BotGuilds.Find(name) is { } exact)
        {
            return exact;
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i]?.Guild is Guild { Disbanded: false } guild && string.Equals(guild.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return guild;
            }
        }

        return null;
    }

    /// <summary>Whether this guild's clock for an expedition has run, without starting it. See <see cref="Due"/>.</summary>
    internal static bool Ripe(Guild guild) =>
        !_considered.TryGetValue(guild.Name, out var at) || Core.TickCount - at >= ConsiderMs;

    // ---- Lines, the page, the door ------------------------------------------------------------------------------------------

    /// <summary>The colonies' clause of the <c>Sea:</c> line.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "colonies are off";
        }

        var sb = new StringBuilder(512);

        sb.Append($"colonies: {_colonies.Count} standing");

        for (var i = 0; i < _colonies.Count; i++)
        {
            var c = _colonies[i];

            sb.Append(
                $" ({c.Guild} on {c.Island} at {c.Site.X},{c.Site.Y}{(c.Claimed ? "" : ", unclaimed")}: {c.Colonists} on the land, {c.Stock()} in store, {c.OreIn} ore {c.IngotsIn} bars {c.LogsIn} wood in, {c.YieldPerHour:F1} a colonist-hour, {c.Runs} runs {c.CargoGold}gp)"
            );
        }

        sb.Append(
            $"; expeditions: {Considered} guilds weighed, {Offered} offered ({Ordered} by hand), {NoShip} with no ship to be had, {Poor} too poor, {NoIsland} with no island free; "
            + $"{Sailed} parties sailed, {Landed} landed, {Unmustered} never mustered, {VoyageLost} lost at sea, {Relieved} reliefs; {Founded} colonies founded, {Lost} lost, {Abandoned} given up; "
            + $"{StoresGold}gp of stores to the city, {ShipsBought} ships bought for {ShipsGold}gp, {Salvaged} adopted from anchor ({SalvageRefused} refused); "
            + $"claims {ClaimsOpened} laid, {ClaimsRefused} refused, {ClaimsWon} won; store: {Deposits} deposits, {DepositsOre} ore, {DepositsBars} bars, {DepositsWood} wood; "
            + $"{BotSignOn.Describe()}; {BotColonyRun.Describe()}; {BotPiracy.Describe()}"
        );

        return sb.ToString();
    }

    /// <summary>The colonies block of <c>/api/state</c>.</summary>
    public static void Web(Utf8JsonWriter w)
    {
        w.WritePropertyName("colonies");
        w.WriteStartObject();
        w.WriteBoolean("running", Running);
        w.WriteBoolean("piracy", BotPiracy.Running);

        w.WritePropertyName("list");
        w.WriteStartArray();

        for (var i = 0; i < _colonies.Count; i++)
        {
            var c = _colonies[i];

            w.WriteStartObject();
            w.WriteString("guild", c.Guild);
            w.WriteString("island", c.Island);
            w.WriteNumber("x", c.Site.X);
            w.WriteNumber("y", c.Site.Y);
            w.WriteBoolean("claimed", c.Claimed);
            w.WriteNumber("colonists", c.Colonists);
            w.WriteNumber("store", c.Stock());
            w.WriteNumber("oreIn", c.OreIn);
            w.WriteNumber("barsIn", c.IngotsIn);
            w.WriteNumber("woodIn", c.LogsIn);
            w.WriteNumber("perColonistHour", Math.Round(c.YieldPerHour, 2));
            w.WriteNumber("runs", c.Runs);
            w.WriteNumber("cargoUnits", c.CargoUnits);
            w.WriteNumber("cargoGold", c.CargoGold);
            w.WriteNumber("chestGold", c.ChestGold);
            w.WriteNumber("plundered", c.Plundered);
            w.WriteString("founded", c.FoundedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            w.WriteString("last", c.Last);
            w.WriteEndObject();
        }

        w.WriteEndArray();

        w.WritePropertyName("passages");
        w.WriteStartArray();

        for (var i = 0; i < _passages.Count; i++)
        {
            var p = _passages[i];

            w.WriteStartObject();
            w.WriteString("guild", p.Guild?.Name);
            w.WriteString("kind", p.Heading == BotSailing.Way.Home ? "run home" : p.Founding ? "expedition" : "relief");
            w.WriteString("captain", p.Captain?.Name);
            w.WriteString("from", p.From?.Name);
            w.WriteString("to", p.To?.Name);
            w.WriteString("state", p.State.ToString());
            w.WriteNumber("signed", p.Roster.Count);
            w.WriteNumber("aboard", p.Aboard.Count);
            w.WriteEndObject();
        }

        w.WriteEndArray();

        w.WriteNumber("founded", Founded);
        w.WriteNumber("lost", Lost);
        w.WriteNumber("abandoned", Abandoned);
        w.WriteNumber("sailed", Sailed);
        w.WriteNumber("landed", Landed);
        w.WriteNumber("runs", BotColonyRun.Runs);
        w.WriteNumber("cargoGold", BotColonyRun.Gold);
        w.WriteNumber("shipsBought", ShipsBought);
        w.WriteNumber("shipsAdopted", Salvaged);
        w.WriteNumber("ambushes", BotPiracy.Struck);
        w.WriteNumber("plunderedUnits", BotPiracy.TakenUnits);
        w.WriteEndObject();
    }

    /// <summary><c>do expeditions</c>: every colony and every party at a dock or at sea, with its numbers.</summary>
    public static string Tell()
    {
        var sb = new StringBuilder(1024);

        sb.Append(Running ? "Colonies are on. " : "Colonies are OFF. ");

        if (_colonies.Count == 0)
        {
            sb.Append("No guild has a colony. ");
        }

        for (var i = 0; i < _colonies.Count; i++)
        {
            var c = _colonies[i];
            var hours = (DateTime.UtcNow - c.FoundedAt).TotalHours;

            sb.Append(
                $"{c.Guild} on {c.Island} at ({c.Site.X}, {c.Site.Y}), founded {hours:F1}h ago, {(c.Claimed ? "its square the guild's" : "its square not yet claimed")}: "
                + $"{c.Colonists} on the land; store {c.Stock()} units; in {c.OreIn} ore, {c.IngotsIn} bars, {c.LogsIn} wood over {c.ColonistMinutes} colonist-minutes ({c.YieldPerHour:F1} an hour each); "
                + $"{c.Runs} runs home carried {c.CargoUnits} units for {c.CargoGold}gp ({c.ChestGold} to the chest, {c.CrewGold} to hands, {c.CaptainGold} to captains); plundered {c.Plundered}; last: {c.Last}. "
            );
        }

        if (_passages.Count == 0)
        {
            sb.Append("No party at a dock or at sea. ");
        }

        for (var i = 0; i < _passages.Count; i++)
        {
            var p = _passages[i];

            sb.Append($"{p}: {p.Roster.Count} signed, {p.Present()} at the captain's side, {p.Aboard.Count} aboard. ");
        }

        sb.Append(Describe());

        return sb.ToString();
    }

    // ---- The store's file ----------------------------------------------------------------------------------------------------

    internal static IReadOnlyList<BotColony> ForSave() => _colonies;

    internal static void Restore(BotColony colony)
    {
        if (colony?.Guild != null && Of(colony.Guild) == null)
        {
            _colonies.Add(colony);
        }
    }

    /// <summary>
    /// <b>Patrick's reset (07.10.2026, reset26): every colony given up, its store taken off the site, so the save writes the colonies
    /// empty.</b> <see cref="Forget"/> keeps <see cref="_colonies"/> for a reload, and the reset of 20:56 that night left
    /// <c>Saves/BotColonies</c> whole: the next boot would have found every old guild's colony again by its store's serial and set an
    /// empty store down where it was missing — a colony of a guild that no longer exists, on ground nobody claims. This empties
    /// exactly what <see cref="BotColonyStore"/> writes and deletes each store chest from the world, with the ore, bars and logs in
    /// it; the passages, the sign-ons, the runs and the piracy go with <see cref="Forget"/>.
    ///
    /// <para>
    /// <b>No coin is held here.</b> An expedition's stores were paid out of the guild's chest into the city's purse when it set
    /// out (<c>BotCity.Tax</c>); a run's cargo was sold for coin paid at once to the chest and the hands. What a store chest holds
    /// is ore, bars and logs, so taking it away moves no gold and leaves nothing counted anywhere. Returns the colonies given up.
    /// </para>
    /// </summary>
    public static int Wipe()
    {
        var gone = _colonies.Count;

        for (var i = 0; i < _colonies.Count; i++)
        {
            var store = _colonies[i].Store ?? World.FindItem(_colonies[i].StoreSerial) as Container;

            if (store is { Deleted: false })
            {
                store.Delete();
            }
        }

        _colonies.Clear();
        Forget();

        return gone;
    }

    public static void Forget()
    {
        _passages.Clear();
        _considered.Clear();
        _sites.Clear();
        _looked = 0;
        _resolved = false;
        Considered = 0;
        Offered = 0;
        NoShip = 0;
        Poor = 0;
        NoIsland = 0;
        Founded = 0;
        Lost = 0;
        Abandoned = 0;
        Sailed = 0;
        Landed = 0;
        Unmustered = 0;
        VoyageLost = 0;
        Relieved = 0;
        StoresGold = 0;
        ShipsBought = 0;
        ShipsGold = 0;
        Salvaged = 0;
        SalvageRefused = 0;
        ClaimsOpened = 0;
        ClaimsRefused = 0;
        ClaimsWon = 0;
        Deposits = 0;
        DepositsOre = 0;
        DepositsBars = 0;
        DepositsWood = 0;
        Ordered = 0;
        Order = "";
        BotSignOn.Forget();
        BotColonyRun.Forget();
        BotPiracy.Forget();
    }
}
