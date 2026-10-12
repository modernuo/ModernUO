using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Where each guild lives: the point its hall is raised near, its members are born at and rise again at,
/// and the place "home" means to them.
///
/// <para>
/// <b>Patrick's order of 13.09.2026: the bots all live by the graveyard; he wants them south, east and
/// west as well, and one guild moved to the south.</b> The reason they were all there is arithmetic, not
/// preference. The population has one home, 1440,1470, and every bot that dies is put back on its feet
/// there — 3,597 times in the twelve hours before this was written, every one of them onto the same
/// patch. The plot search spirals outward from that same point and Britain's town and the graveyard's
/// belt shut off everything south and west of it, so the four halls went up in the one strip of ground
/// left, sixty tiles north, with their yards on top of each other. Every hall touching every other is
/// what the border drift was written to sour, and it did: that strip is where the wars came from.
/// </para>
///
/// <para>
/// <b>A seat is one point per guild and everything else follows from it.</b> The plot search starts at
/// the seat; a member is born and revived beside the guild's hall, or at the seat until it has one; the
/// homeward walk goes there; and a hall standing further than <see cref="Settled"/> from its seat is
/// carried to it by the same errand that carries a beaten guild out of the winner's yard. So "move a
/// guild south" is one line in <c>bot-estate.json</c> or one word to Argus, and the guild does the rest.
/// </para>
///
/// <para>
/// <b>Configured, overridden by hand, and the hand's word is kept across restarts.</b> The file is the
/// design; the store is what somebody said at the keyboard on a live shard, which would otherwise be lost
/// at the next start and have to be said again. The store only ever holds overrides.
/// </para>
/// </summary>
public static class BotSeat
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSeat));

    /// <summary>Whether guilds have seats at all. Off, and everything is where the population's home is.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// How far a hall may stand from its seat and still be at home.
    ///
    /// A hundred and twenty: ground the engine will take a house on is scarce, so the nearest plot to a seat
    /// is often fifty tiles off, and a rule that asked for less would move the same hall every half hour.
    /// Wider than a yard, narrower than the distance between two seats.
    /// </summary>
    public static int Settled { get; set; } = 120;

    /// <summary>How long a guild rests between one carrying of its hall and the next.</summary>
    public static int MoveEveryMs { get; set; } = 1800000;

    /// <summary>How far around the seat a born or revived member is put down.</summary>
    public static int Spread { get; set; } = 6;

    /// <summary>Placements made at a guild's hall or seat rather than at the population's home.</summary>
    public static long Homed { get; private set; }

    /// <summary>Seats set by hand.</summary>
    public static long Spoken { get; private set; }

    /// <summary>
    /// The share of newly founded guilds that settle by another reachable town rather than at the population's home.
    ///
    /// <para>
    /// <b>Patrick's order of 29.09.2026: "some guilds may decide to settle by other towns, so as not to be so dense round
    /// Britain."</b> A guild founded on the shard had no seat and so lived at the population's home with every other; now
    /// it is seated, at founding, by a town the gates say its members can walk to — the town with the fewest guilds by it
    /// — or at home, by this share. Written into the store like a seat set by hand, so a restart keeps it.
    /// </para>
    /// </summary>
    public static double AbroadShare { get; set; } = 0.5;

    /// <summary>How far from a town's square a guild's seat is put, so two guilds by one town are not on one tile.</summary>
    public static int TownSpread { get; set; } = 40;

    /// <summary>Guilds seated by a town other than home's, and those seated at home, at founding.</summary>
    public static long Abroad { get; private set; }

    public static long AtHome { get; private set; }

    /// <summary>Halls carried to their seat.</summary>
    public static long Carried { get; private set; }

    private static readonly Dictionary<string, Point3D> _configured = [];

    private static readonly Dictionary<string, Point3D> _overrides = [];

    private static readonly Dictionary<string, long> _moved = [];

    /// <summary>What the file said. Applied after the store, and never over it.</summary>
    public static void Configure(Dictionary<string, int[]> seats)
    {
        _configured.Clear();

        if (seats == null)
        {
            return;
        }

        foreach (var (name, at) in seats)
        {
            if (string.IsNullOrWhiteSpace(name) || at is not { Length: >= 2 })
            {
                continue;
            }

            var seat = new Point3D(at[0], at[1], at.Length > 2 ? at[2] : 0);

            // Not beside another guild's seat. See TooNear: the file's own pair once stood 62 tiles apart.
            if (TooNear(name, seat, out var other, out var gap))
            {
                Crowded++;

                logger.Warning(
                    "The file seats {Guild} at {X},{Y}, {Gap} tiles from the seat of {Other}, nearer than the {Neighbouring} at which guilds sour on each other; that seat is ignored",
                    name,
                    seat.X,
                    seat.Y,
                    gap,
                    other,
                    BotRegard.Neighbouring
                );

                continue;
            }

            _configured[name] = seat;
        }
    }

    /// <summary>Seats refused, in the file or by hand, for standing nearer another guild's than <see cref="BotRegard.Neighbouring"/>.</summary>
    public static long Crowded { get; private set; }

    /// <summary>
    /// Whether a seat for <paramref name="guild"/> at <paramref name="at"/> would stand nearer another guild's seat than
    /// guilds may live without souring on each other, and which guild, and how near.
    ///
    /// <para>
    /// <b>One distance for one fact.</b> <c>BotRegard</c> sours two guilds whose halls stand nearer than
    /// <see cref="BotRegard.Neighbouring"/>, and nothing kept seats outside it: the file seated The Crown and The Blade 62
    /// tiles apart, and both wars of 15.09.2026 were The Blade against a neighbour. Asked of seats set in the file and by
    /// hand, and never of the seat that follows a hall carried out of a winner's yard, which the sentence places and must
    /// not be refused by it. Fable's option, taken on Patrick's word on 15.09.2026.
    /// </para>
    /// </summary>
    public static bool TooNear(string guild, Point3D at, out string other, out int gap)
    {
        other = null;
        gap = int.MaxValue;

        foreach (var name in Named())
        {
            if (string.Equals(name, guild, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var seat = Of(name);

            if (seat == Point3D.Zero)
            {
                continue;
            }

            var apart = Gap(at, seat);

            if (apart < BotRegard.Neighbouring && apart < gap)
            {
                other = name;
                gap = apart;
            }
        }

        return other != null;
    }

    /// <summary>Every guild with a seat of any kind: set by hand, in the file, or where its hall stands.</summary>
    private static List<string> Named()
    {
        var names = new List<string>();

        foreach (var name in _overrides.Keys)
        {
            names.Add(name);
        }

        foreach (var name in _configured.Keys)
        {
            if (!names.Contains(name))
            {
                names.Add(name);
            }
        }

        foreach (var name in BotEstate.Held.Keys)
        {
            if (!names.Contains(name))
            {
                names.Add(name);
            }
        }

        foreach (var held in BotGuildHouses.All)
        {
            if (!names.Contains(held.Guild))
            {
                names.Add(held.Guild);
            }
        }

        return names;
    }

    /// <summary>
    /// The seat of this guild, or nothing. Its house in a town first (<see cref="BotGuildHouses"/>, 29.09.2026, evening:
    /// the building a guild settled in is where it lives), then overrides, then the file, then wherever its hall stands.
    /// </summary>
    public static Point3D Of(string guild)
    {
        if (!Running || guild == null)
        {
            return Point3D.Zero;
        }

        var housed = BotGuildHouses.Seat(guild);

        if (housed != Point3D.Zero)
        {
            return housed;
        }

        return Bare(guild) is var bare && bare != Point3D.Zero
            ? bare
            : BotEstate.Held.TryGetValue(guild, out var hall) && hall is { Deleted: false }
                ? hall.Location
                : Point3D.Zero;
    }

    /// <summary>
    /// The seat said at the keyboard, followed by a hall, chosen at founding or written in the file — a bare point, and
    /// not a house nor where the hall happens to stand. What <see cref="BotGuildHouses"/> asks to learn which town a guild
    /// seated before houses existed belongs to.
    /// </summary>
    public static Point3D Bare(string guild)
    {
        if (!Running || guild == null)
        {
            return Point3D.Zero;
        }

        if (_overrides.TryGetValue(guild, out var over))
        {
            return over;
        }

        return _configured.TryGetValue(guild, out var set) ? set : Point3D.Zero;
    }

    public static Point3D Of(Guild guild) => Of(guild?.Name);

    /// <summary>
    /// Where a bot is put down when it is born or rises again, and where "home" is to it.
    ///
    /// <para>
    /// Beside its guild's hall if the guild has one — the ban spot, which the engine keeps clear outside
    /// the sign — else at the guild's seat, else at the population's home, which is what every bot got
    /// before seats existed. The seat's height is looked up on the ground each time rather than kept,
    /// because the file only says x and y.
    /// </para>
    /// </summary>
    public static Point3D Home(BotMobile bot)
    {
        var where = BotPopulation.Where;

        if (!Running || bot?.Guild is not Guild guild)
        {
            return where;
        }

        // The Shadow rises at its hideout. See BotUnderworld.
        if (BotUnderworld.Member(bot) && BotUnderworld.Hideout != Point3D.Zero)
        {
            return BotUnderworld.Hideout;
        }

        // <b>A guild's house in a town before its hall</b> (29.09.2026, evening): the building the guild settled in is where
        // its members are born, rise and walk home to — inside a guarded town, by its bank and its healers — and the hall
        // out in the fields is the guild's estate. The heart is a floor tile the survey found, height and all. See
        // BotGuildHouses.
        var housed = BotGuildHouses.Seat(guild.Name);

        if (housed != Point3D.Zero)
        {
            return housed;
        }

        if (BotEstate.Hall(guild) is { Deleted: false } hall && hall.Map == BotPopulation.Home)
        {
            // A fallen member rises at the guild's outpost when it fell nearer to it than to the hall. Asked only of the
            // dead: the walk home of the living is to the hall. See BotOutpost (26.09.2026).
            if (!bot.Alive && BotOutpost.Nearer(guild, hall, bot) is var post && post != Point3D.Zero)
            {
                return post;
            }

            return hall.BanLocation;
        }

        var seat = Of(guild.Name);

        if (seat == Point3D.Zero || BotPopulation.Home == null)
        {
            return where;
        }

        if (!BotStep.Settle(BotPopulation.Home, seat.X, seat.Y, out var z))
        {
            return where;
        }

        return new Point3D(seat.X, seat.Y, z);
    }

    /// <summary>
    /// Counts a bot actually put down at its guild's hall or seat.
    ///
    /// Counted by the placement and not by <see cref="Home"/>, which the homeward proposer asks on every
    /// auction of every bot: counted there it read 1,382 in the first five minutes of 13.09.2026, for
    /// eighty births and no deaths. A counter that counts questions is not a counter of anything.
    /// </summary>
    public static void Placed() => Homed++;

    /// <summary>
    /// Whether this guild's hall stands too far from its seat and may be carried now.
    ///
    /// The rest between moves is here rather than in the errand, so that a guild whose nearest plot is
    /// still outside <see cref="Settled"/> does not spend the evening carrying its hall from one field to
    /// the next.
    /// </summary>
    public static bool Moving(Guild guild, out BaseHouse hall, out Point3D seat)
    {
        hall = null;
        seat = Point3D.Zero;

        if (!Running || guild == null)
        {
            return false;
        }

        hall = BotEstate.Hall(guild);
        seat = Of(guild.Name);

        // <b>A guild with a house is carried towards the seat its hall last set, not towards the house</b> (29.09.2026,
        // evening). Raising a hall and losing a war both move the bare seat to the hall (BotHall, BotRemove) precisely so
        // the hall is not carried straight back — back into the winner's yard, after an exile. The house outranks that
        // seat for everything else, and would undo it here; so the carrying keeps asking the seat the hall set.
        if (BotGuildHouses.Held(guild) && Bare(guild.Name) is var set && set != Point3D.Zero)
        {
            seat = set;
        }

        if (hall is not { Deleted: false } || seat == Point3D.Zero)
        {
            return false;
        }

        if (Gap(hall.Location, seat) <= Settled)
        {
            return false;
        }

        return !_moved.TryGetValue(guild.Name, out var last) || Core.TickCount - (last + MoveEveryMs) >= 0;
    }

    /// <summary>Chebyshev, like every other distance on this shard.</summary>
    public static int Gap(Point3D a, Point3D b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    /// <summary>The hall has been carried, or an attempt was made; the guild rests before the next.</summary>
    public static void Moved(string guild, bool carried)
    {
        if (guild == null)
        {
            return;
        }

        _moved[guild] = Core.TickCount;

        if (carried)
        {
            Carried++;
        }
    }

    /// <summary>A seat said at the keyboard. Kept in the store across restarts.</summary>
    public static void Set(string guild, Point3D at) => Set(guild, at, true);

    /// <summary>
    /// The same, and whether a hand said it.
    ///
    /// <para>
    /// <b>A lost war moves the seat with the hall.</b> The sentence carries the loser's hall out of the
    /// winner's yard; left with its old seat, the guild would find its hall "too far from home" and carry
    /// it straight back in, and the sentence would have lasted half an hour. So the exile resets the seat
    /// to where the hall now stands, kept in the store like a hand's word, because that is what it is: an
    /// order about where the guild lives.
    /// </para>
    /// </summary>
    public static void Set(string guild, Point3D at, bool byHand)
    {
        if (string.IsNullOrEmpty(guild))
        {
            return;
        }

        _overrides[guild] = at;
        _moved.Remove(guild);

        if (byHand)
        {
            Spoken++;

            // The hand's word outranks a guild's house in a town: the guild leaves it and is not settled again. See
            // BotGuildHouses.Pin (29.09.2026, evening).
            BotGuildHouses.Pin(guild);

            logger.Warning("The seat of {Guild} is now {X},{Y}; its hall will be carried there if it stands further than {Settled} tiles off", guild, at.X, at.Y, Settled);

            return;
        }

        logger.Information("The seat of {Guild} follows its hall to {X},{Y}", guild, at.X, at.Y);
    }

    /// <summary>
    /// A bare seat moved off another guild's held ground (06.10.2026, <see cref="BotBounds"/>): kept in the store like any seat
    /// that is not the file's, said with the reason, and not a hand's word, so a guild's house is not left for it.
    /// </summary>
    public static void Shift(string guild, Point3D at, string why)
    {
        if (string.IsNullOrEmpty(guild))
        {
            return;
        }

        _overrides[guild] = at;
        _moved.Remove(guild);

        logger.Warning("The seat of {Guild} moves to {X},{Y}: {Why}", guild, at.X, at.Y, why);
    }

    /// <summary>
    /// A seat for a guild founded just now, when the file and the store say nothing about it. See <see cref="AbroadShare"/>.
    /// Returns the town it settled by, or null for home.
    /// </summary>
    public static BotTowns.Town Choose(string guild, Map map)
    {
        // <b>With guild houses running, no bare point by a town is chosen any more</b> (29.09.2026, evening: "only in an
        // empty and suitable building"). The name's old wish is forgotten and the settler draws a new one — the same share
        // abroad — and puts the guild into a building of a town with room, or has it save for Britain. See BotGuildHouses.
        if (BotGuildHouses.Running && BotGuildHouses.Started)
        {
            BotGuildHouses.Founded(guild);

            return null;
        }

        if (!Running || string.IsNullOrEmpty(guild) || Of(guild) != Point3D.Zero || map == null || !BotTowns.Ensure(map))
        {
            return null;
        }

        if (Utility.RandomDouble() >= AbroadShare)
        {
            AtHome++;

            return null;
        }

        var home = BotPopulation.Where;
        BotTowns.Town best = null;
        var fewest = int.MaxValue;

        for (var i = 0; i < BotTowns.All.Count; i++)
        {
            var town = BotTowns.All[i];

            if (!town.FromHome || town.Home || town.Bounds.Contains(home))
            {
                continue;
            }

            var seated = 0;

            foreach (var name in Named())
            {
                var seat = Of(name);

                if (seat != Point3D.Zero && Utility.InRange(seat, town.Square, BotTowns.Roam))
                {
                    seated++;
                }
            }

            if (seated < fewest)
            {
                fewest = seated;
                best = town;
            }
        }

        if (best == null)
        {
            AtHome++;

            return null;
        }

        // Somewhere near the square, apart from the seats already by this town, on ground that holds a body.
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var x = best.Square.X + Utility.RandomMinMax(-TownSpread, TownSpread);
            var y = best.Square.Y + Utility.RandomMinMax(-TownSpread, TownSpread);

            if (!BotStep.Settle(map, x, y, out var z))
            {
                continue;
            }

            var at = new Point3D(x, y, z);

            if (TooNear(guild, at, out _, out _))
            {
                continue;
            }

            // Not on ground another guild holds. See BotBounds.
            if (BotBounds.Foreign(guild, map, at, out _))
            {
                BotBounds.SeatRefused();

                continue;
            }

            Set(guild, at, false);
            best.Seated++;
            Abroad++;

            logger.Information("{Guild} settles by {Town}, at {X},{Y}: {Seated} guilds are seated there now", guild, best.Name, x, y, best.Seated);

            return best;
        }

        AtHome++;

        return null;
    }

    /// <summary>Puts a hand-set seat back as it was read off the save. No log, no count: nothing happened.</summary>
    public static void Restore(string guild, Point3D at)
    {
        if (!string.IsNullOrEmpty(guild))
        {
            _overrides[guild] = at;
        }
    }

    /// <summary>The hand-set seats, for the store.</summary>
    public static IEnumerable<KeyValuePair<string, Point3D>> Overrides => _overrides;

    /// <summary>One clause per guild, for Argus and the boot log.</summary>
    public static string Tell()
    {
        using var say = Server.Text.ValueStringBuilder.Create(128);

        foreach (var guild in BotGuilds.Standing)
        {
            var seat = Of(guild.Name);

            if (say.Length > 0)
            {
                say.Append(", ");
            }

            say.Append(guild.Name);
            say.Append(' ');

            if (seat == Point3D.Zero)
            {
                say.Append("has no seat");

                continue;
            }

            say.Append($"at {seat.X},{seat.Y}");

            // Not "by hand": a seat that followed its hall out of a winner's yard is an override too, and the
            // store does not remember which was which. Said so the morning of 14.09.2026, when the Crown's
            // exile read as somebody's order.
            if (BotGuildHouses.Of(guild.Name) is { } house)
            {
                say.Append($" (its house in {house.Town})");
            }
            else if (_overrides.ContainsKey(guild.Name))
            {
                say.Append(" (overriding the file)");
            }

            if (BotEstate.Hall(guild) is { Deleted: false } hall)
            {
                say.Append($", hall {Gap(hall.Location, seat)} off");
            }
        }

        return say.Length == 0 ? "no guilds stand" : say.ToString();
    }

    /// <summary>Seats the road map did not reach from home at the last audit. For the summary.</summary>
    public static int RoadlessSeats { get; private set; }

    /// <summary>
    /// Whether the roads from home do not reach this seat. Only judged once the map is flooded and only for ground
    /// the map covers; anything else is not known to be off the roads.
    /// </summary>
    public static bool Roadless(Point3D seat, out string why)
    {
        why = null;

        var map = BotPopulation.Home;

        if (map == null || seat == Point3D.Zero || !BotRoads.Ready || !BotRoads.Covers(map, seat.X, seat.Y))
        {
            return false;
        }

        if (BotRoads.FromHome(map, seat.X, seat.Y) >= 0)
        {
            return false;
        }

        why = $"no road reaches ({seat.X}, {seat.Y}) from home at ({BotRoads.Home.X}, {BotRoads.Home.Y})";

        return true;
    }

    /// <summary>
    /// Every guild's seat asked of the road map once it is flooded. A seat the roads do not reach is a seat every bot
    /// put down at has to be carried home from; it is warned about, not moved — where a guild lives is Patrick's word
    /// or the file's, and this is the measurement that precedes moving it. Patrick's decision of 16.09.2026, evening:
    /// seats only on ground with a road from home, and the door refuses any other.
    /// </summary>
    public static void Audit()
    {
        RoadlessSeats = 0;

        foreach (var guild in BotGuilds.Standing)
        {
            var seat = Of(guild.Name);

            if (!Roadless(seat, out var why))
            {
                continue;
            }

            RoadlessSeats++;

            logger.Warning(
                "The seat of {Guild} at {X},{Y} is off the roads: {Why}; bots put down there are carried home from it. Move it with \"seat <guild> <x> <y>\" at the door",
                guild.Name,
                seat.X,
                seat.Y,
                why
            );
        }
    }

    /// <summary>Part of the estate's line.</summary>
    public static string Describe() =>
        !Running
            ? "guilds have no seats"
            : $"seats: {Tell()}; a hall is at home within {Settled} tiles of its seat; {Homed} bots put down at their guild's hall or seat, {Carried} halls carried to a seat, {Spoken} seats set by hand, {RoadlessSeats} seats off the roads at the last audit";

    public static void Forget()
    {
        Homed = 0;
        Carried = 0;
        Spoken = 0;
        Abroad = 0;
        AtHome = 0;
        _moved.Clear();
    }

    /// <summary>
    /// A closed guild's seat taken off the books (06.10.2026, <c>BotGuilds.Close</c>), so <see cref="TooNear"/> does not keep
    /// a seat for a guild that no longer stands and refuse a living guild the ground beside it. The file's seat for that
    /// name, if it has one, comes back at the next boot like any configuration.
    /// </summary>
    public static void Drop(string guild)
    {
        if (string.IsNullOrEmpty(guild))
        {
            return;
        }

        _overrides.Remove(guild);
        _configured.Remove(guild);
        _moved.Remove(guild);
    }

    /// <summary>The hand-set seats forgotten; the file's seats stay, being configuration. For the population's reset.</summary>
    public static int Wipe()
    {
        var gone = _overrides.Count;

        _overrides.Clear();
        Forget();

        return gone;
    }
}

/// <summary>
/// Keeps the seats that were set by hand across restarts. Only those: the file is read every start.
/// </summary>
public sealed class BotSeatStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSeatStore));

    private const int Shape = 1;

    private const int Oldest = 1;

    private static BotSeatStore _store;

    public static void Configure() => _store ??= new BotSeatStore();

    public BotSeatStore() : base("BotSeats", 15)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);

        var many = 0;

        foreach (var _ in BotSeat.Overrides)
        {
            many++;
        }

        writer.WriteEncodedInt(many);

        foreach (var (guild, at) in BotSeat.Overrides)
        {
            writer.Write(guild);
            writer.WriteEncodedInt(at.X);
            writer.WriteEncodedInt(at.Y);
            writer.WriteEncodedInt(at.Z);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < Oldest || shape > Shape)
        {
            logger.Warning(
                "The saved seats are shape {Found} and this build reads {Oldest} to {Wanted}; they cannot be read, and the shard will stop on the engine's own prompt until Saves/BotSeats/BotSeats.bin is deleted",
                shape,
                Oldest,
                Shape
            );

            return;
        }

        var many = reader.ReadEncodedInt();

        for (var i = 0; i < many; i++)
        {
            var guild = reader.ReadString();
            var x = reader.ReadEncodedInt();
            var y = reader.ReadEncodedInt();
            var z = reader.ReadEncodedInt();

            BotSeat.Restore(guild, new Point3D(x, y, z));
        }

        if (many > 0)
        {
            logger.Information("{Many} seats set by hand read back off the save", many);
        }
    }
}
