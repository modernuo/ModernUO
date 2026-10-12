using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// The guilds' houses in the towns: which guild lives in which empty building, the door it put into the doorway and the sign
/// it hung beside it, and the one price — Britain's.
///
/// <para>
/// <b>Patrick's idea of 29.09.2026, evening: "one or two guilds may settle in each of the other towns, but only in an empty
/// and suitable building; when a guild settles there, a door is put in and a sign is hung on the wall. There may be guilds in
/// Britain, but settling there costs 10,000 gold; that counts as the guild's home. They still have to raise a hall and take
/// ground."</b> Until now a guild's seat was a bare point: set by hand, in the file, where its hall stood, or — for a guild
/// founded after the morning of 29.09 — a random spot within forty tiles of another town's middle (<c>BotSeat.Choose</c>).
/// Now it is a building the survey found empty (<see cref="BotHouseSurvey"/>): the guild's seat is the building's heart,
/// its members are born and rise there and live in its town, and the hall and the claims still have to be earned the old way.
/// </para>
///
/// <para>
/// <b>One notion of home, not two.</b> Nothing here answers "where does this bot live"; it answers "where is this guild
/// seated", and three existing questions ask it: <c>BotSeat.Of</c> (the seat the hall is looked for from and carried to),
/// <c>BotSeat.Home</c> (where a member is born, rises and walks home to — the house before the hall) and
/// <c>BotRelocate.Forced</c> (the town a member lives in — Britain too, when the house is there).
/// </para>
///
/// <para>
/// <b>Who goes where.</b> Each guild wishes once, kept across restarts: abroad (by <c>BotSeat.AbroadShare</c>, the order of
/// 29.09.2026 that some guilds settle by other towns) or at home in Britain. A guild already seated at a bare point by
/// another town wishes abroad. Abroad, a guild takes the free building nearest the town's bank in the town with the fewest
/// guild houses, among the towns the gates join to home that are fit to live in (<c>BotSettle</c>: a road, a bank, shops),
/// at most <see cref="MostPerTown"/> a town, for <see cref="TownPrice"/> (nothing, by default: the building stood empty). In
/// Britain a guild waits for its hall (<see cref="BritainAfterHall"/>), then saves <see cref="BritainPrice"/>, which is levied
/// off its chest and members like a hall (<c>BotEstate.Levy</c>) and paid to the city's treasury (<c>BotCity.Tax</c>), so
/// the coin goes round rather than out of the world. A guild with no house and no seat lives as it did: at the population's
/// home, its hall looked for from there.
/// </para>
///
/// <para>
/// <b>The world is the record of what stands; the store is the record of whose.</b> The doors and signs are items and the
/// world save keeps them; <see cref="BotGuildHouseStore"/> keeps which guild holds which building (by the tile of its first
/// door) and each guild's wish. At every boot the survey is read, every house is found again and its door and sign put back
/// if the world lost them, and a door or sign of ours standing in a doorway nobody holds is taken down. A guild gone from
/// the roll for <see cref="GoneLooks"/> looks in a row leaves its house: the door and sign come down and the building is
/// empty again.
/// </para>
/// </summary>
public static class BotGuildHouses
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGuildHouses));

    /// <summary>Whether guilds settle in town buildings at all. Off, and seats are what they were before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The most guild houses in one town other than Britain. Patrick's words: "one or two guilds".</summary>
    public static int MostPerTown { get; set; } = 2;

    /// <summary>The most guild houses in Britain. Three: the capital has the most empty buildings of any town (24 at the offline count).</summary>
    public static int MostInBritain { get; set; } = 3;

    /// <summary>What settling in an empty building of another town costs the guild. Nothing: the order names Britain's price only.</summary>
    public static int TownPrice { get; set; } = 0;

    /// <summary>What settling in Britain costs the guild, paid to the city. Patrick's number.</summary>
    public static int BritainPrice { get; set; } = 10000;

    /// <summary>
    /// Whether a guild that wishes for Britain raises its hall first. On: the hall is what the ground and the claims hang
    /// on (<c>BotHolder</c> offers no square to a guild without one), and the day's money says a guild saving for both at once
    /// would have neither for a day. Off, and Britain is saved for first.
    /// </summary>
    public static bool BritainAfterHall { get; set; } = true;

    /// <summary>Whether a town must be fit to live in (<c>BotSettle</c>: a road in the book, a bank, shops) to be settled in.</summary>
    public static bool FitTownsOnly { get; set; } = true;

    /// <summary>
    /// Whether a guild seated at a bare point by another town moves into a building of that town when one is free there (only
    /// there since build 329 — see the note in the choice). Off, and bare seats are left as they are.
    /// </summary>
    public static bool Rehouse { get; set; } = true;

    /// <summary>How often the guilds are looked over.</summary>
    public static int LookMs { get; set; } = 60000;

    /// <summary>How many looks in a row a guild must be gone from the roll before its house is emptied.</summary>
    public static int GoneLooks { get; set; } = 2;

    /// <summary>How often the free buildings are counted again for the summary and the page.</summary>
    public static int RecountMs { get; set; } = 600000;

    /// <summary>
    /// How far round a house's heart a member is born or rises. Three: the kept buildings are eight to twenty tiles across, and
    /// the population's own spread (ten) would put half the births in the next street. See <c>BotResidence.Span</c>.
    /// </summary>
    public static int Spread { get; set; } = 3;

    /// <summary>What one guild holds: the building, where its seat is, and what it paid.</summary>
    public sealed class Holding
    {
        public string Guild;

        public string Town;

        /// <summary>The first door tile of the building's first way in: the building's name across restarts.</summary>
        public Point3D Key;

        /// <summary>The seat: the building's heart.</summary>
        public Point3D Heart;

        public bool Britain;

        public int Paid;

        public DateTime Since;

        internal BotHouseSurvey.Building Building;

        internal int Gone;
    }

    private static readonly Dictionary<string, Holding> _held = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Each guild's wish, by name: true for Britain, false for another town. Absent until drawn.</summary>
    private static readonly Dictionary<string, bool> _britain = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Guilds whose seat was set by hand (<c>do seat</c> at Argus, <c>BotSeat.Set</c>): the hand's word outranks a house, so
    /// such a guild leaves its house and is not settled again until the world is reset or the name is founded anew.
    /// </summary>
    private static readonly HashSet<string> _pinned = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>When a guild was last said to have nowhere to go, so the log says it once an hour and not once a minute.</summary>
    private static readonly Dictionary<string, long> _said = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Free buildings by town at the last count. See <see cref="RecountMs"/>.</summary>
    private static readonly Dictionary<string, int> _free = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Why buildings were not free at the last count, by reason.</summary>
    private static readonly Dictionary<string, int> _why = new(StringComparer.Ordinal);

    private static bool _reconciled;

    private static long _nextLook;

    private static long _nextCount;

    public static long Settled { get; private set; }

    public static long SettledBritain { get; private set; }

    public static long PaidGold { get; private set; }

    public static long Vacated { get; private set; }

    public static long DoorsPut { get; private set; }

    public static long SignsHung { get; private set; }

    /// <summary>Strangers turned away at a guild's door. See <see cref="BotGuildDoor.Use"/>.</summary>
    public static long Turned { get; private set; }

    /// <summary>Looks at a guild wishing abroad that found no free building in any town with room.</summary>
    public static long NoRoom { get; private set; }

    /// <summary>Looks at a guild wishing for Britain that is waiting for its hall, saving, or finding Britain full or no building free.</summary>
    public static long AwaitingHall { get; private set; }

    public static long SavingBritain { get; private set; }

    public static long BritainFull { get; private set; }

    public static long NoBuilding { get; private set; }

    /// <summary>Settlings that fell over at the levy, or at the doorway.</summary>
    public static long Short { get; private set; }

    public static long Blocked { get; private set; }

    /// <summary>Houses read back at the boot, doors and signs put back, and doors and signs of nobody's taken down.</summary>
    public static int ReadBack { get; private set; }

    public static int PutBack { get; private set; }

    public static int Orphans { get; private set; }

    /// <summary>
    /// Whether the module has started. Until it has — and for ever when it is switched off (<c>bots.guildhouse.enabled</c>) —
    /// no guild has a house as far as any other question is concerned, although the store read the houses back: the doors
    /// and signs stand, and nothing is seated in them.
    /// </summary>
    public static bool Started { get; internal set; }

    /// <summary>Whether houses are being answered and settled: the module runs and the survey is ready.</summary>
    public static bool Active => Running && Started && BotHouseSurvey.Ready;

    /// <summary>What this guild holds, or null.</summary>
    public static Holding Of(string guild) => Running && Started && guild != null ? _held.GetValueOrDefault(guild) : null;

    public static IEnumerable<Holding> All => _held.Values;

    /// <summary>
    /// The guild's seat when it has a house: the building's heart. Asked by <c>BotSeat.Of</c> before anything else it knows.
    /// Answered from the store from the first second of a boot, before the survey is ready: the heart is kept, so the seat
    /// does not flicker to the population's home while the towns are being read.
    /// </summary>
    public static Point3D Seat(string guild) => Of(guild)?.Heart ?? Point3D.Zero;

    public static bool Held(Guild guild) => Of(guild?.Name) != null;

    /// <summary>Whether this point is the heart of a guild's house: where its members are put down, within <see cref="Spread"/>.</summary>
    public static bool IsHeart(Point3D at)
    {
        if (!Running || !Started || _held.Count == 0)
        {
            return false;
        }

        foreach (var held in _held.Values)
        {
            if (held.Heart == at)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The town the guild's house stands in, Britain included; null without one. Asked by <c>BotRelocate.Forced</c>.</summary>
    public static BotTowns.Town TownOf(Guild guild) => Of(guild?.Name) is { } held && BotTowns.Surveyed ? BotTowns.Find(held.Town) : null;

    /// <summary>
    /// What the guild's chest is keeping back for a house in Britain now: the price, while the guild wishes for Britain, has
    /// none, and is past its hall (or does not wait for one). Asked by <c>BotDues.Goal</c>, after the hall's own goal.
    /// </summary>
    public static int Saving(Guild guild)
    {
        if (!Running || !Started || guild == null || _held.ContainsKey(guild.Name) || !_britain.TryGetValue(guild.Name, out var britain) || !britain)
        {
            return 0;
        }

        return BritainAfterHall && BotEstate.Running && BotEstate.Hall(guild) == null ? 0 : BritainPrice;
    }

    /// <summary>A guild has just been founded under this name: whatever an older guild of the name wished is forgotten. From <c>BotSeat.Choose</c>.</summary>
    public static void Founded(string guild)
    {
        if (guild != null)
        {
            _britain.Remove(guild);
            _pinned.Remove(guild);
        }
    }

    /// <summary>
    /// A seat was set for this guild by hand. Patrick's word is the seat: the guild leaves its house, if it has one, and the
    /// settler leaves it alone from now on. From <c>BotSeat.Set</c>.
    /// </summary>
    public static void Pin(string guild)
    {
        if (!Running || string.IsNullOrEmpty(guild) || !_pinned.Add(guild))
        {
            return;
        }

        if (_held.TryGetValue(guild, out var held))
        {
            held.Building ??= BotHouseSurvey.ByDoor(held.Key);
            Vacate(held, BotPopulation.Home, "its seat was set by hand", true);
        }
    }

    /// <summary>
    /// The guild leaves its house because the ground it stands on is now another guild's (06.10.2026, <c>BotBounds</c>): the
    /// path a seat set at the door takes (<see cref="Pin"/>), without the pin, so the settler finds it a building that is not on
    /// anybody's ground. True when it held one.
    /// </summary>
    public static bool Leave(string guild, string why)
    {
        if (!Running || string.IsNullOrEmpty(guild) || !_held.TryGetValue(guild, out var held))
        {
            return false;
        }

        held.Building ??= BotHouseSurvey.ByDoor(held.Key);
        Vacate(held, BotPopulation.Home, why, true);

        return true;
    }

    /// <summary>
    /// Whether this door opens for this mobile: staff, a member of the door's guild, anybody standing inside (nobody is shut
    /// in), and anybody at all once the guild holds no house there — a door of nobody's house bars nobody.
    /// </summary>
    public static bool MayOpen(BotGuildDoor door, Mobile from)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        if (string.Equals((from.Guild as Guild)?.Name, door.Guild, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var held = Of(door.Guild);

        return held == null || held.Building != null && held.Building.Holds(from.X, from.Y);
    }

    /// <summary>A stranger was turned away at a guild's door.</summary>
    public static void Refused(Mobile from) => Turned++;

    /// <summary>The module's clock: the survey a slice at a time, the boot's reconciliation once, then a look every <see cref="LookMs"/>.</summary>
    internal static void Tick(Map map)
    {
        if (!Running || !Started || map == null || map == Map.Internal)
        {
            return;
        }

        if (!BotHouseSurvey.Ready)
        {
            BotHouseSurvey.Step(map);

            return;
        }

        var now = Core.TickCount;

        if (!_reconciled)
        {
            _reconciled = true;
            Reconcile(map);
            Count(map);
            _nextCount = now + RecountMs;
            _nextLook = now;

            return;
        }

        if (now - _nextCount >= 0)
        {
            _nextCount = now + RecountMs;
            Count(map);
        }

        if (now - _nextLook < 0)
        {
            return;
        }

        _nextLook = now + Math.Max(5000, LookMs);
        Look(map);
    }

    /// <summary>
    /// The boot: every house the store names is found in the survey again, and its doors and sign put back if the world has
    /// lost them; a door or sign of ours standing in any surveyed doorway that nobody holds is taken down.
    /// </summary>
    private static void Reconcile(Map map)
    {
        ReadBack = 0;
        PutBack = 0;
        Orphans = 0;

        foreach (var held in new List<Holding>(_held.Values))
        {
            var building = BotHouseSurvey.ByDoor(held.Key);

            if (building == null)
            {
                logger.Warning(
                    "Guild houses: the house of {Guild} in {Town} at {X},{Y} is not among the surveyed buildings any more; it is emptied",
                    held.Guild,
                    held.Town,
                    held.Key.X,
                    held.Key.Y
                );

                Vacate(held, map, "its building is gone from the survey", false);

                continue;
            }

            held.Building = building;
            held.Heart = building.Heart;
            ReadBack++;
            PutBack += Hang(held, map);

            if (BotTowns.Find(held.Town) is { } town)
            {
                town.Seated++;
            }
        }

        for (var i = 0; i < BotHouseSurvey.All.Count; i++)
        {
            var building = BotHouseSurvey.All[i];
            string holder = null;

            foreach (var held in _held.Values)
            {
                if (held.Building == building)
                {
                    holder = held.Guild;

                    break;
                }
            }

            Orphans += TakeDown(building, map, holder);
        }

        logger.Information(
            "Guild houses: {Houses} read back from the save ({List}); {PutBack} doors and signs put back that the world had lost, {Orphans} of nobody's taken down",
            ReadBack,
            ReadBack == 0 ? "none" : Tell(),
            PutBack,
            Orphans
        );
    }

    /// <summary>Counts the free buildings, town by town, and why the rest are not free. For the summary and the page.</summary>
    private static void Count(Map map)
    {
        _free.Clear();
        _why.Clear();

        for (var i = 0; i < BotHouseSurvey.All.Count; i++)
        {
            var building = BotHouseSurvey.All[i];

            if (HolderOf(building) != null)
            {
                _why["held by a guild"] = _why.GetValueOrDefault("held by a guild") + 1;

                continue;
            }

            var why = BotHouseSurvey.Occupied(building);

            if (why == null)
            {
                _free[building.Town] = _free.GetValueOrDefault(building.Town) + 1;

                continue;
            }

            // Grouped by the first words, so "a spawner stands in it (Banker)" and "(Tailor)" are one reason.
            var cut = why.IndexOf(" (", StringComparison.Ordinal);
            var kind = cut > 0 ? why[..cut] : why.Contains(" lives in it", StringComparison.Ordinal) ? "somebody lives in it" : why;

            _why[kind] = _why.GetValueOrDefault(kind) + 1;
        }
    }

    /// <summary>Free buildings in this town at the last count.</summary>
    public static int Free(string town) => town == null ? 0 : _free.GetValueOrDefault(town);

    /// <summary>Guild houses in this town now.</summary>
    public static int Houses(string town)
    {
        var many = 0;

        foreach (var held in _held.Values)
        {
            if (string.Equals(held.Town, town, StringComparison.OrdinalIgnoreCase))
            {
                many++;
            }
        }

        return many;
    }

    private static string HolderOf(BotHouseSurvey.Building building)
    {
        foreach (var held in _held.Values)
        {
            if (held.Building == building || held.Building == null && held.Key == building.Door)
            {
                return held.Guild;
            }
        }

        return null;
    }

    /// <summary>
    /// One look over the guilds: a guild gone from the roll leaves its house; a guild without one settles if it can — abroad
    /// in a free building, or in Britain when it has saved the price.
    /// </summary>
    private static void Look(Map map)
    {
        if (!BotTowns.Ready)
        {
            return;
        }

        foreach (var held in new List<Holding>(_held.Values))
        {
            var guild = BotGuilds.Find(held.Guild);

            if (guild != null && Members(guild) > 0)
            {
                held.Gone = 0;

                continue;
            }

            if (++held.Gone >= Math.Max(1, GoneLooks))
            {
                Vacate(held, map, "the guild is gone", true);
            }
        }

        foreach (var guild in new List<Guild>(BotGuilds.Standing))
        {
            if (guild is not { Disbanded: false } || BotUnderworld.Band(guild) || Members(guild) == 0 || _held.ContainsKey(guild.Name)
                || _pinned.Contains(guild.Name))
            {
                continue;
            }

            if (Wish(guild))
            {
                Britain(guild, map);
            }
            else
            {
                Abroad(guild, map);
            }
        }
    }

    private static int Members(Guild guild)
    {
        var many = 0;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false })
            {
                many++;
            }
        }

        return many;
    }

    /// <summary>What this guild wishes, drawn the first time it is asked and kept: true for Britain, false for another town.</summary>
    private static bool Wish(Guild guild)
    {
        if (_britain.TryGetValue(guild.Name, out var britain))
        {
            return britain;
        }

        var bare = BotSeat.Bare(guild.Name);

        britain = bare != Point3D.Zero
            ? BotTowns.Nearest(bare) is { Home: true }
            : Utility.RandomDouble() >= BotSeat.AbroadShare;

        _britain[guild.Name] = britain;

        logger.Information(
            "{Guild} will settle {Where}",
            guild.Name,
            britain
                ? $"in Britain, for {BritainPrice}gp{(BritainAfterHall ? " once its hall stands" : "")}"
                : bare != Point3D.Zero
                    ? $"in an empty building by its seat at {bare.X},{bare.Y}, or in another town with room"
                    : "in an empty building of another town"
        );

        return britain;
    }

    /// <summary>A guild wishing abroad: the free building nearest the bank of the town with the fewest guild houses, its own town first.</summary>
    private static void Abroad(Guild guild, Map map)
    {
        var bare = BotSeat.Bare(guild.Name);
        var own = bare != Point3D.Zero ? BotTowns.Nearest(bare) : null;
        var hall = BotEstate.Hall(guild);

        // <b>A guild that raised its hall at home while it waited for a building abroad stays at home.</b> A raised hall moves
        // the seat to itself (BotHall), and a guild with a hall moves only within its own town — anywhere else would carry
        // the hall across the island. So its wish becomes Britain, said once.
        if (own is { Home: true } && hall != null)
        {
            _britain[guild.Name] = true;
            logger.Information("{Guild} raised its hall by Britain before a building abroad was free; it will settle in Britain, for {Price}gp", guild.Name, BritainPrice);

            return;
        }

        // A bare seat that no town claims is left as it is.
        if (bare != Point3D.Zero && own is null or { Home: true })
        {
            return;
        }

        var towns = new List<BotTowns.Town>();

        for (var i = 0; i < BotTowns.All.Count; i++)
        {
            var town = BotTowns.All[i];

            if (!Fit(town) || town.Home || ReferenceEquals(town, BotSettle.HomeTown) || Houses(town.Name) >= MostPerTown)
            {
                continue;
            }

            // A town the last count found nothing free in is not searched building by building again every minute; the count
            // is ten minutes old at most, and a house left empty counts itself back at once (Vacate).
            if (Free(town.Name) == 0)
            {
                continue;
            }

            // <b>A guild seated at a bare point by a town moves only within that town (merged as build 329).</b> Patrick's answer
            // of 29.09.2026 morning: "The Needle stays in Cove; guilds may live in other towns". Cove has no free building, so
            // rehousing "elsewhere while it has no hall" would have carried The Needle off to another town. It keeps its bare
            // seat until a building in its own town is free.
            if (own != null && !ReferenceEquals(town, own))
            {
                continue;
            }

            towns.Add(town);
        }

        var home = BotPopulation.Where;

        towns.Sort(
            (a, b) =>
            {
                if (ReferenceEquals(a, own) != ReferenceEquals(b, own))
                {
                    return ReferenceEquals(a, own) ? -1 : 1;
                }

                var byHouses = Houses(a.Name).CompareTo(Houses(b.Name));

                return byHouses != 0 ? byHouses : BotSeat.Gap(a.Square, home).CompareTo(BotSeat.Gap(b.Square, home));
            }
        );

        for (var i = 0; i < towns.Count; i++)
        {
            var building = Pick(towns[i], guild, hall);

            if (building != null && Settle(guild, building, towns[i], false, TownPrice, map))
            {
                return;
            }
        }

        NoRoom++;
        Say(guild, "finds no free building in a town fit to live in with room for another guild house");
    }

    /// <summary>A guild wishing for Britain: its hall first, then the price, then the free building nearest its hall or the bank.</summary>
    private static void Britain(Guild guild, Map map)
    {
        var britain = BotSettle.HomeTown ?? HomeTown();

        if (britain == null)
        {
            return;
        }

        var hall = BotEstate.Hall(guild);

        if (BritainAfterHall && BotEstate.Running && hall == null)
        {
            AwaitingHall++;

            return;
        }

        if (Houses(britain.Name) >= MostInBritain)
        {
            BritainFull++;
            Say(guild, $"has no room in Britain: {MostInBritain} guild houses stand there");

            return;
        }

        if (BotEstate.Fund(guild) < BritainPrice)
        {
            SavingBritain++;

            return;
        }

        var building = Pick(britain, guild, hall);

        if (building == null)
        {
            NoBuilding++;
            Say(guild, "has the price of a house in Britain and finds no free building there");

            return;
        }

        Settle(guild, building, britain, true, BritainPrice, map);
    }

    private static BotTowns.Town HomeTown()
    {
        for (var i = 0; i < BotTowns.All.Count; i++)
        {
            if (BotTowns.All[i].Home)
            {
                return BotTowns.All[i];
            }
        }

        return null;
    }

    /// <summary>Whether a town may be settled in: joined to home by the gates, and fit to live in when that is asked.</summary>
    private static bool Fit(BotTowns.Town town)
    {
        if (!town.FromHome)
        {
            return false;
        }

        // Only a town on the mainland, as a hall is (01.10.2026, build 379). See BotPlot.Mainland.
        if (BotPlot.Mainland && BotPopulation.Home is { } map
            && (!BotGates.Ready || BotGates.LandOf(map, town.Square) != BotGates.LandOf(map, BotPopulation.Where)))
        {
            return false;
        }

        if (!FitTownsOnly || !BotResidence.Active || !BotSettle.Reckoned)
        {
            return true;
        }

        return BotSettle.Of(town) is { Fit: true };
    }

    /// <summary>
    /// The free building of this town the guild would take: nearest its hall when it has one (so a hall is not carried for
    /// the move), else nearest the town's bank, where the shops are. Asked live, building by building, nearest first.
    /// </summary>
    private static BotHouseSurvey.Building Pick(BotTowns.Town town, Guild guild, BaseHouse hall)
    {
        var near = hall is { Deleted: false } ? hall.Location : BotSettle.Hearth(town);
        var found = new List<BotHouseSurvey.Building>();

        for (var i = 0; i < BotHouseSurvey.All.Count; i++)
        {
            var building = BotHouseSurvey.All[i];

            if (string.Equals(building.Town, town.Name, StringComparison.OrdinalIgnoreCase) && HolderOf(building) == null)
            {
                found.Add(building);
            }
        }

        found.Sort((a, b) => BotSeat.Gap(a.Heart, near).CompareTo(BotSeat.Gap(b.Heart, near)));

        for (var i = 0; i < found.Count; i++)
        {
            // Not a building whose heart lies on ground another guild holds (06.10.2026). See BotBounds.
            if (BotBounds.Foreign(guild.Name, BotPopulation.Home, found[i].Heart, out _))
            {
                BotBounds.HouseRefused();

                continue;
            }

            if (BotHouseSurvey.Occupied(found[i], guild.Name) == null)
            {
                return found[i];
            }
        }

        return null;
    }

    /// <summary>
    /// The guild moves in: the price levied and paid to the city when there is one, a door put into every doorway, the sign
    /// hung, the record written. False, with nothing taken, when the doorway is blocked or the levy falls short.
    /// </summary>
    private static bool Settle(Guild guild, BotHouseSurvey.Building building, BotTowns.Town town, bool britain, int price, Map map)
    {
        for (var i = 0; i < building.Entrances.Length; i++)
        {
            var doors = building.Entrances[i].Doors;

            for (var j = 0; j < doors.Length; j++)
            {
                // The engine's own test before it puts a door in (DoorGenerator.AddDoor): nothing standing in the doorway.
                if (!map.CanFit(doors[j].X, doors[j].Y, doors[j].Z, 16, false, false))
                {
                    Blocked++;

                    return false;
                }
            }
        }

        if (price > 0)
        {
            var paid = new List<BotEstate.Contribution>();
            var got = BotEstate.Levy(guild, price, paid);

            if (got < price)
            {
                BotEstate.Refund(paid);
                Short++;

                return false;
            }

            // Into the city's treasury, where a claim's price goes: the coin goes round rather than out of the world.
            BotCity.Tax(price);
            PaidGold += price;
        }

        var held = new Holding
        {
            Guild = guild.Name,
            Town = town.Name,
            Key = building.Door,
            Heart = building.Heart,
            Britain = britain,
            Paid = price,
            Since = Core.Now,
            Building = building
        };

        _held[guild.Name] = held;
        town.Seated++;
        Hang(held, map);
        Settled++;

        if (britain)
        {
            SettledBritain++;
        }

        _free[town.Name] = Math.Max(0, _free.GetValueOrDefault(town.Name) - 1);

        var door = building.Entrances[0].Doors[0];

        logger.Information(
            "{Guild} settles in {Town}: the empty building at {X},{Y} ({Area} tiles of floor, {Ways} way{S} in) is its house now — a door put into the doorway at {DX},{DY} and its sign hung beside it; {Paid}; its {Members} members live in {Town} from now on, born and risen there",
            guild.Name,
            town.Name,
            building.Heart.X,
            building.Heart.Y,
            building.Area,
            building.Entrances.Length,
            building.Entrances.Length == 1 ? "" : "s",
            door.X,
            door.Y,
            price > 0 ? $"{price}gp paid to the city out of its chest and members" : "nothing paid, the building stood empty",
            Members(guild),
            town.Name
        );

        BotEvents.Post("house", guild.Leader, $"{guild.Name} settles in {town.Name} at {building.Heart.X},{building.Heart.Y}{(price > 0 ? $" for {price}gp" : "")}");

        return true;
    }

    /// <summary>
    /// The guild leaves its house: the doors and sign come down and the building is empty again. Also for the boot, when a
    /// record names a building the survey no longer has.
    /// </summary>
    private static void Vacate(Holding held, Map map, string why, bool counted)
    {
        _held.Remove(held.Guild);

        if (held.Building != null)
        {
            TakeDown(held.Building, map, null);
        }
        else if (map != null)
        {
            // A building the survey no longer has: its doors and sign are found round the door tile the record names, by whose
            // they are, so nothing of the guild's is left standing in a doorway nobody will ever look at again.
            var gone = new List<Item>();

            foreach (var door in map.GetItemsInRange<BotGuildDoor>(held.Key, 3))
            {
                if (string.Equals(door.Guild, held.Guild, StringComparison.OrdinalIgnoreCase))
                {
                    gone.Add(door);
                }
            }

            foreach (var sign in map.GetItemsInRange<BotGuildSign>(held.Key, 4))
            {
                if (string.Equals(sign.Guild, held.Guild, StringComparison.OrdinalIgnoreCase))
                {
                    gone.Add(sign);
                }
            }

            for (var i = 0; i < gone.Count; i++)
            {
                gone[i].Delete();
            }
        }

        if (BotTowns.Surveyed && BotTowns.Find(held.Town) is { } town)
        {
            town.Seated = Math.Max(0, town.Seated - 1);
        }

        if (held.Town != null)
        {
            _free[held.Town] = _free.GetValueOrDefault(held.Town) + 1;
        }

        if (counted)
        {
            Vacated++;
        }

        logger.Information("{Guild} leaves its house in {Town} at {X},{Y}: {Why}; the door and the sign are taken down and the building stands empty", held.Guild, held.Town, held.Heart.X, held.Heart.Y, why);
    }

    /// <summary>
    /// Makes sure this house has its doors and its sign: what stands is kept, what is missing is put in. Returns how many
    /// items were put in.
    /// </summary>
    private static int Hang(Holding held, Map map)
    {
        var building = held.Building;

        if (building == null || map == null)
        {
            return 0;
        }

        var put = 0;

        for (var i = 0; i < building.Entrances.Length; i++)
        {
            var way = building.Entrances[i];

            if (Doors(way, map, held.Guild) == way.Doors.Length)
            {
                continue;
            }

            Remove(way, map);
            put += PutDoors(way, map, held.Guild);
        }

        if (Sign(building, map, held.Guild) == null)
        {
            put += HangSign(held, building, map);
        }

        return put;
    }

    /// <summary>How many of this guild's doors stand in this doorway, shut in it or swung open beside it.</summary>
    private static int Doors(BotHouseSurvey.Entrance way, Map map, string guild)
    {
        var many = 0;

        foreach (var door in map.GetItemsInRange<BotGuildDoor>(way.Doors[0], 3))
        {
            if (door.Deleted || !string.Equals(door.Guild, guild, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var at = door.Open ? new Point3D(door.X - door.Offset.X, door.Y - door.Offset.Y, door.Z) : door.Location;

            for (var j = 0; j < way.Doors.Length; j++)
            {
                if (way.Doors[j].X == at.X && way.Doors[j].Y == at.Y)
                {
                    many++;
                }
            }
        }

        return many;
    }

    /// <summary>Every door of ours in this doorway, whosever it is, deleted. Returns how many.</summary>
    private static int Remove(BotHouseSurvey.Entrance way, Map map, string sparing = null)
    {
        List<BotGuildDoor> gone = null;

        foreach (var door in map.GetItemsInRange<BotGuildDoor>(way.Doors[0], 3))
        {
            if (door.Deleted || sparing != null && string.Equals(door.Guild, sparing, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var at = door.Open ? new Point3D(door.X - door.Offset.X, door.Y - door.Offset.Y, door.Z) : door.Location;

            for (var j = 0; j < way.Doors.Length; j++)
            {
                if (way.Doors[j].X == at.X && way.Doors[j].Y == at.Y)
                {
                    (gone ??= []).Add(door);

                    break;
                }
            }
        }

        if (gone == null)
        {
            return 0;
        }

        for (var i = 0; i < gone.Count; i++)
        {
            gone[i].Delete();
        }

        return gone.Count;
    }

    /// <summary>
    /// The engine's own door, or its linked pair, put into a doorway with the facings the engine's door generator gives the
    /// same frames (<c>DoorGenerator.Generate</c>): a single door in a row faces WestCW and a pair WestCW and EastCCW; in a
    /// column a single door faces SouthCW and a pair NorthCCW and SouthCW.
    /// </summary>
    private static int PutDoors(BotHouseSurvey.Entrance way, Map map, string guild)
    {
        if (way.Doors.Length == 1)
        {
            var door = new BotGuildDoor(way.AlongX ? DoorFacing.WestCW : DoorFacing.SouthCW, guild);

            door.MoveToWorld(way.Doors[0], map);
            DoorsPut++;

            return 1;
        }

        var first = new BotGuildDoor(way.AlongX ? DoorFacing.WestCW : DoorFacing.NorthCCW, guild);
        var second = new BotGuildDoor(way.AlongX ? DoorFacing.EastCCW : DoorFacing.SouthCW, guild);

        first.MoveToWorld(way.Doors[0], map);
        second.MoveToWorld(way.Doors[1], map);
        first.Link = second;
        second.Link = first;
        DoorsPut += 2;

        return 2;
    }

    /// <summary>
    /// Where a building's sign hangs, and which way it faces: beside its first door, one tile out, as the engine's own shop
    /// signs hang (<see cref="BotGuildSign"/>). A door in a row has a west-facing sign one tile west of it; a door in a column
    /// a north-facing sign one tile north of it.
    /// </summary>
    private static (Point3D At, SignFacing Facing) SignSpot(BotHouseSurvey.Building building)
    {
        var way = building.Entrances[0];
        var door = way.Doors[0];

        return way.AlongX
            ? (new Point3D(door.X - 1, door.Y + way.OutY, door.Z), SignFacing.West)
            : (new Point3D(door.X + way.OutX, door.Y - 1, door.Z), SignFacing.North);
    }

    /// <summary>The sign of this guild at this building's spot, or null.</summary>
    private static BotGuildSign Sign(BotHouseSurvey.Building building, Map map, string guild)
    {
        var (at, _) = SignSpot(building);

        foreach (var sign in map.GetItemsInRange<BotGuildSign>(at, 0))
        {
            if (!sign.Deleted && string.Equals(sign.Guild, guild, StringComparison.OrdinalIgnoreCase))
            {
                return sign;
            }
        }

        return null;
    }

    private static int HangSign(Holding held, BotHouseSurvey.Building building, Map map)
    {
        var (at, facing) = SignSpot(building);
        var guild = BotGuilds.Find(held.Guild);
        var sign = new BotGuildSign(held.Britain ? SignType.BrassSign : SignType.WoodenSign, facing, held.Guild)
        {
            Name = guild?.Abbreviation is { Length: > 0 } tag ? $"{held.Guild} [{tag}]" : held.Guild
        };

        sign.MoveToWorld(at, map);
        SignsHung++;

        return 1;
    }

    /// <summary>
    /// Takes down every door and sign of ours at this building that is not <paramref name="holder"/>'s. Returns how many
    /// items came down.
    /// </summary>
    private static int TakeDown(BotHouseSurvey.Building building, Map map, string holder)
    {
        if (map == null)
        {
            return 0;
        }

        var gone = 0;

        for (var i = 0; i < building.Entrances.Length; i++)
        {
            gone += Remove(building.Entrances[i], map, holder);
        }

        var (at, _) = SignSpot(building);
        List<BotGuildSign> signs = null;

        foreach (var sign in map.GetItemsInRange<BotGuildSign>(at, 1))
        {
            if (!sign.Deleted && (holder == null || !string.Equals(sign.Guild, holder, StringComparison.OrdinalIgnoreCase)))
            {
                (signs ??= []).Add(sign);
            }
        }

        if (signs != null)
        {
            for (var i = 0; i < signs.Count; i++)
            {
                signs[i].Delete();
            }

            gone += signs.Count;
        }

        return gone;
    }

    /// <summary>Says once an hour, per guild, that a guild wishing for a house has nowhere to go and why.</summary>
    private static void Say(Guild guild, string what)
    {
        var now = Core.TickCount;

        if (_said.TryGetValue(guild.Name, out var last) && now - last < 3600000)
        {
            return;
        }

        _said[guild.Name] = now;
        logger.Information("{Guild} {What}", guild.Name, what);
    }

    /// <summary>
    /// Every house emptied and every wish forgotten, for the population's reset: the doors and signs are taken down, so a
    /// new world begins with the towns' buildings empty. Returns how many houses were emptied.
    /// </summary>
    public static int Wipe()
    {
        var map = BotPopulation.Home;
        var gone = _held.Count;

        foreach (var held in new List<Holding>(_held.Values))
        {
            held.Building ??= BotHouseSurvey.ByDoor(held.Key);
            Vacate(held, map, "the world is reset", false);
        }

        _held.Clear();
        _britain.Clear();
        _pinned.Clear();
        _said.Clear();

        return gone;
    }

    /// <summary>The houses and the wishes, for the store.</summary>
    internal static void Save(IGenericWriter writer)
    {
        writer.WriteEncodedInt(_held.Count);

        foreach (var held in _held.Values)
        {
            writer.Write(held.Guild);
            writer.Write(held.Town);
            writer.Write(held.Key);
            writer.Write(held.Heart);
            writer.Write(held.Britain);
            writer.WriteEncodedInt(held.Paid);
            writer.Write(held.Since);
        }

        writer.WriteEncodedInt(_britain.Count);

        foreach (var (guild, britain) in _britain)
        {
            writer.Write(guild);
            writer.Write(britain);
        }

        writer.WriteEncodedInt(_pinned.Count);

        foreach (var guild in _pinned)
        {
            writer.Write(guild);
        }
    }

    /// <summary>The houses and wishes read back. Returns how many houses.</summary>
    internal static int Load(IGenericReader reader)
    {
        _held.Clear();
        _britain.Clear();

        var houses = reader.ReadEncodedInt();

        for (var i = 0; i < houses; i++)
        {
            var held = new Holding
            {
                Guild = reader.ReadString(),
                Town = reader.ReadString(),
                Key = reader.ReadPoint3D(),
                Heart = reader.ReadPoint3D(),
                Britain = reader.ReadBool(),
                Paid = reader.ReadEncodedInt(),
                Since = reader.ReadDateTime()
            };

            if (!string.IsNullOrEmpty(held.Guild))
            {
                _held[held.Guild] = held;
            }
        }

        var wishes = reader.ReadEncodedInt();

        for (var i = 0; i < wishes; i++)
        {
            var guild = reader.ReadString();
            var britain = reader.ReadBool();

            if (!string.IsNullOrEmpty(guild))
            {
                _britain[guild] = britain;
            }
        }

        _pinned.Clear();

        var pinned = reader.ReadEncodedInt();

        for (var i = 0; i < pinned; i++)
        {
            var guild = reader.ReadString();

            if (!string.IsNullOrEmpty(guild))
            {
                _pinned.Add(guild);
            }
        }

        return _held.Count;
    }

    /// <summary>One clause per house, for the boot line and Argus.</summary>
    public static string Tell()
    {
        if (_held.Count == 0)
        {
            return "no guild has a house";
        }

        using var say = Server.Text.ValueStringBuilder.Create(256);

        foreach (var held in _held.Values)
        {
            say.Append(say.Length > 0 ? ", " : "");
            say.Append($"{held.Guild} in {held.Town} at {held.Heart.X},{held.Heart.Y}{(held.Britain ? $" (its home, bought for {held.Paid}gp)" : "")}");
        }

        return say.ToString();
    }

    /// <summary>What each guild without a house is waiting for, for the summary.</summary>
    private static string Waiting()
    {
        using var say = Server.Text.ValueStringBuilder.Create(256);

        foreach (var guild in BotGuilds.Standing)
        {
            if (guild is not { Disbanded: false } || BotUnderworld.Band(guild) || _held.ContainsKey(guild.Name))
            {
                continue;
            }

            say.Append(say.Length > 0 ? ", " : "");
            say.Append(guild.Name);

            if (_pinned.Contains(guild.Name))
            {
                say.Append(" keeps the seat set by hand");

                continue;
            }

            if (!_britain.TryGetValue(guild.Name, out var britain))
            {
                say.Append(" has not chosen yet");

                continue;
            }

            if (!britain)
            {
                say.Append(" wants a building abroad and has found none free");

                continue;
            }

            if (BritainAfterHall && BotEstate.Running && BotEstate.Hall(guild) == null)
            {
                say.Append($" wants Britain once its hall stands ({BotEstate.Fund(guild)} of the hall's {BotEstate.Price}gp)");

                continue;
            }

            say.Append($" saves for Britain ({BotEstate.Fund(guild)} of {BritainPrice}gp)");
        }

        return say.Length == 0 ? "every guild has a house" : say.ToString();
    }

    /// <summary>The summary's "Houses:" line.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "guilds do not settle in town buildings";
        }

        if (!BotHouseSurvey.Ready)
        {
            return $"{Tell()}; {BotHouseSurvey.Describe()}";
        }

        using var free = Server.Text.ValueStringBuilder.Create(256);

        foreach (var (town, many) in _free)
        {
            free.Append(free.Length > 0 ? ", " : "");
            free.Append($"{town} {many}");
        }

        using var why = Server.Text.ValueStringBuilder.Create(256);

        foreach (var (kind, many) in _why)
        {
            why.Append(why.Length > 0 ? ", " : "");
            why.Append($"{many} {kind}");
        }

        return $"{Tell()}; waiting: {Waiting()}; {BotHouseSurvey.Describe()}, free now: {(free.Length == 0 ? "none" : free.ToString())} (not free: {(why.Length == 0 ? "none" : why.ToString())}); "
               + $"at most {MostPerTown} a town and {MostInBritain} in Britain, a town house {(TownPrice > 0 ? $"{TownPrice}gp" : "free")}, Britain {BritainPrice}gp{(BritainAfterHall ? " after the hall" : "")}; "
               + $"{Settled} settled ({SettledBritain} in Britain, {PaidGold}gp paid to the city), {Vacated} left, {DoorsPut} doors put in and {SignsHung} signs hung, {Turned} strangers turned away at a guild's door; "
               + $"looks: {NoRoom} found no free building abroad, {AwaitingHall} waited for the hall before Britain, {SavingBritain} were saving for Britain, {BritainFull} found Britain full, {NoBuilding} found no free building in Britain, {Short} levies fell short, {Blocked} doorways were blocked";
    }

    public static void Forget()
    {
        Settled = 0;
        SettledBritain = 0;
        PaidGold = 0;
        Vacated = 0;
        DoorsPut = 0;
        SignsHung = 0;
        Turned = 0;
        NoRoom = 0;
        AwaitingHall = 0;
        SavingBritain = 0;
        BritainFull = 0;
        NoBuilding = 0;
        Short = 0;
        Blocked = 0;
        _reconciled = false;
        Started = false;
        _free.Clear();
        _why.Clear();
        _said.Clear();
    }
}
