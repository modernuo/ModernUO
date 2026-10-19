using System;
using System.Collections.Generic;

namespace Server.Tests.Terrain;

/// <summary>
/// A named scene stamped once into the shared synthetic terrain. Tests that rely on one call
/// <see cref="AssertPresent"/> first, so a missing scene fails instead of passing on flat default ground.
/// </summary>
public sealed class Prefab
{
    private readonly Action<Prefab> _stamp;
    private readonly Func<Prefab, bool> _isPresent;

    public Prefab(string name, Rectangle2D bounds, Point3D anchor, Action<Prefab> stamp, Func<Prefab, bool> isPresent)
    {
        Name = name;
        Bounds = bounds;
        Anchor = anchor;
        _stamp = stamp;
        _isPresent = isPresent;
    }

    public string Name { get; }

    public Rectangle2D Bounds { get; }

    /// <summary>The cell a test starts from, at its standing Z.</summary>
    public Point3D Anchor { get; }

    /// <summary>A second named cell, for scenes with a start and a goal.</summary>
    public Point3D Target { get; init; }

    internal void Stamp() => _stamp(this);

    public void AssertPresent()
    {
        if (!_isPresent(this))
        {
            throw new InvalidOperationException($"Prefab {Name} is not stamped");
        }
    }
}

/// <summary>
/// The shared scenes. Each lives in its own part of the region (256, 256)-(1280, 1280), which no test
/// uses for bespoke terrain, and they never overlap. <see cref="MapEdgeSea"/> is the exception: it has to
/// touch the map edge.
/// </summary>
public static class Prefabs
{
    private static bool _stamped;

    // Sea to the west at Z -5, grass to the east at Z 0. Anchor: the first grass cell east of the water.
    public static Prefab Coastline { get; } = new(
        "Coastline",
        new Rectangle2D(256, 256, 32, 32),
        new Point3D(272, 272, 0),
        p =>
        {
            TestTerrain.FillLand(new Rectangle2D(p.Bounds.X, p.Bounds.Y, 16, 32), TestTiles.SeaWater, -5);
            TestTerrain.FillLand(new Rectangle2D(p.Bounds.X + 16, p.Bounds.Y, 16, 32), TestTiles.Grass, 0);
        },
        p => IsLand(p.Anchor.X, p.Anchor.Y, TestTiles.Grass, 0) && IsLand(p.Anchor.X - 1, p.Anchor.Y, TestTiles.SeaWater, -5)
    );

    // Open water 64 tiles square, ringed by the default land. Anchor: the middle.
    public static Prefab DeepSea { get; } = new(
        "DeepSea",
        new Rectangle2D(320, 256, 64, 64),
        new Point3D(352, 288, -5),
        p => TestTerrain.FillLand(p.Bounds, TestTiles.SeaWater, -5),
        p => IsLand(p.Bounds.X, p.Bounds.Y, TestTiles.SeaWater, -5) &&
             IsLand(p.Bounds.End.X - 1, p.Bounds.End.Y - 1, TestTiles.SeaWater, -5) &&
             IsLand(p.Anchor.X, p.Anchor.Y, TestTiles.SeaWater, -5)
    );

    // Open water against the west edge of the map. Anchor: 13 tiles in from the edge.
    public static Prefab MapEdgeSea { get; } = new(
        "MapEdgeSea",
        new Rectangle2D(0, 176, 48, 48),
        new Point3D(13, 200, -5),
        p => TestTerrain.FillLand(p.Bounds, TestTiles.SeaWater, -5),
        p => IsLand(0, p.Anchor.Y, TestTiles.SeaWater, -5) && IsLand(p.Bounds.End.X - 1, p.Anchor.Y, TestTiles.SeaWater, -5)
    );

    // Two walled buildings on open ground: one with a doorway and a counter inside, one with a window, and a
    // ramp climbing two Z a tile to a walled deck at Z 20 over open ground, so its cells stand at two Zs.
    // Anchor: the ground just outside the first building's doorway.
    public static Prefab Town { get; } = new(
        "Town",
        new Rectangle2D(448, 256, 32, 32),
        new Point3D(455, 271, 0),
        p =>
        {
            int x0 = p.Bounds.X, y0 = p.Bounds.Y;

            WallRing(x0 + 2, y0 + 2, x0 + 13, y0 + 13, 0, x0 + 7, y0 + 13);
            for (var x = x0 + 4; x <= x0 + 10; x++)
            {
                TestTerrain.AddStatic(x, y0 + 6, TestTiles.Counter, 0);
            }

            WallRing(x0 + 24, y0 + 20, x0 + 29, y0 + 25, 0, x0 + 26, y0 + 20);
            TestTerrain.ClearStatics(x0 + 29, y0 + 22);
            TestTerrain.AddStatic(x0 + 29, y0 + 22, TestTiles.Window, 0);

            // The ramp runs north up x0 + 17 from y0 + 20 (Z 2) to y0 + 11 (Z 20), onto the deck.
            for (var i = 0; i < 10; i++)
            {
                TestTerrain.AddStatic(x0 + 17, y0 + 20 - i, TestTiles.Floor, (sbyte)(2 + 2 * i));
            }

            for (var x = x0 + 15; x <= x0 + 20; x++)
            {
                for (var y = y0 + 2; y <= y0 + 10; y++)
                {
                    var edge = x == x0 + 15 || x == x0 + 20 || y == y0 + 2 || y == y0 + 10;
                    var ramp = x == x0 + 17 && y == y0 + 10;
                    TestTerrain.AddStatic(x, y, edge && !ramp ? TestTiles.Wall : TestTiles.Floor, 20);
                }
            }
        },
        p => IsStatic(p.Bounds.X + 2, p.Bounds.Y + 2, TestTiles.Wall, 0) &&
             IsStatic(p.Bounds.X + 17, p.Bounds.Y + 5, TestTiles.Floor, 20)
    );

    // Impassable rock crossed by a T of bridge tiles. Anchor: on the bridge.
    public static Prefab Causeway { get; } = new(
        "Causeway",
        new Rectangle2D(512, 256, 32, 32),
        new Point3D(516, 272, 1),
        p =>
        {
            int x0 = p.Bounds.X, y0 = p.Bounds.Y;
            TestTerrain.FillLand(p.Bounds, TestTiles.RockLand, 0);

            for (var x = x0; x < x0 + 32; x++)
            {
                for (var y = y0 + 15; y <= y0 + 17; y++)
                {
                    TestTerrain.AddStatic(x, y, TestTiles.Bridge, 0);
                }
            }

            for (var y = y0; y < y0 + 15; y++)
            {
                for (var x = x0 + 15; x <= x0 + 17; x++)
                {
                    TestTerrain.AddStatic(x, y, TestTiles.Bridge, 0);
                }
            }
        },
        p => IsLand(p.Bounds.X, p.Bounds.Y, TestTiles.RockLand, 0) && IsStatic(p.Anchor.X, p.Anchor.Y, TestTiles.Bridge, 0)
    );

    // Impassable rock under a square loop of walkway three tiles wide at Z 5, the only ground to stand on.
    // Anchor: on the walkway.
    public static Prefab SewerWalkway { get; } = new(
        "SewerWalkway",
        new Rectangle2D(576, 256, 32, 32),
        new Point3D(579, 271, 5),
        p =>
        {
            int x0 = p.Bounds.X, y0 = p.Bounds.Y;
            TestTerrain.FillLand(p.Bounds, TestTiles.RockLand, 0);

            for (var x = x0 + 2; x <= x0 + 29; x++)
            {
                for (var y = y0 + 2; y <= y0 + 29; y++)
                {
                    var onLoop = x <= x0 + 4 || x >= x0 + 27 || y <= y0 + 4 || y >= y0 + 27;
                    if (onLoop)
                    {
                        TestTerrain.AddStatic(x, y, TestTiles.Floor, 5);
                    }
                }
            }
        },
        p => IsLand(p.Bounds.X, p.Bounds.Y, TestTiles.RockLand, 0) && IsStatic(p.Anchor.X, p.Anchor.Y, TestTiles.Floor, 5)
    );

    // A walled upper floor at Z 20 with an L-shaped counter between the south and north halves; the only way
    // round is past the east end. Anchor: south of the counter. Target: north of it.
    public static Prefab InnUpstairs { get; } = new(
        "InnUpstairs",
        new Rectangle2D(640, 256, 24, 24),
        new Point3D(648, 271, 20),
        p =>
        {
            int x0 = p.Bounds.X, y0 = p.Bounds.Y;

            for (var x = x0; x < x0 + 24; x++)
            {
                for (var y = y0; y < y0 + 24; y++)
                {
                    var edge = x == x0 || x == x0 + 23 || y == y0 || y == y0 + 23;
                    TestTerrain.AddStatic(x, y, edge ? TestTiles.Wall : TestTiles.Floor, 20);
                }
            }

            for (var x = x0 + 1; x <= x0 + 16; x++)
            {
                TestTerrain.AddStatic(x, y0 + 11, TestTiles.Counter, 20);
            }

            for (var y = y0 + 12; y <= y0 + 14; y++)
            {
                TestTerrain.AddStatic(x0 + 16, y, TestTiles.Counter, 20);
            }
        },
        p => IsStatic(p.Bounds.X + 8, p.Bounds.Y + 11, TestTiles.Counter, 20) &&
             IsStatic(p.Anchor.X, p.Anchor.Y, TestTiles.Floor, 20)
    )
    {
        Target = new Point3D(649, 262, 20)
    };

    public static IReadOnlyList<Prefab> All { get; } =
        [Coastline, DeepSea, MapEdgeSea, Town, Causeway, SewerWalkway, InnUpstairs];

    public static Prefab Named(string name)
    {
        for (var i = 0; i < All.Count; i++)
        {
            if (All[i].Name == name)
            {
                return All[i];
            }
        }

        throw new ArgumentException($"No prefab named {name}", nameof(name));
    }

    /// <summary>Stamps every prefab once, then refuses terrain writes outside a <see cref="TestTerrain.Scope"/>.</summary>
    public static void StampAll()
    {
        if (_stamped)
        {
            return;
        }

        for (var i = 0; i < All.Count; i++)
        {
            for (var j = i + 1; j < All.Count; j++)
            {
                if (Overlaps(All[i].Bounds, All[j].Bounds))
                {
                    throw new InvalidOperationException($"Prefabs {All[i].Name} and {All[j].Name} overlap");
                }
            }
        }

        for (var i = 0; i < All.Count; i++)
        {
            All[i].Stamp();
        }

        _stamped = true;
        TestTerrain.Sealed = true;
    }

    // A ring of Wall from (x1, y1) to (x2, y2) inclusive, with one open cell at (gapX, gapY).
    private static void WallRing(int x1, int y1, int x2, int y2, sbyte z, int gapX, int gapY)
    {
        for (var x = x1; x <= x2; x++)
        {
            for (var y = y1; y <= y2; y++)
            {
                var edge = x == x1 || x == x2 || y == y1 || y == y2;
                if (edge && (x != gapX || y != gapY))
                {
                    TestTerrain.AddStatic(x, y, TestTiles.Wall, z);
                }
            }
        }
    }

    private static bool Overlaps(Rectangle2D a, Rectangle2D b) =>
        a.Start.X < b.End.X && b.Start.X < a.End.X && a.Start.Y < b.End.Y && b.Start.Y < a.End.Y;

    // Both facets must hold the tile; a scene on only one of them is not present.
    private static bool IsLand(int x, int y, short id, sbyte z)
    {
        var felucca = Map.Felucca.Tiles.GetLandTile(x, y);
        var trammel = Map.Trammel.Tiles.GetLandTile(x, y);
        return felucca.ID == id && felucca.Z == z && trammel.ID == id && trammel.Z == z;
    }

    private static bool IsStatic(int x, int y, ushort id, sbyte z) =>
        HasStatic(Map.Felucca, x, y, id, z) && HasStatic(Map.Trammel, x, y, id, z);

    private static bool HasStatic(Map map, int x, int y, ushort id, sbyte z)
    {
        var cell = map.Tiles.GetStaticBlock(x >> 3, y >> 3)[x & 7][y & 7];
        for (var i = 0; i < cell.Length; i++)
        {
            if (cell[i].ID == id && cell[i].Z == z)
            {
                return true;
            }
        }

        return false;
    }
}
