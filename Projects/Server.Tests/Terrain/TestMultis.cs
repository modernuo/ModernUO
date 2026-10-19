using System.Collections.Generic;

namespace Server.Tests.Terrain;

/// <summary>
/// Invented footprints for the multi IDs content looks up through <see cref="MultiData"/>
/// (HouseFoundation, BaseBoat and the pathing tests). Only the IDs are real; the shapes are not.
/// Needs tile data loaded first: <see cref="MultiComponentList"/> masks IDs with TileData.MaxItemValue.
/// </summary>
public static class TestMultis
{
    public const int GuildHouse = 0x74;
    public const int Tower = 0x7A;
    public const int SmallBoatNorth = 0x0;

    // MultiComponentList drops every entry after the first whose flags are 0.
    private const TileFlag Visible = TileFlag.Background;

    public static void Seed()
    {
        MultiData.SetComponents(GuildHouse, WalledHouse(3, doorwayOnSouth: true));
        MultiData.SetComponents(Tower, WalledHouse(4, doorwayOnSouth: false));
        MultiData.SetComponents(SmallBoatNorth, Boat());
    }

    // A square of Floor ringed by Wall, (2 * radius + 1) tiles a side, with a one-tile doorway of Floor
    // in the middle of the south wall, or of the east wall.
    private static MultiComponentList WalledHouse(int radius, bool doorwayOnSouth)
    {
        var entries = new List<MultiTileEntry>();
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var edge = x == -radius || x == radius || y == -radius || y == radius;
                var doorway = doorwayOnSouth ? x == 0 && y == radius : x == radius && y == 0;
                var id = edge && !doorway ? TestTiles.Wall : TestTiles.Floor;
                entries.Add(new MultiTileEntry(id, (short)x, (short)y, 0, Visible));
            }
        }

        return new MultiComponentList(entries);
    }

    // A deck of Floor three tiles wide and seven long, with Wall rails down both sides.
    private static MultiComponentList Boat()
    {
        var entries = new List<MultiTileEntry>();
        for (var x = -2; x <= 2; x++)
        {
            for (var y = -3; y <= 3; y++)
            {
                var id = x == -2 || x == 2 ? TestTiles.Wall : TestTiles.Floor;
                entries.Add(new MultiTileEntry(id, (short)x, (short)y, 0, Visible));
            }
        }

        return new MultiComponentList(entries);
    }
}
