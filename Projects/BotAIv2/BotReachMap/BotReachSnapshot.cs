using System;

namespace Server.BotAI.V2;

/// <summary>
/// The reach map as one solve left it: a class for every tile, and for a tile whose standing places differ (a bridge over a
/// road, a roof over a room) the class of each with its height. Immutable: made on a worker, published through
/// <see cref="BotReachMap"/>'s one volatile reference, read by the loop's walkers and the dashboard's threads alike.
///
/// <para>
/// <b>A byte a tile, shared where a cluster is all one thing.</b> 7168 × 4096 tiles of Felucca are 29 million bytes if every
/// cluster had its own array; the open sea, the solid rock and the open fields are clusters of one class, and those all point at
/// one array per class, so the snapshot holds only the clusters with a border in them.
/// </para>
/// </summary>
public sealed class BotReachSnapshot
{
    public BotReachSnapshot(
        BotReachSolveInput input,
        byte[][] classes,
        int[][] layerClasses,
        int[] bases,
        int[] roots,
        byte[] regionClass,
        int[] landTiles,
        long[] tiles,
        int lands,
        BotReachLand[] largest,
        int[] mixed,
        int regions,
        int edges,
        bool homed,
        double solveMs,
        int ownArrays
    )
    {
        Map = input.Map;
        MapName = input.MapName;
        Width = input.Width;
        Height = input.Height;
        ClustersX = input.ClustersX;
        ClustersY = input.ClustersY;
        Clusters = input.Clusters;
        Home = input.Home;
        Gates = input.Gates.Count;
        Version = input.Version;
        Classes = classes;
        LayerClasses = layerClasses;
        Bases = bases;
        Roots = roots;
        RegionClass = regionClass;
        LandTiles = landTiles;
        Tiles = tiles;
        Lands = lands;
        Largest = largest;
        Mixed = mixed;
        Regions = regions;
        Edges = edges;
        Homed = homed;
        SolveMs = solveMs;
        At = DateTime.UtcNow;
        OwnArrays = ownArrays;
    }

    /// <summary>The map, for the loop's questions only (<see cref="Holds"/>); the dashboard reads <see cref="MapName"/>.</summary>
    public Map Map { get; }

    public string MapName { get; }

    public int Width { get; }

    public int Height { get; }

    public int ClustersX { get; }

    public int ClustersY { get; }

    /// <summary>The clusters it was solved from; never changed.</summary>
    public BotReachCluster[] Clusters { get; }

    public Point3D Home { get; }

    public int Gates { get; }

    public int Version { get; }

    public byte[][] Classes { get; }

    public int[][] LayerClasses { get; }

    public int[] Bases { get; }

    public int[] Roots { get; }

    public byte[] RegionClass { get; }

    public int[] LandTiles { get; }

    /// <summary>Tiles of each class, by <see cref="BotReachClass"/>.</summary>
    public long[] Tiles { get; }

    /// <summary>Lands no walk from home reaches.</summary>
    public int Lands { get; }

    public BotReachLand[] Largest { get; }

    /// <summary>Clusters where open ground borders ground of another class: where the check samples.</summary>
    public int[] Mixed { get; }

    public int Regions { get; }

    public int Edges { get; }

    /// <summary>Whether home stood on the map: without it nothing is open.</summary>
    public bool Homed { get; }

    public double SolveMs { get; }

    public DateTime At { get; }

    /// <summary>Clusters holding an array of their own: the ones with a border in them. The rest share one per class.</summary>
    public int OwnArrays { get; }

    public bool Holds(Map map) => map != null && map == Map;

    public bool Inside(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

    /// <summary>What a tile is, by its best standing place. <see cref="BotReachClass.Blocked"/> off the map.</summary>
    public BotReachClass ClassAt(int x, int y)
    {
        if (!Inside(x, y))
        {
            return BotReachClass.Blocked;
        }

        return (BotReachClass)Classes[(y >> 4) * ClustersX + (x >> 4)][((y & 15) << 4) | (x & 15)];
    }

    /// <summary>
    /// What a tile is at a height: where its standing places differ, the one nearest <paramref name="z"/>; elsewhere the tile's
    /// own class.
    /// </summary>
    public BotReachClass ClassAt(int x, int y, int z)
    {
        if (!Inside(x, y))
        {
            return BotReachClass.Blocked;
        }

        var c = (y >> 4) * ClustersX + (x >> 4);
        var cell = ((y & 15) << 4) | (x & 15);
        var layers = LayerClasses[c];

        if (layers == null)
        {
            return (BotReachClass)Classes[c][cell];
        }

        var lo = 0;
        var hi = layers.Length - 1;

        while (lo < hi)
        {
            var mid = (lo + hi) >> 1;

            if ((layers[mid] & 0xFF) < cell)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        if (lo >= layers.Length || (layers[lo] & 0xFF) != cell)
        {
            return (BotReachClass)Classes[c][cell];
        }

        var best = (BotReachClass)Classes[c][cell];
        var gap = int.MaxValue;

        for (var i = lo; i < layers.Length && (layers[i] & 0xFF) == cell; i++)
        {
            var g = Math.Abs(((layers[i] >> 8) & 0xFF) - 128 - z);

            if (g < gap)
            {
                gap = g;
                best = (BotReachClass)((layers[i] >> 16) & 0xFF);
            }
        }

        return best;
    }

    /// <summary>Whether any tile within <paramref name="reach"/> of (x, y) is of class <paramref name="want"/>.</summary>
    public bool Any(int x, int y, int reach, BotReachClass want)
    {
        var w = (byte)want;

        for (var dy = -reach; dy <= reach; dy++)
        {
            var ty = y + dy;

            if ((uint)ty >= (uint)Height)
            {
                continue;
            }

            var row = (ty >> 4) * ClustersX;
            var cy = (ty & 15) << 4;

            for (var dx = -reach; dx <= reach; dx++)
            {
                var tx = x + dx;

                if ((uint)tx < (uint)Width && Classes[row + (tx >> 4)][cy | (tx & 15)] == w)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>The land a tile's lowest standing place belongs to, its size in tiles and class; -1 where it has none.</summary>
    public int LandAt(int x, int y, out int tiles, out BotReachClass kind)
    {
        tiles = 0;
        kind = ClassAt(x, y);

        if (!Inside(x, y))
        {
            return -1;
        }

        var c = (y >> 4) * ClustersX + (x >> 4);

        if (Clusters[c] is not { } cluster)
        {
            return -1;
        }

        var code = cluster.Cells[((y & 15) << 4) | (x & 15)];

        if (code >= BotReachCluster.Crowded)
        {
            return -1;
        }

        var g = Bases[c] + code;
        var root = Roots[g];

        tiles = LandTiles[root];

        return root;
    }

    /// <summary>
    /// The classes of a rectangle, one byte a tile or, at a <paramref name="scale"/> above one, one byte a square of that side:
    /// the most telling class in it — ground no walk reaches over open ground over water over rock — so a pocket two tiles
    /// wide still shows on a map drawn four tiles a pixel. For the dashboard's picture.
    /// </summary>
    public byte[] Picture(int x0, int y0, int columns, int rows, int scale)
    {
        var bytes = new byte[columns * rows];
        Span<int> seen = stackalloc int[6];

        for (var py = 0; py < rows; py++)
        {
            for (var px = 0; px < columns; px++)
            {
                var tx = x0 + px * scale;
                var ty = y0 + py * scale;

                if (scale == 1)
                {
                    bytes[py * columns + px] = Inside(tx, ty) ? Classes[(ty >> 4) * ClustersX + (tx >> 4)][((ty & 15) << 4) | (tx & 15)] : (byte)0;

                    continue;
                }

                seen.Clear();

                for (var dy = 0; dy < scale; dy++)
                {
                    for (var dx = 0; dx < scale; dx++)
                    {
                        var x = tx + dx;
                        var y = ty + dy;

                        if (Inside(x, y))
                        {
                            seen[Classes[(y >> 4) * ClustersX + (x >> 4)][((y & 15) << 4) | (x & 15)]]++;
                        }
                    }
                }

                // Pocket and isle first, from an eighth of the square: they are what the layer is for, and a single roof tile
                // on a picture of four tiles a pixel paints every town pink. Then open ground, water, rock.
                var few = Math.Max(1, scale * scale / 8);

                bytes[py * columns + px] = seen[(int)BotReachClass.Pocket] >= few ? (byte)BotReachClass.Pocket
                    : seen[(int)BotReachClass.Isle] >= few ? (byte)BotReachClass.Isle
                    : seen[(int)BotReachClass.Open] * 2 >= scale * scale ? (byte)BotReachClass.Open
                    : seen[(int)BotReachClass.Water] >= seen[(int)BotReachClass.Blocked] && seen[(int)BotReachClass.Water] > 0 ? (byte)BotReachClass.Water
                    : seen[(int)BotReachClass.Blocked] > 0 ? (byte)BotReachClass.Blocked
                    : seen[(int)BotReachClass.Open] > 0 ? (byte)BotReachClass.Open
                    : (byte)BotReachClass.Unknown;
            }
        }

        return bytes;
    }
}
