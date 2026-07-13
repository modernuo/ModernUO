/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BitmapAStarAlgorithm.cs                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Engines.Pathing;
using Server.Engines.Pathing.Cache;
using Server.Mobiles;
using Server.Systems.FeatureFlags;
using CalcMoves = Server.Movement.Movement;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.PathAlgorithms;

/// <summary>
/// A* pathfinder that expands a cell with a single <see cref="StepCache.TryGetMask"/> lookup,
/// which returns all 8 directions' walkability and destination Zs at once. Where the cache can't
/// answer — a fallthrough on that cell, or a flying creature the static cache can't model —
/// <see cref="GetSuccessorsSlowPath"/> runs the per-direction <see cref="CalcMoves.CheckMovement"/>
/// loop for that one cell instead, so a partial cache miss costs only the cells it affects.
/// </summary>
public class BitmapAStarAlgorithm : PathAlgorithm
{
    private struct PathNode
    {
        public int cost;
        public int total;
        public int parent;
        public int z;
    }

    private const int AreaSize = 38;

    private const int NodeCount = AreaSize * AreaSize * PlaneCount;

    private const int PlaneOffset = 128;
    private const int PlaneCount = 13;
    private const int PlaneHeight = 20;
    // The shared default. A differently-configured variant is just another instance.
    public static readonly BitmapAStarAlgorithm Instance = new();

    // Scratch reused across every Find on this instance — roughly 320 KB of it, so create
    // instances once and hold them, never per call. Per-instance rather than static so two
    // differently-configured algorithms don't share state. Reuse is safe because the game loop is
    // single-threaded and Find never re-enters.
    private readonly Direction[] _path = new Direction[AreaSize * AreaSize];
    private readonly PathNode[] _nodes = new PathNode[NodeCount];
    private readonly byte[] _nodeStates = new byte[NodeCount];
    private readonly int[] _successors = new int[8];
    private readonly PriorityQueue<int, int> _openQueue = new();

    // Expansion budget: the search gives up and returns null past this many nodes. It bounds the
    // cost of an unreachable goal, which would otherwise exhaust the whole search window. A
    // successful search stops when it finds the goal, so the budget only binds on hard or hopeless
    // routes — it needs to stay high enough to solve walled-off indoor ones.
    public int MaxSearchNodes { get; set; } = 1000;

    private int _xOffset;
    private int _yOffset;

    // Every expansion goes to the slow path: the creature can fly, and arbitrary Z-jumping is
    // outside what a static cache can model.
    private bool _currentMobileNeedsSlowPath;

    // Diagonal corner-cut uses the strict rule — both cardinal partners walkable, not just one.
    // Non-GM players only. The cache still applies; the partner bits are in the same mask byte.
    private bool _currentMobilePlayerStrict;

    // Capability overlay on the cache's two rule sets, applied per cell as
    //   effective = (walkMask & !cantWalk) | (wetMask & canSwim)
    private bool _currentMobileCanSwim;
    private bool _currentMobileCantWalk;

    // Per-mobile flags for the dynamic-obstacle pass, derived once in Find rather than per cell.
    private bool _currentMobileIgnoreDoors;
    private bool _currentMobileIgnoreSpellFields;
    private bool _currentMobileIgnoreMovableImpassables;

    public static void Configure()
    {
        // Shard-tunable expansion budget for the shared instance; see MaxSearchNodes. Written back
        // to server.cfg on first boot so it's discoverable.
        Instance.MaxSearchNodes = ServerConfiguration.GetOrUpdateSetting(
            "pathfinding.maxSearchNodes",
            1000
        );
    }

    private Point3D _goal;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Heuristic(int x, int y, int z)
    {
        x -= _goal.X - _xOffset;
        y -= _goal.Y - _yOffset;
        z -= _goal.Z;

        x *= 11;
        y *= 11;

        return x * x + y * y + z * z;
    }

    public override bool CheckCondition(Mobile m, Map map, Point3D start, Point3D goal) =>
        Utility.InRange(start, goal, AreaSize);

    public override Direction[] Find(Mobile m, Map map, Point3D start, Point3D goal)
    {
        if (!Utility.InRange(start, goal, AreaSize))
        {
            return null;
        }

        // The frontier probes a given chunk dozens of times over one search; opening a generation
        // is what makes the cache's promotion gate count all of that as a single touch.
        StepCache.Instance.BeginFindGeneration();

        PathfindRecorder.RecordIfEnabled(m, map, start, goal);

        _currentMobileNeedsSlowPath = RequiresSlowPath(m);
        _currentMobilePlayerStrict = m.Player && m.AccessLevel < AccessLevel.GameMaster;
        if (m is BaseCreature creature)
        {
            _currentMobileCanSwim = creature.CanSwim;
            _currentMobileCantWalk = creature.CantWalk;
            _currentMobileIgnoreDoors = creature.CanOpenDoors;
            _currentMobileIgnoreMovableImpassables = creature.CanMoveOverObstacles;
        }
        else
        {
            _currentMobileCanSwim = false;
            _currentMobileCantWalk = false;
            _currentMobileIgnoreDoors = false;
            _currentMobileIgnoreMovableImpassables = false;
        }

        // Dead and spectral mobiles pass through doors too. Mirrors MovementImpl.
        _currentMobileIgnoreDoors |= !m.Alive || m.Body.BodyID == 0x3DB || m.IsDeadBondedPet;
        _currentMobileIgnoreSpellFields = m is PlayerMobile && map != Map.Felucca;

        Array.Clear(_nodeStates);

        _goal = goal;

        _xOffset = (start.X + goal.X - AreaSize) / 2;
        _yOffset = (start.Y + goal.Y - AreaSize) / 2;

        var fromNode = GetIndex(start.X, start.Y, start.Z);
        var destNode = GetIndex(goal.X, goal.Y, goal.Z);

        _nodes[fromNode].cost = 0;
        _nodes[fromNode].total = Heuristic(start.X - _xOffset, start.Y - _yOffset, start.Z);
        _nodes[fromNode].parent = -1;
        _nodes[fromNode].z = start.Z;

        _openQueue.Enqueue(fromNode, _nodes[fromNode].total);
        _nodeStates[fromNode] = 1;

        var bc = m as BaseCreature;

        int backtrack = 0, depth = 0;

        var path = _path;

        while (_openQueue.Count > 0)
        {
            if (++depth > MaxSearchNodes)
            {
                break;
            }

            if (!_openQueue.TryDequeue(out var bestNode, out var bestTotal))
            {
                break;
            }

            // Duplicate, lower priority
            if (_nodeStates[bestNode] == 2 || _nodes[bestNode].total != bestTotal)
            {
                continue;
            }

            _nodeStates[bestNode] = 2;

            // MovementImpl reads these statics, so a slow-path fallthrough on any cell below needs
            // them set for this mobile.
            if (bc != null)
            {
                MoveImpl.AlwaysIgnoreDoors = bc.CanOpenDoors;
                MoveImpl.IgnoreMovableImpassables = bc.CanMoveOverObstacles;
            }

            MoveImpl.Goal = goal;

            var vals = _successors;
            var count = GetSuccessors(bestNode, m, map);

            MoveImpl.AlwaysIgnoreDoors = false;
            MoveImpl.IgnoreMovableImpassables = false;
            MoveImpl.Goal = Point3D.Zero;

            if (count == 0)
            {
                continue;
            }

            for (var i = 0; i < count; ++i)
            {
                var newNode = vals[i];

                // Skip if the node is already closed
                if (_nodeStates[newNode] == 2)
                {
                    continue;
                }

                var isDiagonal = i % 2 == 1;
                var moveCost = isDiagonal ? 14 : 10;
                var newCost = _nodes[bestNode].cost + moveCost;
                var newTotal = newCost + Heuristic(
                    newNode % AreaSize,
                    newNode / AreaSize % AreaSize,
                    _nodes[newNode].z
                );

                if (_nodeStates[newNode] == 0 || newTotal < _nodes[newNode].total)
                {
                    _nodes[newNode].parent = bestNode;
                    _nodes[newNode].cost = newCost;
                    _nodes[newNode].total = newTotal;

                    // Requeue (duplicates allowed), and mark as open
                    _openQueue.Enqueue(newNode, newTotal);
                    _nodeStates[newNode] = 1;
                }

                if (newNode != destNode)
                {
                    continue;
                }

                var pathCount = 0;
                var parent = _nodes[newNode].parent;

                while (parent != -1)
                {
                    path[pathCount++] = GetDirection(
                        parent % AreaSize,
                        parent / AreaSize % AreaSize,
                        newNode % AreaSize,
                        newNode / AreaSize % AreaSize
                    );
                    newNode = parent;
                    parent = _nodes[newNode].parent;

                    if (newNode == fromNode)
                    {
                        break;
                    }
                }

                var dirs = new Direction[pathCount];

                while (pathCount > 0)
                {
                    dirs[backtrack++] = path[--pathCount];
                }

                _openQueue.Clear();
                _currentMobileNeedsSlowPath = false;
                _currentMobilePlayerStrict = false;
                _currentMobileCanSwim = false;
                _currentMobileCantWalk = false;
                _currentMobileIgnoreDoors = false;
                _currentMobileIgnoreSpellFields = false;
                _currentMobileIgnoreMovableImpassables = false;
                return dirs;
            }
        }

        _openQueue.Clear();
        _currentMobileNeedsSlowPath = false;
        _currentMobilePlayerStrict = false;
        _currentMobileCanSwim = false;
        _currentMobileCantWalk = false;
        _currentMobileIgnoreDoors = false;
        _currentMobileIgnoreSpellFields = false;
        _currentMobileIgnoreMovableImpassables = false;
        return null;
    }

    private int GetIndex(int x, int y, int z)
    {
        x -= _xOffset;
        y -= _yOffset;
        z += PlaneOffset;
        z /= PlaneHeight;

        return x + y * AreaSize + z * AreaSize * AreaSize;
    }

    /// <summary>
    /// Expands one cell into its walkable neighbours. A single cache lookup covers all 8
    /// directions, including the partner bits the diagonal corner-cut needs, so no neighbouring
    /// cell has to be consulted. Falls back to <see cref="GetSuccessorsSlowPath"/> for this cell
    /// alone when the cache can't answer.
    /// </summary>
    private int GetSuccessors(int p, Mobile m, Map map)
    {
        var px = p % AreaSize;
        var py = p / AreaSize % AreaSize;
        var pz = _nodes[p].z;

        var p3D = new Point3D(px + _xOffset, py + _yOffset, pz);

        var vals = _successors;

        if (_currentMobileNeedsSlowPath || !ContentFeatureFlags.BitmapPathfindingCache)
        {
            return GetSuccessorsSlowPath(m, map, px, py, p3D, vals);
        }

        var count = 0;

        var lookup = StepCache.Instance.TryGetMask(map, p3D.X, p3D.Y, (sbyte)p3D.Z);

        if (!lookup.IsHit)
        {
            // A multi-covered cell still gets a whole-cell mask, synthesized over the house or boat
            // components rather than looked up. That keeps it out of the slow path's 8 separate
            // CheckMovement calls, and the result flows through the same overlay, corner-cut and
            // dynamic-obstacle logic below as a cache hit would.
            if (lookup.HitKind == CacheHitKind.Fallthrough_Multi)
            {
                lookup = MultiMaskCache.Instance.GetMask(map, p3D.X, p3D.Y, (sbyte)p3D.Z);
            }
            else
            {
                return GetSuccessorsSlowPath(m, map, px, py, p3D, vals);
            }
        }

        // Overlay the mobile's capabilities onto the cache's two rule sets. The corner-cut below
        // reads its partner bits from this effective mask, not the raw one.
        var walkBits = _currentMobileCantWalk ? (byte)0 : lookup.WalkMask;
        var swimBits = _currentMobileCanSwim ? lookup.WetMask : (byte)0;
        var mask = (byte)(walkBits | swimBits);

        for (var i = 0; i < 8; ++i)
        {
            var x = px;
            var y = py;
            CalcMoves.Offset((Direction)i, ref x, ref y);

            if (x is < 0 or >= AreaSize || y is < 0 or >= AreaSize)
            {
                continue;
            }

            if ((mask & (1 << i)) == 0)
            {
                continue;
            }

            // Diagonal corner-cut: a creature needs at least one of the two flanking cardinals to
            // be walkable, a non-GM player needs both.
            if ((i & 1) == 1)
            {
                var leftBit = 1 << ((i - 1) & 0x7);
                var rightBit = 1 << ((i + 1) & 0x7);
                if (_currentMobilePlayerStrict
                        ? (mask & leftBit) == 0 || (mask & rightBit) == 0
                        : (mask & leftBit) == 0 && (mask & rightBit) == 0)
                {
                    continue;
                }
            }

            // Walking wins over swimming where both are possible. MovementImpl picks the surface
            // closest to the start Z, and for a creature standing on land that is always the walk
            // surface.
            var useWalkZ = (walkBits & (1 << i)) != 0;
            var z = useWalkZ
                ? i switch
                {
                    0 => lookup.WalkZ_N,
                    1 => lookup.WalkZ_NE,
                    2 => lookup.WalkZ_E,
                    3 => lookup.WalkZ_SE,
                    4 => lookup.WalkZ_S,
                    5 => lookup.WalkZ_SW,
                    6 => lookup.WalkZ_W,
                    7 => lookup.WalkZ_NW,
                    _ => (sbyte)0
                }
                : i switch
                {
                    0 => lookup.SwimZ_N,
                    1 => lookup.SwimZ_NE,
                    2 => lookup.SwimZ_E,
                    3 => lookup.SwimZ_SE,
                    4 => lookup.SwimZ_S,
                    5 => lookup.SwimZ_SW,
                    6 => lookup.SwimZ_W,
                    7 => lookup.SwimZ_NW,
                    _ => (sbyte)0
                };

            var absX = x + _xOffset;
            var absY = y + _yOffset;

            // The cache only knows static terrain, so items and mobiles at the target cell have to
            // be checked live.
            if (IsBlockedByDynamic(m, map, absX, absY, z))
            {
                continue;
            }

            var idx = GetIndex(absX, absY, z);

            if (idx >= 0 && idx < NodeCount)
            {
                _nodes[idx].z = z;
                vals[count++] = idx;
            }
        }

        return count;
    }

    private const int PersonHeightConst = 16;
    private const int MobileHeight = 15;

    /// <summary>
    /// MovementImpl's item and mobile collision phase for one target cell: impassable items
    /// overlapping the mobile's vertical envelope block it, subject to the capability overrides,
    /// as does any other mobile it can't move over.
    /// </summary>
    private bool IsBlockedByDynamic(Mobile m, Map map, int x, int y, int z)
    {
        var ourTop = z + PersonHeightConst;

        foreach (var item in map.GetItemsAt(x, y))
        {
            var itemData = item.ItemData;
            if (!itemData.ImpassableSurface)
            {
                continue;
            }

            if (_currentMobileIgnoreMovableImpassables && item.Movable)
            {
                continue;
            }

            var itemId = item.ItemID & TileData.MaxItemValue;
            if (_currentMobileIgnoreDoors
                && (itemData.Door
                    || itemId is 0x692 or 0x846 or 0x873
                    || itemId >= 0x6F5 && itemId <= 0x6F6))
            {
                continue;
            }

            if (_currentMobileIgnoreSpellFields && itemId is 0x82 or 0x3946 or 0x3956)
            {
                continue;
            }

            var checkZ = item.Z;
            var checkTop = checkZ + itemData.CalcHeight;
            if (checkTop > z && ourTop > checkZ)
            {
                return true;
            }
        }

        // The goal cell is usually occupied by whatever the mobile is chasing, so blocking on it
        // would fail every pursuit. The follower stops short of the goal anyway. Every other cell
        // still blocks on mobiles.
        var skipMobCheck = x == MoveImpl.Goal.X && y == MoveImpl.Goal.Y;

        if (!skipMobCheck)
        {
            foreach (var mob in map.GetMobilesAt(x, y))
            {
                if (mob == m)
                {
                    continue;
                }

                if (mob.Z + MobileHeight > z && z + MobileHeight > mob.Z && !CanMoveOver(m, mob))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// True when m can step onto t's cell — a corpse, hidden staff, and so on. Mirrors
    /// MovementImpl.CanMoveOver.
    /// </summary>
    private static bool CanMoveOver(Mobile m, Mobile t) =>
        !t.Alive || !m.Alive || t.IsDeadBondedPet || m.IsDeadBondedPet
        || t.Hidden && t.AccessLevel > AccessLevel.Player;

    /// <summary>
    /// Expands one cell the long way, with a CheckMovement call per direction. The same-cell
    /// mobile check is layered on top because MovementImpl doesn't iterate those.
    /// </summary>
    private int GetSuccessorsSlowPath(Mobile m, Map map, int px, int py, Point3D p3D, int[] vals)
    {
        var count = 0;

        for (var i = 0; i < 8; ++i)
        {
            var x = px;
            var y = py;
            CalcMoves.Offset((Direction)i, ref x, ref y);

            if (x is < 0 or >= AreaSize || y is < 0 or >= AreaSize)
            {
                continue;
            }

            if (!CalcMoves.CheckMovement(m, map, p3D, (Direction)i, out var z))
            {
                continue;
            }

            var absX = x + _xOffset;
            var absY = y + _yOffset;
            if (IsBlockedByDynamic(m, map, absX, absY, z))
            {
                continue;
            }

            var idx = GetIndex(absX, absY, z);
            if (idx >= 0 && idx < NodeCount)
            {
                _nodes[idx].z = z;
                vals[count++] = idx;
            }
        }

        return count;
    }

    /// <summary>
    /// True for creatures the static cache can't model at all. Only flying ones qualify: they
    /// Z-jump freely, so the cache's source-Z guard would reject nearly every cell anyway. Swim
    /// and cant-walk are handled by the capability overlay, and the door / obstacle capabilities
    /// only affect dynamic items, so none of those disqualify the cache.
    /// </summary>
    private static bool RequiresSlowPath(Mobile m) => m is BaseCreature bc && bc.CanFly;
}
