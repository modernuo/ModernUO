using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// What goes inside a hall: a chest, and the guild's own tools of the trade.
///
/// <para>
/// <b>This is the cheapest part of the whole order and the one that pays for itself immediately.</b>
/// <see cref="BotGround"/> already sweeps the ground wherever bots go and remembers forges with an anvil
/// beside them, every fire the engine will cook over, and counters. A forge inside a hall is an ordinary
/// item in the world, so the sweep finds it with no new code at all — the hall becomes a workshop by being
/// built, and the economy picks it up without a line of arithmetic changing.
/// </para>
///
/// <para>
/// <b>The fittings say what the guild is.</b> The Hammer gets a forge and an anvil, because that is what
/// smiths and miners are for; the Needle gets a loom, a wheel and an oven; the Blade and the Crown get a
/// chest and a roof, because a barracks is a place to be rather than a place to make things.
/// </para>
///
/// <para>
/// <b>Inside means inside the walls, and working that out took a chest across a doorway.</b> The first
/// version asked the multi which tiles it covered and treated all of them as the room — but a house's front
/// steps are part of its multi, and <c>BaseHouse.IsInside</c> agrees they are inside it. So the Blade's
/// chest was set down on the porch, in the one tile everybody has to walk through. The room is now found by
/// flooding outwards from the middle of the house and refusing to pass through a doorway: what the flood
/// reaches is the room, what it cannot reach is the world.
/// </para>
/// </summary>
public static class BotFittings
{
    /// <summary>Whether halls are furnished at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// How much clear floor is left around each door. One tile: enough to walk in and turn.
    /// </summary>
    public static int Doorway { get; set; } = 1;

    /// <summary>Fittings moved back indoors by <see cref="Tidy"/>.</summary>
    public static long Moved { get; private set; }

    /// <summary>One thing a guild wants in its hall, and what the guild will have to pay for it.</summary>
    public readonly struct Bench
    {
        public Bench(string name, Type kind, Func<BaseAddon> make, int price)
        {
            Name = name;
            Kind = kind;
            Make = make;
            Price = price;
        }

        /// <summary>What it is called out loud — "a forge", "a loom".</summary>
        public string Name { get; }

        /// <summary>The addon type, which is how "has this hall got one" is answered.</summary>
        public Type Kind { get; }

        public Func<BaseAddon> Make { get; }

        public int Price { get; }
    }

    /// <summary>
    /// What each guild wants in its hall, in the order it wants it, and what each costs.
    ///
    /// <para>
    /// <b>Bought rather than given, by Patrick's order of 08.09.2026 — and the prices are ours, which is
    /// worth saying plainly.</b> This engine has no shop price for a forge or a loom: they are carpentry
    /// crafts (<c>DefCarpentry</c>: a <c>SmallForgeDeed</c> is five logs at 73.6 skill), and nothing on any
    /// vendor's shelf sells one. So there is no engine number to take, unlike every other price on this
    /// shard, and these are set against the one number that is already ours: a hall costs five thousand, and
    /// fitting one out completely comes to about a third of that again.
    /// </para>
    ///
    /// <para>
    /// Named by guild rather than by trade, for the same reason <see cref="BotGuilds"/> reads classes by
    /// name: a guild added later is fitted out by being listed here and nowhere else. A guild that is not
    /// named wants nothing, which is right for the Blade and the Crown — a barracks is a place to be, not a
    /// place to make things.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, Bench[]> _wanted = new()
    {
        ["The Hammer"] =
        [
            new Bench("a forge", typeof(SmallForgeAddon), () => new SmallForgeAddon(), 800),
            new Bench("an anvil", typeof(AnvilEastAddon), () => new AnvilEastAddon(), 500)
        ],
        ["The Needle"] =
        [
            new Bench("an oven", typeof(StoneOvenEastAddon), () => new StoneOvenEastAddon(), 600),
            new Bench("a loom", typeof(LoomEastAddon), () => new LoomEastAddon(), 600),
            new Bench("a spinning wheel", typeof(SpinningWheelEastAddon), () => new SpinningWheelEastAddon(), 400)
        ]
    };

    /// <summary>Benches bought and set up.</summary>
    public static long Bought { get; private set; }

    /// <summary>Coin the guilds have spent on them.</summary>
    public static long Spent { get; private set; }

    /// <summary>
    /// The first thing this guild wants that its hall has not got, or false.
    ///
    /// Asked of the hall's own addon list rather than of a record kept here: the house is the fact, and a
    /// second record of what is in it is a second record to go stale.
    /// </summary>
    /// <summary>How high above the house's own height a tile may start and still be the ground floor's. See Room.</summary>
    public static int GroundHigh { get; set; } = 16;

    /// <summary>The benches of the guild's list this hall has standing, for carrying into a bigger one. See BotEnlarge.</summary>
    public static List<Bench> Installed(BaseHouse hall, Guild guild)
    {
        List<Bench> had = [];

        if (hall is not { Deleted: false } || !_wanted.TryGetValue(guild?.Name ?? string.Empty, out var list))
        {
            return had;
        }

        for (var i = 0; i < list.Length; i++)
        {
            if (Has(hall, list[i].Kind))
            {
                had.Add(list[i]);
            }
        }

        return had;
    }

    /// <summary>
    /// Takes every bench out of a hall before it comes down, so the engine does not leave a deed lying where each stood
    /// (<c>BaseHouse.OnAfterDelete</c> drops one per addon). Returns how many.
    /// </summary>
    public static int Strip(BaseHouse hall)
    {
        if (hall?.Addons == null)
        {
            return 0;
        }

        var taken = 0;

        for (var i = hall.Addons.Count - 1; i >= 0; i--)
        {
            var addon = hall.Addons[i];

            hall.Addons.RemoveAt(i);

            if (addon is { Deleted: false })
            {
                addon.Delete();
                taken++;
            }
        }

        return taken;
    }

    public static bool Wanting(BaseHouse hall, Guild guild, out Bench bench)
    {
        bench = default;

        if (hall is not { Deleted: false } || !_wanted.TryGetValue(guild?.Name ?? string.Empty, out var list))
        {
            return false;
        }

        for (var i = 0; i < list.Length; i++)
        {
            if (!Has(hall, list[i].Kind))
            {
                bench = list[i];

                return true;
            }
        }

        return false;
    }

    /// <summary>Whether this hall already has one of these.</summary>
    private static bool Has(BaseHouse hall, Type kind)
    {
        if (hall.Addons == null)
        {
            return false;
        }

        for (var i = 0; i < hall.Addons.Count; i++)
        {
            if (hall.Addons[i] is { Deleted: false } addon && addon.GetType() == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sets one bench up in a hall that has just paid for it. False if there is nowhere in the room it fits.
    ///
    /// <para>
    /// Where it goes is the engine's decision, not ours: <c>BaseAddon.CouldFit</c> is the same test the addon
    /// deeds use and it knows about walls, other addons and whether the spot is inside a house at all. What
    /// is ours is the order the tiles are offered in — near the last thing set down, because an anvil is only
    /// an anvil to this shard if it is within <c>BotGround.AnvilReach</c> of the fire.
    /// </para>
    /// </summary>
    public static bool Install(BaseHouse hall, Bench bench)
    {
        if (Place(hall, bench.Make?.Invoke()) is not { } addon)
        {
            return false;
        }

        Bought++;
        Spent += bench.Price;

        return true;
    }

    /// <summary>
    /// Sets one addon up in a house of a bot's own, which the bot itself has paid for (PLAN-life A4). The same engine
    /// answer on where it fits as a hall's bench gets; the guild's counters are not touched — a house's furnishing is
    /// counted by <c>BotFurnish</c>. Returns the addon placed, or null when there is nowhere in the room it fits.
    /// </summary>
    public static BaseAddon InstallOwned(BaseHouse house, Func<BaseAddon> make) => Place(house, make?.Invoke());

    /// <summary>
    /// The shared body of <see cref="Install"/> and <see cref="InstallOwned"/>: the addon in hand is set down on the
    /// first free tile of the room the engine accepts, nearest the last thing set down; deleted and null when nowhere fits.
    /// </summary>
    private static BaseAddon Place(BaseHouse hall, BaseAddon addon)
    {
        if (addon == null || hall is not { Deleted: false } || hall.Map == null || hall.Map == Map.Internal)
        {
            addon?.Delete();

            return null;
        }

        var spots = Free(Room(hall), hall.Map);
        var beside = Nearest(hall);

        if (beside != Point3D.Zero)
        {
            spots.Sort((a, b) => a.GetDistanceToSqrt(beside).CompareTo(b.GetDistanceToSqrt(beside)));
        }
        else
        {
            var door = Doorstep(hall);

            spots.Sort((a, b) => b.GetDistanceToSqrt(door).CompareTo(a.GetDistanceToSqrt(door)));
        }

        for (var i = 0; i < spots.Count; i++)
        {
            BaseHouse found = null;

            if (addon.CouldFit(spots[i], hall.Map, hall.Owner, ref found) != AddonFitResult.Valid || found != hall)
            {
                continue;
            }

            addon.MoveToWorld(spots[i], hall.Map);
            hall.Addons ??= [];
            hall.Addons.Add(addon);

            return addon;
        }

        addon.Delete();

        return null;
    }

    /// <summary>
    /// One free tile inside a hall, furthest from the door, or Zero if the room is full.
    ///
    /// Public because two things outside this file need to stand in a room: a bot placing a merchant, and
    /// the merchant itself. Both want the same answer, and it is the one this file already works out.
    /// </summary>
    public static Point3D Spot(BaseHouse hall)
    {
        if (hall is not { Deleted: false } || hall.Map == null || hall.Map == Map.Internal)
        {
            return Point3D.Zero;
        }

        var spare = Free(Room(hall), hall.Map);

        if (spare.Count == 0)
        {
            return Point3D.Zero;
        }

        var door = Doorstep(hall);

        spare.Sort((a, b) => b.GetDistanceToSqrt(door).CompareTo(a.GetDistanceToSqrt(door)));

        return spare[0];
    }

    /// <summary>Merchants moved out of a doorway by <see cref="Unblock"/>.</summary>
    public static long Unblocked { get; private set; }

    /// <summary>Merchants found by a door with nowhere further in to be moved to.</summary>
    public static long Cornered { get; private set; }

    /// <summary>
    /// Moves any merchant standing within <see cref="Doorway"/> tiles of a door to the far end of the room,
    /// and says how many it moved.
    ///
    /// For the merchants hired before 21.09.2026, every one of which was set down on the tile behind the
    /// door. See the note in <c>BotHire</c>. Asked whenever a hall is handed to its guild, which is every
    /// boot, so a hall cannot stay sealed across a restart.
    /// </summary>
    public static int Unblock(BaseHouse hall)
    {
        if (hall is not { Deleted: false } || hall.Map == null || hall.Map == Map.Internal
            || hall.PlayerVendors == null || hall.Doors == null)
        {
            return 0;
        }

        var moved = 0;

        for (var i = 0; i < hall.PlayerVendors.Count; i++)
        {
            if (hall.PlayerVendors[i] is not { Deleted: false } merchant)
            {
                continue;
            }

            // <b>One tile further than the doorway itself, and the first version of this missed by exactly that
            // tile.</b> The room is what the flood reaches, and the flood will not enter the band of Doorway tiles
            // round a door — so the first tile of the room, the one a hirer reaches first and the one that is the
            // neck of a small hall, lies Doorway + 1 from the door. Renaldo stood two tiles from The Hammer's door,
            // the test asked for one, and build 175's first boot moved nobody.
            var nearest = int.MaxValue;
            Item way = null;

            for (var d = 0; d < hall.Doors.Count; d++)
            {
                if (hall.Doors[d] is Item { Deleted: false } door)
                {
                    var gap = Math.Max(Math.Abs(door.X - merchant.X), Math.Abs(door.Y - merchant.Y));

                    if (gap < nearest)
                    {
                        nearest = gap;
                        way = door;
                    }
                }
            }

            if (way == null || nearest > Doorway + 1)
            {
                continue;
            }

            var spot = Spot(hall);

            // Only ever further in. A room so full that the one free tile is nearer the door than where the
            // merchant stands is not improved by moving it there.
            if (spot == Point3D.Zero
                || Math.Max(Math.Abs(way.X - spot.X), Math.Abs(way.Y - spot.Y)) <= nearest)
            {
                Cornered++;

                continue;
            }

            merchant.MoveToWorld(spot, hall.Map);
            moved++;
        }

        Unblocked += moved;

        return moved;
    }

    /// <summary>
    /// Whether this tile is part of the hall's room, as opposed to merely part of its multi.
    ///
    /// <para>
    /// <b>The distinction the porch taught us, asked a second time.</b> <c>BaseHouse.FindHouseAt</c> answers
    /// "yes, this is the house" for a tile on the front steps, so a bot standing in the doorway believes it
    /// is indoors — and the first merchant this shard ever hired was set down there, in the one tile
    /// everybody walks through, exactly as the first chest was. The room is what the flood reaches.
    /// </para>
    /// </summary>
    public static bool InRoom(BaseHouse hall, Point3D at)
    {
        if (hall is not { Deleted: false })
        {
            return false;
        }

        return Within(Room(hall), at);
    }

    /// <summary>Where the last bench went, so the next one goes beside it and a smithy stays one smithy.</summary>
    private static Point3D Nearest(BaseHouse hall)
    {
        if (hall.Addons == null)
        {
            return Point3D.Zero;
        }

        for (var i = hall.Addons.Count - 1; i >= 0; i--)
        {
            if (hall.Addons[i] is { Deleted: false } addon)
            {
                return addon.Location;
            }
        }

        return Point3D.Zero;
    }

    /// <summary>
    /// Puts a chest and the guild's fittings into a hall that has just gone up. Returns how many things
    /// were set down.
    /// </summary>
    public static int Furnish(BaseHouse house, Guild guild)
    {
        if (!Running || house is not { Deleted: false } || house.Map == null || house.Map == Map.Internal)
        {
            return 0;
        }

        var owner = house.Owner;
        var map = house.Map;
        var spots = Free(Room(house), map);

        // The guild is not read here any more — what it wants is bought later, by BotFitter — but it stays
        // in the signature because the caller has it and the next thing to go in a new hall will need it.
        _ = guild;

        if (spots.Count == 0)
        {
            return 0;
        }

        var placed = 0;
        var door = Doorstep(house);

        // <b>A chest and nothing else, and the workshop is bought afterwards.</b> The benches used to come
        // free with the walls; Patrick's order of 08.09.2026 is that a guild pays for its tools separately,
        // one at a time, out of the same levy. See BotFitter, and see _wanted above for what each guild is
        // then saving up for.
        //
        // The chest stays free because it is furniture rather than a tool: a hall with nowhere to put
        // anything is not a hall that can be improved later, it is a room.
        if (spots.Count > 0)
        {
            // Against the far wall. It was put nearest the door at first, on the reasoning that it is the
            // thing a bot walks in to use — which is exactly how a chest ends up in a doorway.
            spots.Sort((a, b) => b.GetDistanceToSqrt(door).CompareTo(a.GetDistanceToSqrt(door)));

            var chest = new WoodenChest();
            chest.MoveToWorld(spots[0], map);

            // Locked down so it is furniture rather than loot: a movable container on a floor is something
            // the engine may decay and something a bot may pick up. If the house will not take the lockdown
            // — no owner, or the count is full — the fallback does the half of it that matters.
            if (owner == null || !house.LockDown(owner, chest))
            {
                chest.Movable = false;
            }

            placed++;
        }

        return placed;
    }

    /// <summary>
    /// Moves anything of ours that is standing outside the room back into it.
    ///
    /// <para>
    /// Called when a hall is adopted from the save, so that a hall furnished by an older, wronger version of
    /// this file repairs itself at the next start rather than having to be razed and rebuilt. It moves; it
    /// never deletes. Whatever is in the chest travels with it.
    /// </para>
    /// </summary>
    public static int Tidy(BaseHouse house)
    {
        if (!Running || house is not { Deleted: false } || house.Map == null || house.Map == Map.Internal)
        {
            return 0;
        }

        var room = Room(house);

        if (room.Count == 0)
        {
            return 0;
        }

        var door = Doorstep(house);

        // <b>Judged against the whole room and moved into the free part of it, and the difference between
        // those two lists is a defect this had for one restart.</b> The room was being asked for with its
        // occupied tiles already filtered out — right for putting something down, wrong for deciding whether
        // something is already indoors, because the thing being judged is standing on the tile that its own
        // presence removed from the list. The chest was therefore declared outside its own hall and moved one
        // tile every time the shard started.
        var spare = Free(room, house.Map);

        spare.Sort((a, b) => b.GetDistanceToSqrt(door).CompareTo(a.GetDistanceToSqrt(door)));

        var stray = new List<Item>();

        if (house.Addons != null)
        {
            for (var i = 0; i < house.Addons.Count; i++)
            {
                if (house.Addons[i] is { Deleted: false } addon && !Within(room, addon.Location))
                {
                    stray.Add(addon);
                }
            }
        }

        if (house.LockDowns != null)
        {
            foreach (var item in house.LockDowns)
            {
                if (item is { Deleted: false } && !Within(room, item.Location))
                {
                    stray.Add(item);
                }
            }
        }

        var moved = 0;

        for (var i = 0; i < stray.Count && spare.Count > 0; i++)
        {
            var to = spare[0];

            spare.RemoveAt(0);
            stray[i].MoveToWorld(to, house.Map);
            moved++;
        }

        // <b>And the merchants, which the sweep above cannot see.</b> A hired shopkeeper is a mobile rather
        // than an item, so it is in none of the house's item lists; the first one hired stood on the porch
        // for the same reason the first chest did, and nothing would ever have moved it.
        if (house.PlayerVendors != null)
        {
            for (var i = 0; i < house.PlayerVendors.Count && spare.Count > 0; i++)
            {
                if (house.PlayerVendors[i] is not { Deleted: false } merchant || Within(room, merchant.Location))
                {
                    continue;
                }

                var to = spare[0];

                spare.RemoveAt(0);
                merchant.MoveToWorld(to, house.Map);
                moved++;
            }
        }

        Moved += moved;

        return moved;
    }

    private static bool Within(List<Point3D> room, Point3D at)
    {
        for (var i = 0; i < room.Count; i++)
        {
            if (room[i].X == at.X && room[i].Y == at.Y)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Where the front door is, so everything else can be put as far from it as the room allows.</summary>
    private static Point3D Doorstep(BaseHouse house) =>
        house.Doors is { Count: > 0 } && house.Doors[0] is Item door ? door.Location : house.Location;

    /// <summary>
    /// The room: every tile the inside of the house is made of, occupied or not.
    ///
    /// <para>
    /// Found by flooding outwards from the middle of the multi across tiles that have a floor and no wall,
    /// and refusing to step onto a doorway. A doorway is the only gap in the walls, so a flood that will not
    /// use one cannot get out — which makes the porch, the steps and the street unreachable without this
    /// file knowing anything about which way the house faces.
    /// </para>
    ///
    /// <para>
    /// The heights come off the multi's own components. A height worked out by arithmetic is a place nothing
    /// can reach, and this project has paid for that twice.
    /// </para>
    /// </summary>
    private static List<Point3D> Room(BaseHouse house)
    {
        var mcl = house.Components;
        var wide = mcl.Width;
        var high = mcl.Height;

        var open = new bool[wide, high];
        var floor = new sbyte[wide, high];

        for (var ix = 0; ix < wide; ix++)
        {
            for (var iy = 0; iy < high; iy++)
            {
                var tiles = mcl.Tiles[ix][iy];
                var found = sbyte.MinValue;
                var blocked = false;

                for (var t = 0; t < tiles.Length; t++)
                {
                    var tile = tiles[t];

                    // <b>The ground floor only.</b> A hall of more than one storey (BotHallKind, 26.09.2026) has floors and
                    // walls above the room; the highest surface of such a column is upstairs, and a wall upstairs is not a
                    // wall down here. The small house has nothing above GroundHigh but a roof that is neither, so its room
                    // is what it always was.
                    if (tile.Z > GroundHigh)
                    {
                        continue;
                    }

                    var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                    if ((data.Flags & TileFlag.Impassable) != 0)
                    {
                        blocked = true;

                        continue;
                    }

                    if ((data.Flags & TileFlag.Surface) != 0)
                    {
                        var top = (sbyte)(tile.Z + data.CalcHeight);

                        if (top > found)
                        {
                            found = top;
                        }
                    }
                }

                open[ix, iy] = !blocked && found != sbyte.MinValue;
                floor[ix, iy] = found;
            }
        }

        // The doorways, and the tiles beside them. A door is an item rather than a multi tile, so nothing
        // above marks it — and it is the one gap the flood must not go through.
        var shut = new bool[wide, high];

        if (house.Doors != null)
        {
            for (var i = 0; i < house.Doors.Count; i++)
            {
                if (house.Doors[i] is not Item door)
                {
                    continue;
                }

                var dx = door.X - house.X - mcl.Min.X;
                var dy = door.Y - house.Y - mcl.Min.Y;

                for (var ox = -Doorway; ox <= Doorway; ox++)
                {
                    for (var oy = -Doorway; oy <= Doorway; oy++)
                    {
                        var x = dx + ox;
                        var y = dy + oy;

                        if (x >= 0 && x < wide && y >= 0 && y < high)
                        {
                            shut[x, y] = true;
                        }
                    }
                }
            }
        }

        var seen = new bool[wide, high];
        var queue = new Queue<(int X, int Y)>();
        var start = Middle(open, wide, high, -mcl.Min.X, -mcl.Min.Y);

        if (start.X < 0)
        {
            return [];
        }

        queue.Enqueue(start);
        seen[start.X, start.Y] = true;

        var room = new List<Point3D>();

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();

            var at = new Point3D(house.X + mcl.Min.X + x, house.Y + mcl.Min.Y + y, house.Z + floor[x, y]);

            if (!shut[x, y])
            {
                room.Add(at);
            }

            // Four ways only. A diagonal between two wall corners is not a way out of a room in this engine
            // and must not be one here either, or the flood leaks into the street through a corner.
            Step(open, seen, shut, queue, x + 1, y, wide, high);
            Step(open, seen, shut, queue, x - 1, y, wide, high);
            Step(open, seen, shut, queue, x, y + 1, wide, high);
            Step(open, seen, shut, queue, x, y - 1, wide, high);
        }

        return room;
    }

    /// <summary>One step of the flood. A doorway is entered by nobody, so the room stays a room.</summary>
    private static void Step(
        bool[,] open, bool[,] seen, bool[,] shut, Queue<(int X, int Y)> queue, int x, int y, int wide, int high
    )
    {
        if (x < 0 || x >= wide || y < 0 || y >= high || seen[x, y] || !open[x, y] || shut[x, y])
        {
            return;
        }

        seen[x, y] = true;
        queue.Enqueue((x, y));
    }

    /// <summary>
    /// Somewhere to start the flood: the middle of the multi, or the nearest open tile to it.
    ///
    /// The middle of a house is inside it for every multi this shard will ever place, but it is not worth
    /// depending on — a house whose centre tile is a staircase would otherwise be furnished with nothing at
    /// all, silently.
    /// </summary>
    private static (int X, int Y) Middle(bool[,] open, int wide, int high, int cx, int cy)
    {
        for (var ring = 0; ring < Math.Max(wide, high); ring++)
        {
            for (var ox = -ring; ox <= ring; ox++)
            {
                for (var oy = -ring; oy <= ring; oy++)
                {
                    var x = cx + ox;
                    var y = cy + oy;

                    if (x >= 0 && x < wide && y >= 0 && y < high && open[x, y])
                    {
                        return (x, y);
                    }
                }
            }
        }

        return (-1, -1);
    }

    /// <summary>
    /// The part of a room nothing is standing on, which is where something new may be put down.
    ///
    /// Separate from the room itself on purpose: see the note in <see cref="Tidy"/>.
    /// </summary>
    private static List<Point3D> Free(List<Point3D> room, Map map)
    {
        var spare = new List<Point3D>(room.Count);

        for (var i = 0; i < room.Count; i++)
        {
            if (!Taken(map, room[i].X, room[i].Y))
            {
                spare.Add(room[i]);
            }
        }

        return spare;
    }

    /// <summary>Whether something already stands on this tile — a fitting put down a moment ago, or loot.</summary>
    private static bool Taken(Map map, int x, int y)
    {
        foreach (var item in map.GetItemsAt(x, y))
        {
            if (item is { Deleted: false })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Part of the estate's line: what the halls have been fitted out with.</summary>
    public static string Fittings() =>
        (Bought == 0
            ? "no benches have been bought"
            : $"{Bought} benches bought for {Spent}gp")
        + $"; {Unblocked} merchants moved out of a doorway and {Cornered} left in one for want of anywhere further in";

    public static void Forget()
    {
        Moved = 0;
        Bought = 0;
        Spent = 0;
    }
}
