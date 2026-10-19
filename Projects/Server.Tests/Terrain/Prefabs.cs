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

    public Point3D Anchor { get; }

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
/// uses for bespoke terrain, and they never overlap.
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

    public static IReadOnlyList<Prefab> All { get; } = [Coastline];

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

    private static bool Overlaps(Rectangle2D a, Rectangle2D b) =>
        a.Start.X < b.End.X && b.Start.X < a.End.X && a.Start.Y < b.End.Y && b.Start.Y < a.End.Y;

    // Both facets must hold the tile; a scene on only one of them is not present.
    private static bool IsLand(int x, int y, short id, sbyte z)
    {
        var felucca = Map.Felucca.Tiles.GetLandTile(x, y);
        var trammel = Map.Trammel.Tiles.GetLandTile(x, y);
        return felucca.ID == id && felucca.Z == z && trammel.ID == id && trammel.Z == z;
    }
}
