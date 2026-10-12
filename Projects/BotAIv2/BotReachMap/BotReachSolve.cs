using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Server.BotAI.V2;

/// <summary>What one tile of the map is to a walker. See <see cref="BotReachMap"/>.</summary>
public enum BotReachClass : byte
{
    /// <summary>Not drawn yet.</summary>
    Unknown = 0,

    /// <summary>Somewhere to stand that a walk from home reaches, over the ground and through the gates.</summary>
    Open = 1,

    /// <summary>Somewhere to stand no walk from home reaches, on land that touches water: a ship could put a walker down there.</summary>
    Isle = 2,

    /// <summary>Somewhere to stand no walk from home reaches and no shore leads to: a walled yard, a roof, a sealed room.</summary>
    Pocket = 3,

    /// <summary>Nowhere to stand, and water: a ship sails it, a walker never.</summary>
    Water = 4,

    /// <summary>Nowhere to stand, and dry: rock, wall, a tree's trunk, the void between the dungeons.</summary>
    Blocked = 5
}

/// <summary>One land no walk from home reaches, for the page and the door.</summary>
public readonly record struct BotReachLand(int Root, int Tiles, int X, int Y, bool Coastal);

/// <summary>A way the solve is told of that the ground does not show: a teleporter or a moongate, from the tile stepped on to where the body is put down.</summary>
public readonly record struct BotReachGate(Point3D From, Point3D To);

/// <summary>What a solve is handed on the loop: the clusters as drawn, the gates and home. Nothing in it is game state.</summary>
public sealed class BotReachSolveInput
{
    public Map Map;

    /// <summary>The map's name, taken on the loop, so the dashboard's threads never touch the map itself.</summary>
    public string MapName;

    public int Width;

    public int Height;

    public int ClustersX;

    public int ClustersY;

    public BotReachCluster[] Clusters;

    public List<BotReachGate> Gates;

    public Point3D Home;

    public int Version;
}

/// <summary>
/// The second half of the reach map: the clusters' regions joined into the whole map, and each tile named by what a walker
/// from home can do with it. Pure arithmetic over the clusters (<see cref="BotReachCluster"/>), which are never changed once
/// made — so it runs on a worker thread when the machine has the cores, and hands back an immutable
/// <see cref="BotReachSnapshot"/> through one volatile reference (CLAUDE.md rules 3 and 10).
///
/// <para>
/// <b>Reachable means reachable from home, in the direction of travel (01.10.2026).</b> Home is where the population lives
/// (<see cref="BotPopulation.Where"/>); the search runs along every step the way it can be taken — a ledge dropped off is ground
/// the walk reaches, the ledge above it is not — and through every gate from the tile stepped on to where the body is put down.
/// A tile a walk from home reaches is <see cref="BotReachClass.Open"/>; everything reachable from an open tile is open too, so a
/// walker standing on open ground can be refused anything that is not, without a search.
/// </para>
/// </summary>
public static class BotReachSolve
{
    /// <summary>How many of the largest lands no walk reaches are kept for the page.</summary>
    public static int LargestKept { get; set; } = 32;

    public static BotReachSnapshot Run(BotReachSolveInput input)
    {
        var started = Stopwatch.GetTimestamp();
        var clusters = input.Clusters;
        var count = clusters.Length;
        var bases = new int[count + 1];

        for (var c = 0; c < count; c++)
        {
            bases[c + 1] = bases[c] + (clusters[c]?.Regions ?? 0);
        }

        var n = bases[count];
        var parent = new int[n];

        for (var i = 0; i < n; i++)
        {
            parent[i] = i;
        }

        // Directed edges, counted then filled: a row of targets per region.
        var degree = new int[n + 1];

        void Each(Action<int, int> edge)
        {
            for (var c = 0; c < count; c++)
            {
                var cluster = clusters[c];

                if (cluster?.Links == null)
                {
                    continue;
                }

                for (var k = 0; k < cluster.Links.Length; k++)
                {
                    var link = cluster.Links[k];
                    var rel = BotReachCluster.LinkRel(link);
                    var other = rel == BotReachCluster.Self ? c : BotReachCluster.Neighbour(c, rel, input.ClustersX, input.ClustersY);

                    if (other < 0 || clusters[other] == null)
                    {
                        continue;
                    }

                    var from = BotReachCluster.LinkFrom(link);
                    var to = BotReachCluster.LinkTo(link);

                    if (from >= cluster.Regions || to >= clusters[other].Regions)
                    {
                        continue;
                    }

                    var ga = bases[c] + from;
                    var gb = bases[other] + to;
                    var ways = BotReachCluster.LinkWays(link);

                    if ((ways & BotReachCluster.Forward) != 0)
                    {
                        edge(ga, gb);
                    }

                    if ((ways & BotReachCluster.Backward) != 0)
                    {
                        edge(gb, ga);
                    }
                }
            }

            for (var g = 0; g < input.Gates.Count; g++)
            {
                var from = RegionAt(input, bases, input.Gates[g].From, 1);
                var to = RegionAt(input, bases, input.Gates[g].To, 2);

                if (from >= 0 && to >= 0)
                {
                    edge(from, to);
                }
            }
        }

        Each((a, _) => degree[a + 1]++);

        for (var i = 0; i < n; i++)
        {
            degree[i + 1] += degree[i];
        }

        var targets = new int[degree[n]];
        var fill = new int[n];

        Each(
            (a, b) =>
            {
                targets[degree[a] + fill[a]++] = b;
                Union(parent, a, b);
            }
        );

        // Reached from home, in the direction of travel.
        var open = new bool[n];
        var queue = new int[Math.Max(1, n)];
        int head = 0, tail = 0;
        var home = RegionAt(input, bases, input.Home, 4);

        if (home >= 0)
        {
            open[home] = true;
            queue[tail++] = home;
        }

        while (head < tail)
        {
            var r = queue[head++];

            for (var e = degree[r]; e < degree[r + 1]; e++)
            {
                var t = targets[e];

                if (!open[t])
                {
                    open[t] = true;
                    queue[tail++] = t;
                }
            }
        }

        // The lands no walk reaches: one per undirected piece, a shore if any of its regions touches water.
        var roots = new int[n];
        var coastal = new bool[n];

        for (var c = 0; c < count; c++)
        {
            var cluster = clusters[c];

            for (var r = 0; cluster != null && r < cluster.Regions; r++)
            {
                var g = bases[c] + r;
                var root = Find(parent, g);

                roots[g] = root;

                if (cluster.Coastal(r))
                {
                    coastal[root] = true;
                }
            }
        }

        var regionClass = new byte[n];

        for (var g = 0; g < n; g++)
        {
            regionClass[g] = (byte)(open[g] ? BotReachClass.Open : coastal[roots[g]] ? BotReachClass.Isle : BotReachClass.Pocket);
        }

        // The tiles.
        var classes = new byte[count][];
        var layerClasses = new int[count][];
        var tiles = new long[6];
        var landTiles = new int[n];
        var landX = new int[n];
        var landY = new int[n];
        var mixed = new List<int>();
        var own = 0;
        var layerScratch = new List<int>();
        Span<byte> strataClass = stackalloc byte[8];
        Span<int> strataZ = stackalloc int[8];

        for (var c = 0; c < count; c++)
        {
            var cluster = clusters[c];

            if (cluster == null)
            {
                classes[c] = _unknown;
                tiles[0] += 256;

                continue;
            }

            var x0 = c % input.ClustersX * 16;
            var y0 = c / input.ClustersX * 16;
            var cells = new byte[256];

            layerScratch.Clear();

            for (var i = 0; i < 256; i++)
            {
                var code = cluster.Cells[i];
                byte k;

                if (code == BotReachCluster.Blocked)
                {
                    k = (byte)BotReachClass.Blocked;
                }
                else if (code == BotReachCluster.Water)
                {
                    k = (byte)BotReachClass.Water;
                }
                else if (code == BotReachCluster.Crowded)
                {
                    k = (byte)BotReachClass.Pocket;
                }
                else
                {
                    var g = bases[c] + code;

                    k = regionClass[g];

                    var root = roots[g];

                    if (k != (byte)BotReachClass.Open)
                    {
                        if (landTiles[root] == 0)
                        {
                            landX[root] = x0 + (i & 15);
                            landY[root] = y0 + (i >> 4);
                        }

                        landTiles[root]++;
                    }
                }

                // A tile with more than one standing place is as good as its best; where its places differ, each is kept with its height.
                var first = cluster.FirstLayer(i);

                if (first >= 0)
                {
                    var m = 0;
                    var differ = false;

                    for (var j = first; j < cluster.Layers.Length && BotReachCluster.LayerCell(cluster.Layers[j]) == i && m < 8; j++)
                    {
                        var region = BotReachCluster.LayerRegion(cluster.Layers[j]);
                        var lk = region >= BotReachCluster.Crowded ? (byte)BotReachClass.Pocket : regionClass[bases[c] + region];

                        strataClass[m] = lk;
                        strataZ[m] = BotReachCluster.LayerZ(cluster.Layers[j]);
                        differ |= m > 0 && lk != strataClass[0];
                        m++;
                    }

                    for (var j = 0; j < m; j++)
                    {
                        if (strataClass[j] < k)
                        {
                            k = strataClass[j];
                        }
                    }

                    if (differ)
                    {
                        for (var j = 0; j < m; j++)
                        {
                            layerScratch.Add(i | ((strataZ[j] + 128) << 8) | (strataClass[j] << 16));
                        }
                    }
                }

                cells[i] = k;
                tiles[k]++;
            }

            classes[c] = SharedClass(cells);

            if (ReferenceEquals(classes[c], cells))
            {
                own++;
            }

            if (layerScratch.Count > 0)
            {
                layerClasses[c] = layerScratch.ToArray();
            }

            if (!ReferenceEquals(classes[c], cells) || !HasOpenBorder(cells))
            {
                continue;
            }

            mixed.Add(c);
        }

        // The largest lands no walk reaches.
        var lands = 0;
        var largest = new List<BotReachLand>();

        for (var g = 0; g < n; g++)
        {
            if (roots[g] != g || landTiles[g] == 0)
            {
                continue;
            }

            lands++;
            largest.Add(new BotReachLand(g, landTiles[g], landX[g], landY[g], coastal[g]));
        }

        largest.Sort(static (a, b) => b.Tiles.CompareTo(a.Tiles));

        if (largest.Count > LargestKept)
        {
            largest.RemoveRange(LargestKept, largest.Count - LargestKept);
        }

        var ms = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

        return new BotReachSnapshot(
            input,
            classes,
            layerClasses,
            bases,
            roots,
            regionClass,
            landTiles,
            tiles,
            lands,
            largest.ToArray(),
            mixed.ToArray(),
            n,
            degree[n],
            home >= 0,
            ms,
            own
        );
    }

    private static readonly byte[] _unknown = new byte[256];

    private static readonly byte[][] _uniform = MakeUniform();

    private static byte[][] MakeUniform()
    {
        var all = new byte[6][];

        for (var k = 0; k < all.Length; k++)
        {
            all[k] = new byte[256];
            Array.Fill(all[k], (byte)k);
        }

        return all;
    }

    private static byte[] SharedClass(byte[] cells)
    {
        var first = cells[0];

        for (var i = 1; i < 256; i++)
        {
            if (cells[i] != first)
            {
                return cells;
            }
        }

        return first < _uniform.Length ? _uniform[first] : cells;
    }

    /// <summary>Whether open ground in a mixed cluster touches ground of another class: where the border tiles to check are.</summary>
    private static bool HasOpenBorder(byte[] cells)
    {
        for (var i = 0; i < 256; i++)
        {
            if (cells[i] != (byte)BotReachClass.Open)
            {
                continue;
            }

            var x = i & 15;
            var y = i >> 4;

            if (x < 15 && cells[i + 1] != cells[i] || y < 15 && cells[i + 16] != cells[i]
                || x > 0 && cells[i - 1] != cells[i] || y > 0 && cells[i - 16] != cells[i])
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The global region standing at a point: the tile's own place nearest its height, or the nearest tile within <paramref name="within"/> with one.</summary>
    private static int RegionAt(BotReachSolveInput input, int[] bases, Point3D at, int within)
    {
        for (var r = 0; r <= within; r++)
        {
            for (var dy = -r; dy <= r; dy++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    var x = at.X + dx;
                    var y = at.Y + dy;

                    if (x < 0 || y < 0 || x >= input.Width || y >= input.Height)
                    {
                        continue;
                    }

                    var c = (y >> 4) * input.ClustersX + (x >> 4);

                    if (input.Clusters[c] is not { } cluster)
                    {
                        continue;
                    }

                    var region = cluster.RegionNear(((y & 15) << 4) | (x & 15), at.Z);

                    if (region >= 0)
                    {
                        return bases[c] + region;
                    }
                }
            }
        }

        return -1;
    }

    private static int Find(int[] parent, int i)
    {
        while (parent[i] != i)
        {
            parent[i] = parent[parent[i]];
            i = parent[i];
        }

        return i;
    }

    private static void Union(int[] parent, int a, int b)
    {
        var ra = Find(parent, a);
        var rb = Find(parent, b);

        if (ra != rb)
        {
            parent[Math.Max(ra, rb)] = Math.Min(ra, rb);
        }
    }
}
