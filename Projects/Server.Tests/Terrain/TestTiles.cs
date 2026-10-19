using System.Collections.Generic;

namespace Server.Tests.Terrain;

public readonly record struct TestItemTile(ushort Id, string Name, TileFlag Flags, byte Height);

public readonly record struct TestLandTile(short Id, string Name, TileFlag Flags);

/// <summary>
/// The invented tiles the generated tiledata.mul holds. Every other entry in the file is zero.
/// IDs are arbitrary and carry no meaning from the client, except the content-referenced land below.
/// </summary>
public static class TestTiles
{
    public const ushort Wall = 0x3000;
    public const ushort Floor = 0x3001;
    public const ushort Counter = 0x3002;
    public const ushort Stair = 0x3003;
    public const ushort Bridge = 0x3004;
    public const ushort Roof = 0x3005;
    public const ushort Door = 0x3006;
    public const ushort Window = 0x3007;
    public const ushort Water = 0x3008;
    public const ushort Rock = 0x3009;
    public const ushort Plain = 0x300A;

    public const short Grass = 0x3000;
    public const short RockLand = 0x3001;
    public const short Dirt = 0x3002;

    // Fishing only treats these land IDs as water (Fishing.cs _waterLandTiles), so they keep their numbers.
    public const short SeaWater = 0xA8;

    public static IReadOnlyList<TestItemTile> Items { get; } =
    [
        new(Wall, "test wall", TileFlag.Wall | TileFlag.Impassable | TileFlag.NoShoot, 20),
        new(Floor, "test floor", TileFlag.Surface | TileFlag.Background, 0),
        new(Counter, "test counter", TileFlag.Surface | TileFlag.Impassable, 4),
        new(Stair, "test stair", TileFlag.Bridge | TileFlag.Surface | TileFlag.StairBack, 5),
        new(Bridge, "test bridge", TileFlag.Bridge | TileFlag.Surface, 2),
        new(Roof, "test roof", TileFlag.Roof | TileFlag.Impassable, 0),
        new(Door, "test door", TileFlag.Door | TileFlag.Impassable | TileFlag.NoShoot, 20),
        new(Window, "test window", TileFlag.Window | TileFlag.Wall | TileFlag.Impassable, 20),
        new(Water, "test water", TileFlag.Wet | TileFlag.Impassable | TileFlag.Surface, 0),
        new(Rock, "test rock", TileFlag.Impassable, 20),
        new(Plain, "test plain", TileFlag.None, 1)
    ];

    public static IReadOnlyList<TestLandTile> Land { get; } =
    [
        new(Grass, "test grass", TileFlag.None),
        new(RockLand, "test rock", TileFlag.Impassable),
        new(Dirt, "test dirt", TileFlag.None),
        new(0xA8, "test water", TileFlag.Wet | TileFlag.Impassable),
        new(0xA9, "test water", TileFlag.Wet | TileFlag.Impassable),
        new(0xAA, "test water", TileFlag.Wet | TileFlag.Impassable),
        new(0xAB, "test water", TileFlag.Wet | TileFlag.Impassable),
        new(0x136, "test water", TileFlag.Wet | TileFlag.Impassable),
        new(0x137, "test water", TileFlag.Wet | TileFlag.Impassable)
    ];
}
