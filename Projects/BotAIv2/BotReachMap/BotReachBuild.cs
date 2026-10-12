using System;
using System.Collections.Generic;
using Server.Engines.Pathing.Cache;
using Server.Engines.Pathing.Tiered;
using Server.Items;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// The ground the reach map is surveyed from: the navigation graph's own probe (<see cref="StepProbe"/>: land, statics and the
/// houses standing on it), with two differences, both measured against what a bot's walker does.
///
/// <para>
/// <b>Boats are left out (01.10.2026).</b> A ship is a multi, and a multi's deck is a standing surface to the probe; a map drawn
/// with the ships in it would carry little islands of "land" across the harbours that sail away a minute later, and the water
/// under them would read as ground — the one place Patrick's rule says must stay water ("on the water a boat lifts the
/// unreachability"). Houses are kept: a guild hall's walls are ground for as long as it stands, and a change to one is announced
/// (<see cref="NavigationService.GroundChangedHook"/>).
/// </para>
///
/// <para>
/// <b>Immovable items stand in the way (01.10.2026).</b> The world is decorated with items — the graveyard's railing, the
/// shops' counters, the forges and looms of a hall — and the walker refuses a step into any of them (<see cref="BotStep.BlockedByItems"/>),
/// while the graph never sees them. A standing height an immovable, impassable item fills is taken away here, so a railing is
/// a wall on this map as it is to the walker. Doors, teleporters and moongates are not in the way, as they are not to the
/// walker. What an item covers is written into the cluster's <see cref="BotReachCluster.Signature"/>, so a boot notices a
/// decoration moved since the map was drawn and draws that cluster again.
/// </para>
/// </summary>
public sealed class BotReachTerrain : INavTerrain
{
    private const int Side = NavWindow.Side;

    private const int PersonHeight = 16;

    private readonly Map _map;

    private int _ox;

    private int _oy;

    private bool _housed;

    /// <summary>The impassable immovable items over each cell of the window: heights and tops, packed two a short.</summary>
    private readonly List<int>[] _items = new List<int>[Side * Side];

    private readonly List<int> _touched = [];

    public BotReachTerrain(Map map) => _map = map;

    public Map Map => _map;

    public int Width => _map.Width;

    public int Height => _map.Height;

    /// <summary>Whether the window was read with the houses in it.</summary>
    public bool Housed => _housed;

    /// <summary>Items read into windows so far, for the cost of the survey.</summary>
    public long ItemsRead { get; private set; }

    /// <summary>
    /// Reads what the window round the cluster at (<paramref name="x0"/>, <paramref name="y0"/>) holds that the tiles do not: the
    /// houses near it and the items standing in it. Returns the signature of both.
    /// </summary>
    public long Prepare(int x0, int y0)
    {
        for (var i = 0; i < _touched.Count; i++)
        {
            _items[_touched[i]].Clear();
        }

        _touched.Clear();
        _ox = x0 - 1;
        _oy = y0 - 1;
        _housed = false;

        var sx = x0 >> Map.SectorShift;
        var sy = y0 >> Map.SectorShift;

        for (var dy = -1; dy <= 1 && !_housed; dy++)
        {
            for (var dx = -1; dx <= 1 && !_housed; dx++)
            {
                if (!_map.GetRealSector(sx + dx, sy + dy).HasMultis)
                {
                    continue;
                }

                foreach (var multi in _map.GetMultisInSector((sx + dx) << Map.SectorShift, (sy + dy) << Map.SectorShift))
                {
                    if (multi is not BaseBoat)
                    {
                        _housed = true;

                        break;
                    }
                }
            }
        }

        long signature = _housed ? 0x5A17 : 0;

        foreach (var item in _map.GetItemsInBounds(new Rectangle2D(_ox, _oy, Side, Side)))
        {
            if (!Blocks(item))
            {
                continue;
            }

            var cx = item.X - _ox;
            var cy = item.Y - _oy;

            if ((uint)cx >= Side || (uint)cy >= Side)
            {
                continue;
            }

            var cell = cy * Side + cx;
            var list = _items[cell] ??= [];

            if (list.Count == 0)
            {
                _touched.Add(cell);
            }

            var top = item.Z + item.ItemData.CalcHeight;

            list.Add((item.Z & 0xFFFF) | (top << 16));
            ItemsRead++;

            // Order-free: a sector hands its items back in no promised order.
            signature += Mix(item.X, item.Y, item.Z, item.ItemID);
        }

        return signature;
    }

    /// <summary>Whether an item fills the ground it stands on for a walker: the walker's own test (<see cref="BotStep.BlockedByItems"/>), immovable things only.</summary>
    public static bool Blocks(Item item)
    {
        if (item == null || item.Deleted || item.Movable || item.Parent != null || item is BaseMulti or Teleporter or PublicMoongate or Moongate)
        {
            return false;
        }

        var data = item.ItemData;

        if (!data.ImpassableSurface)
        {
            return false;
        }

        var id = item.ItemID & TileData.MaxItemValue;

        return !(data.Door || id is 0x692 or 0x846 or 0x873 || id is >= 0x6F5 and <= 0x6F6);
    }

    private static long Mix(int x, int y, int z, int id)
    {
        var h = (ulong)(uint)x * 0x9E3779B97F4A7C15UL;

        h ^= (ulong)(uint)y * 0xC2B2AE3D27D4EB4FUL;
        h ^= (ulong)(uint)(z + 128) * 0x165667B19E3779F9UL;
        h ^= (ulong)(uint)id * 0x27D4EB2F165667C5UL;
        h ^= h >> 29;

        return (long)h;
    }

    public int Surfaces(int x, int y, Span<sbyte> zs)
    {
        var n = _housed
            ? StepProbe.ComputeStandableSurfaceZsWithMultis(_map, x, y, zs)
            : StepProbe.ComputeStandableSurfaceZs(_map, x, y, zs);

        var cx = x - _ox;
        var cy = y - _oy;

        if (n == 0 || (uint)cx >= Side || (uint)cy >= Side || _items[cy * Side + cx] is not { Count: > 0 } items)
        {
            return n;
        }

        var kept = 0;

        for (var i = 0; i < n; i++)
        {
            var z = zs[i];
            var blocked = false;

            for (var k = 0; k < items.Count; k++)
            {
                var itemZ = (short)(items[k] & 0xFFFF);
                var itemTop = items[k] >> 16;

                if (itemTop > z && z + PersonHeight > itemZ)
                {
                    blocked = true;

                    break;
                }
            }

            if (!blocked)
            {
                zs[kept++] = z;
            }
        }

        return kept;
    }

    public StepMask Step(int x, int y, sbyte z) =>
        _housed ? StepProbe.ComputeMultiMaskAt(_map, x, y, z) : StepProbe.ComputeMaskAt(_map, x, y, z);

    /// <summary>
    /// Whether a tile's own land is water: the sea, a lake, a river — water a ship could be on. A moat or a fountain is a wet
    /// static over dry land, and a yard beside one is not a shore (01.10.2026: Britain's gardens read as isles by their pools).
    /// </summary>
    public bool WetLand(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _map.Width || y >= _map.Height)
        {
            return false;
        }

        var land = _map.Tiles.GetLandTile(x, y);

        return !land.Ignored && (TileData.LandTable[land.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0;
    }

    /// <summary>Whether a tile with nowhere to stand is water: land that is wet, or a wet static (a moat, a fountain, the sea under a pier).</summary>
    public bool Wet(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _map.Width || y >= _map.Height)
        {
            return false;
        }

        var land = _map.Tiles.GetLandTile(x, y);

        if (!land.Ignored && (TileData.LandTable[land.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
        {
            return true;
        }

        foreach (var tile in _map.Tiles.GetStaticTiles(x, y))
        {
            if ((TileData.ItemTable[tile.ID & TileData.MaxItemValue].Flags & TileFlag.Wet) != 0)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// One cluster of the reach map as the survey left it: which standing place of each of its 256 tiles belongs to which of its
/// own regions, and how its regions are joined to each other and to the regions of the four clusters east, south, south-east
/// and south-west of it. Never changed once made — a cluster drawn again is a new one — so a snapshot built from these can be
/// read from any thread.
///
/// <para>
/// <b>A region</b> is the ground inside the cluster a body can walk round in both directions without leaving it: steps that go
/// both ways join, a step one way only (a drop off a ledge) is a <see cref="Links">link</see> from one region to another. That
/// keeps "a ledge you can drop off and never climb" out of the ground below it when the solve asks what can be walked
/// <em>to</em> from home.
/// </para>
/// </summary>
public sealed class BotReachCluster
{
    /// <summary>A tile nothing can stand on and no ship floats on: rock, wall, tree trunk, the void.</summary>
    public const byte Blocked = 0xFF;

    /// <summary>A tile nothing can stand on that is water.</summary>
    public const byte Water = 0xFE;

    /// <summary>Standing ground in a region past the most one cluster may number: kept as a sealed pocket.</summary>
    public const byte Crowded = 0xFD;

    /// <summary>Regions one cluster may number; past it the ground is <see cref="Crowded"/>.</summary>
    public const int MostRegions = 0xFD;

    /// <summary>Neighbours a link may point into.</summary>
    public const int Self = 0;

    public const int East = 1;

    public const int South = 2;

    public const int SouthEast = 3;

    public const int SouthWest = 4;

    /// <summary>A link one way: from this cluster's region into the other's.</summary>
    public const int Forward = 1;

    /// <summary>A link the other way: from the other's region into this cluster's.</summary>
    public const int Backward = 2;

    /// <summary>For each tile: the region of its lowest standing place, or <see cref="Blocked"/>, <see cref="Water"/>, <see cref="Crowded"/>.</summary>
    public byte[] Cells;

    /// <summary>
    /// Every standing place of every tile that has more than one, in tile order and lowest first: tile (8 bits), stratum (2),
    /// region (8) and height plus 128 (8), packed. Null when no tile has two.
    /// </summary>
    public int[] Layers;

    /// <summary>Regions numbered.</summary>
    public int Regions;

    /// <summary>Regions with water beside them, a bit each; null for none.</summary>
    public ulong[] Coast;

    /// <summary>
    /// Links out of this cluster's regions: region (8 bits), neighbour (3), the neighbour's region (8), and the directions (2)
    /// packed. See <see cref="Forward"/>, <see cref="Backward"/>.
    /// </summary>
    public int[] Links;

    /// <summary>What the houses and items round it read when it was drawn. See <see cref="BotReachTerrain.Prepare"/>.</summary>
    public long Signature;

    /// <summary>Whether it was drawn with a house in or beside it.</summary>
    public bool Housed;

    /// <summary>Tiles with somewhere to stand.</summary>
    public int Standing;

    public static int LinkFrom(int link) => link & 0xFF;

    public static int LinkRel(int link) => (link >> 8) & 7;

    public static int LinkTo(int link) => (link >> 11) & 0xFF;

    public static int LinkWays(int link) => (link >> 19) & 3;

    public static int LayerCell(int layer) => layer & 0xFF;

    public static int LayerStratum(int layer) => (layer >> 8) & 3;

    public static int LayerRegion(int layer) => (layer >> 10) & 0xFF;

    public static int LayerZ(int layer) => ((layer >> 18) & 0xFF) - 128;

    /// <summary>The region of stratum <paramref name="stratum"/> of tile <paramref name="cell"/>, or -1.</summary>
    public int RegionOf(int cell, int stratum)
    {
        if (stratum == 0 || Layers == null)
        {
            var code = Cells[cell];

            return stratum == 0 && code < Crowded ? code : -1;
        }

        var i = FirstLayer(cell);

        for (; i >= 0 && i < Layers.Length && LayerCell(Layers[i]) == cell; i++)
        {
            if (LayerStratum(Layers[i]) == stratum)
            {
                var region = LayerRegion(Layers[i]);

                return region < Crowded ? region : -1;
            }
        }

        return -1;
    }

    /// <summary>The region of the standing place of tile <paramref name="cell"/> nearest height <paramref name="z"/>, or -1.</summary>
    public int RegionNear(int cell, int z)
    {
        if (Layers != null)
        {
            var i = FirstLayer(cell);

            if (i >= 0)
            {
                var best = -1;
                var gap = int.MaxValue;

                for (; i < Layers.Length && LayerCell(Layers[i]) == cell; i++)
                {
                    var g = Math.Abs(LayerZ(Layers[i]) - z);

                    if (g < gap)
                    {
                        gap = g;
                        best = LayerRegion(Layers[i]);
                    }
                }

                return best < Crowded ? best : -1;
            }
        }

        var code = Cells[cell];

        return code < Crowded ? code : -1;
    }

    /// <summary>The index of the first layer of a tile, or -1 when it has none.</summary>
    public int FirstLayer(int cell)
    {
        if (Layers == null)
        {
            return -1;
        }

        int lo = 0, hi = Layers.Length - 1;

        while (lo < hi)
        {
            var mid = (lo + hi) >> 1;

            if (LayerCell(Layers[mid]) < cell)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo < Layers.Length && LayerCell(Layers[lo]) == cell ? lo : -1;
    }

    public bool Coastal(int region) => Coast != null && (Coast[region >> 6] & (1UL << (region & 63))) != 0;

    /// <summary>The cluster a neighbour index points to, or -1 off the map.</summary>
    public static int Neighbour(int c, int rel, int clustersX, int clustersY)
    {
        var cx = c % clustersX;
        var cy = c / clustersX;

        (cx, cy) = rel switch
        {
            East => (cx + 1, cy),
            South => (cx, cy + 1),
            SouthEast => (cx + 1, cy + 1),
            SouthWest => (cx - 1, cy + 1),
            _ => (cx, cy)
        };

        return cx < 0 || cy < 0 || cx >= clustersX || cy >= clustersY ? -1 : cy * clustersX + cx;
    }

    private static readonly byte[] _allBlocked = Filled(Blocked);

    private static readonly byte[] _allWater = Filled(Water);

    private static readonly byte[] _allFirst = Filled(0);

    private static byte[] Filled(byte value)
    {
        var cells = new byte[256];

        Array.Fill(cells, value);

        return cells;
    }

    /// <summary>One array for every cluster whose 256 tiles are all the same, so the open sea costs nothing.</summary>
    public static byte[] Shared(byte[] cells)
    {
        var first = cells[0];

        for (var i = 1; i < cells.Length; i++)
        {
            if (cells[i] != first)
            {
                return cells;
            }
        }

        return first switch
        {
            Blocked => _allBlocked,
            Water => _allWater,
            0 => _allFirst,
            _ => cells
        };
    }
}

/// <summary>
/// Draws one cluster of the reach map from the ground (<see cref="BotReachTerrain"/>) through the navigation graph's own window
/// (<see cref="NavWindow"/>), so the two cannot disagree about a step. On the game loop only: it reads the map's tiles.
///
/// <para>
/// <b>Clusters are drawn from the south-east corner of the map backwards (01.10.2026).</b> A cluster's links reach into its
/// east, south, south-east and south-west neighbours (every pair of touching clusters is joined by exactly one of the two), and
/// those are drawn before it, so a link names the neighbour's region at once and the solve has nothing left to match up.
/// </para>
/// </summary>
public sealed class BotReachBuilder
{
    private const int Cells = NavWindow.ClusterSide * NavWindow.ClusterSide;

    private readonly NavWindow _w = new();

    private readonly BotReachTerrain _terrain;

    private readonly int _clustersX;

    private readonly int _clustersY;

    private readonly int[] _parent = new int[Cells * NavWindow.MaxStrata];

    private readonly int[] _region = new int[Cells * NavWindow.MaxStrata];

    private readonly int[] _count = new int[Cells];

    private readonly List<(int A, int B)> _oneWay = [];

    private readonly Dictionary<int, int> _links = new();

    private readonly List<int> _layers = [];


    public BotReachBuilder(Map map)
    {
        _terrain = new BotReachTerrain(map);
        _clustersX = (map.Width + NavWindow.ClusterSide - 1) / NavWindow.ClusterSide;
        _clustersY = (map.Height + NavWindow.ClusterSide - 1) / NavWindow.ClusterSide;
    }

    public int ClustersX => _clustersX;

    public int ClustersY => _clustersY;

    public BotReachTerrain Terrain => _terrain;

    /// <summary>Cells probed through the window, for the cost of the survey.</summary>
    public long Probes => _w.Probes;

    /// <summary>The houses and items round a cluster now, without drawing it: what a boot compares a kept cluster with.</summary>
    public long Signature(int c) => _terrain.Prepare(c % _clustersX * NavWindow.ClusterSide, c / _clustersX * NavWindow.ClusterSide);

    /// <summary>Draws cluster <paramref name="c"/>. The clusters east, south, south-east and south-west of it must be drawn already.</summary>
    public BotReachCluster Build(int c, BotReachCluster[] drawn)
    {
        var x0 = c % _clustersX * NavWindow.ClusterSide;
        var y0 = c / _clustersX * NavWindow.ClusterSide;
        var signature = _terrain.Prepare(x0, y0);

        _w.Reset(_terrain, c, x0, y0);

        var total = 0;

        for (var i = 0; i < Cells; i++)
        {
            _count[i] = _w.Count(x0 + (i & 15), y0 + (i >> 4));
            total += _count[i];
        }

        var cluster = new BotReachCluster { Signature = signature, Housed = _terrain.Housed };
        var cells = new byte[Cells];

        if (total == 0)
        {
            for (var i = 0; i < Cells; i++)
            {
                cells[i] = _terrain.Wet(x0 + (i & 15), y0 + (i >> 4)) ? BotReachCluster.Water : BotReachCluster.Blocked;
            }

            cluster.Cells = BotReachCluster.Shared(cells);
            cluster.Links = [];

            return cluster;
        }

        // Regions: steps that go both ways join two standing places into one.
        for (var s = 0; s < _parent.Length; s++)
        {
            _parent[s] = s;
        }

        _oneWay.Clear();

        for (var i = 0; i < Cells; i++)
        {
            var x = x0 + (i & 15);
            var y = y0 + (i >> 4);

            for (var s = 0; s < _count[i]; s++)
            {
                var a = i * NavWindow.MaxStrata + s;

                for (var d = 0; d < 8; d++)
                {
                    if (!StepOk(x, y, s, d, out var nx, out var ny, out var ns) || !_w.InCluster(nx, ny))
                    {
                        continue;
                    }

                    var b = ((ny - y0) << 4 | (nx - x0)) * NavWindow.MaxStrata + ns;

                    if (StepOk(nx, ny, ns, (d + 4) & 7, out var bx, out var by, out var bs) && bx == x && by == y && bs == s)
                    {
                        Union(a, b);
                    }
                    else
                    {
                        _oneWay.Add((a, b));
                    }
                }
            }
        }

        // Numbered in tile order, so a cluster drawn twice from the same ground numbers its regions the same way.
        var regions = 0;

        Array.Fill(_region, -1);

        for (var i = 0; i < Cells; i++)
        {
            for (var s = 0; s < _count[i]; s++)
            {
                var root = Find(i * NavWindow.MaxStrata + s);

                if (_region[root] < 0)
                {
                    _region[root] = regions < BotReachCluster.MostRegions ? regions++ : BotReachCluster.Crowded;
                }

                _region[i * NavWindow.MaxStrata + s] = _region[root];
            }
        }

        _layers.Clear();

        var standing = 0;

        for (var i = 0; i < Cells; i++)
        {
            var n = _count[i];

            if (n == 0)
            {
                cells[i] = _terrain.Wet(x0 + (i & 15), y0 + (i >> 4)) ? BotReachCluster.Water : BotReachCluster.Blocked;

                continue;
            }

            standing++;
            cells[i] = (byte)_region[i * NavWindow.MaxStrata];

            if (n < 2)
            {
                continue;
            }

            for (var s = 0; s < n; s++)
            {
                var z = _w.Z(x0 + (i & 15), y0 + (i >> 4), s);

                _layers.Add(i | (s << 8) | (_region[i * NavWindow.MaxStrata + s] << 10) | ((z + 128) << 18));
            }
        }

        cluster.Cells = BotReachCluster.Shared(cells);
        cluster.Layers = _layers.Count > 0 ? _layers.ToArray() : null;
        cluster.Regions = regions;
        cluster.Standing = standing;

        // <b>A shore: the land's own water within <see cref="ShoreReach"/> tiles (01.10.2026).</b> The land's water — the sea, a
        // lake, a river — and not a fountain's or a moat's, which are statics: Britain's walled gardens read as isles by their
        // pools. Within three and not beside: Nujel'm's beaches meet the sea through a band of surf no body stands on, and read
        // as a sealed pocket when only the tile beside was asked. The water is read from the land tiles alone, the window
        // widened by the reach and swept both ways, so it costs a few hundred lookups a cluster.
        cluster.Coast = Shore(x0, y0);

        // Links: one-way steps inside, and every step across the east, south, south-east and south-west edges, both ways.
        _links.Clear();

        for (var k = 0; k < _oneWay.Count; k++)
        {
            var ra = _region[_oneWay[k].A];
            var rb = _region[_oneWay[k].B];

            if (ra != rb && ra < BotReachCluster.Crowded && rb < BotReachCluster.Crowded)
            {
                Link(ra, BotReachCluster.Self, rb, BotReachCluster.Forward);
            }
        }

        for (var i = 0; i < Cells; i++)
        {
            var ix = i & 15;
            var iy = i >> 4;

            // Only the tiles on the east or south edge, or the west edge's south corner, touch the four neighbours joined here.
            if (ix != 15 && iy != 15 && !(ix == 0 && iy == 15))
            {
                continue;
            }

            var x = x0 + ix;
            var y = y0 + iy;

            for (var s = 0; s < _count[i]; s++)
            {
                var ra = _region[i * NavWindow.MaxStrata + s];

                if (ra >= BotReachCluster.Crowded)
                {
                    continue;
                }

                for (var d = 0; d < 8; d++)
                {
                    var nx = x + NavWindow.Dx(d);
                    var ny = y + NavWindow.Dy(d);
                    var rel = Rel(nx - x0, ny - y0);

                    if (rel == BotReachCluster.Self)
                    {
                        continue;
                    }

                    var nc = BotReachCluster.Neighbour(c, rel, _clustersX, _clustersY);

                    if (nc < 0 || drawn[nc] is not { } other)
                    {
                        continue;
                    }

                    var nbx = nc % _clustersX * NavWindow.ClusterSide;
                    var nby = nc / _clustersX * NavWindow.ClusterSide;
                    var ncell = (ny - nby) << 4 | (nx - nbx);

                    if (StepOk(x, y, s, d, out _, out _, out var ns))
                    {
                        var rb = other.RegionOf(ncell, ns);

                        if (rb >= 0)
                        {
                            Link(ra, rel, rb, BotReachCluster.Forward);
                        }
                    }

                    var back = (d + 4) & 7;
                    var m = _w.Count(nx, ny);

                    for (var t = 0; t < m; t++)
                    {
                        if (StepOk(nx, ny, t, back, out var bx, out var by, out var bs) && bx == x && by == y && bs == s)
                        {
                            var rb = other.RegionOf(ncell, t);

                            if (rb >= 0)
                            {
                                Link(ra, rel, rb, BotReachCluster.Backward);
                            }
                        }
                    }
                }
            }
        }

        var links = new int[_links.Count];
        var li = 0;

        foreach (var (key, ways) in _links)
        {
            links[li++] = key | (ways << 19);
        }

        Array.Sort(links);
        cluster.Links = links;

        return cluster;
    }

    /// <summary>
    /// A step as the walker takes it: the graph's own (<see cref="NavWindow.Step"/>), and on a diagonal both flanking steps must
    /// land somewhere too — the engine's rule, which reads items on the flanks as the window's mask cannot (a railing's gap
    /// one tile wide cut on the slant is no gap to a walker).
    /// </summary>
    private bool StepOk(int x, int y, int s, int d, out int nx, out int ny, out int ns)
    {
        if (!_w.Step(x, y, s, d, out nx, out ny, out ns))
        {
            return false;
        }

        if ((d & 1) == 0)
        {
            return true;
        }

        return _w.Step(x, y, s, (d + 7) & 7, out _, out _, out _) && _w.Step(x, y, s, (d + 1) & 7, out _, out _, out _);
    }

    /// <summary>How far from the land's water standing ground still counts as a shore a ship could put a walker down on.</summary>
    public const int ShoreReach = 3;

    private const int ShoreSide = NavWindow.ClusterSide + 2 * ShoreReach;

    private readonly bool[] _shoreWet = new bool[ShoreSide * ShoreSide];

    private readonly bool[] _shoreRow = new bool[ShoreSide * ShoreSide];

    /// <summary>The regions within <see cref="ShoreReach"/> of the land's own water, a bit each; null for none.</summary>
    private ulong[] Shore(int x0, int y0)
    {
        var any = false;

        for (var yy = 0; yy < ShoreSide; yy++)
        {
            for (var xx = 0; xx < ShoreSide; xx++)
            {
                var wet = _terrain.WetLand(x0 - ShoreReach + xx, y0 - ShoreReach + yy);

                _shoreWet[yy * ShoreSide + xx] = wet;
                any |= wet;
            }
        }

        if (!any)
        {
            return null;
        }

        // Across, then down: a tile is near water when any tile within the reach both ways is water.
        for (var yy = 0; yy < ShoreSide; yy++)
        {
            for (var xx = ShoreReach; xx < ShoreSide - ShoreReach; xx++)
            {
                var near = false;

                for (var k = -ShoreReach; k <= ShoreReach && !near; k++)
                {
                    near = _shoreWet[yy * ShoreSide + xx + k];
                }

                _shoreRow[yy * ShoreSide + xx] = near;
            }
        }

        ulong[] coast = null;

        for (var i = 0; i < Cells; i++)
        {
            if (_count[i] == 0)
            {
                continue;
            }

            var xx = (i & 15) + ShoreReach;
            var yy = (i >> 4) + ShoreReach;
            var near = false;

            for (var k = -ShoreReach; k <= ShoreReach && !near; k++)
            {
                near = _shoreRow[(yy + k) * ShoreSide + xx];
            }

            if (!near)
            {
                continue;
            }

            for (var s = 0; s < _count[i]; s++)
            {
                var r = _region[i * NavWindow.MaxStrata + s];

                if (r < BotReachCluster.Crowded)
                {
                    coast ??= new ulong[4];
                    coast[r >> 6] |= 1UL << (r & 63);
                }
            }
        }

        return coast;
    }

    /// <summary>Which neighbour a tile at this offset from the cluster's corner lies in: <see cref="BotReachCluster.Self"/> when not one of the four.</summary>
    private static int Rel(int dx, int dy)
    {
        var ex = dx >= NavWindow.ClusterSide;
        var wx = dx < 0;
        var sy = dy >= NavWindow.ClusterSide;

        if (!sy)
        {
            return ex && dy >= 0 ? BotReachCluster.East : BotReachCluster.Self;
        }

        return ex ? BotReachCluster.SouthEast : wx ? BotReachCluster.SouthWest : BotReachCluster.South;
    }

    private void Link(int ra, int rel, int rb, int way)
    {
        var key = ra | (rel << 8) | (rb << 11);

        _links[key] = _links.GetValueOrDefault(key) | way;
    }

    private int Find(int i)
    {
        while (_parent[i] != i)
        {
            _parent[i] = _parent[_parent[i]];
            i = _parent[i];
        }

        return i;
    }

    private void Union(int a, int b)
    {
        var ra = Find(a);
        var rb = Find(b);

        if (ra != rb)
        {
            _parent[Math.Max(ra, rb)] = Math.Min(ra, rb);
        }
    }
}
