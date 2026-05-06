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
using Server.Engines.Pathing.Cache;
using Server.Mobiles;
using Server.Systems.FeatureFlags;
using CalcMoves = Server.Movement.Movement;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.PathAlgorithms.BitmapAStar;

/// <summary>
/// A* pathfinder with a single bitmap-cache lookup per cell expansion. Default walkers
/// take one <see cref="StepCache.TryGetMask"/> call returning the 8-direction
/// mask + per-direction Z. Non-default walkers (non-GM players, creatures with swim/fly/
/// door/clip capabilities) and per-cell cache fallthroughs route through
/// <see cref="GetSuccessorsSlowPath"/>, which runs the per-direction
/// <see cref="CalcMoves.CheckMovement"/> loop for that one cell.
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

    private const int MaxDepth = 300;
    private const int AreaSize = 38;

    private const int NodeCount = AreaSize * AreaSize * PlaneCount;

    private const int PlaneOffset = 128;
    private const int PlaneCount = 13;
    private const int PlaneHeight = 20;
    public static readonly PathAlgorithm Instance = new BitmapAStarAlgorithm();

    private static readonly Direction[] _path = new Direction[AreaSize * AreaSize];
    private static readonly PathNode[] _nodes = new PathNode[NodeCount];
    private static readonly byte[] _nodeStates = new byte[NodeCount];
    private static readonly int[] _successors = new int[8];
    private static readonly PriorityQueue<int, int> _openQueue = new();

    private static int _xOffset;
    private static int _yOffset;

    // When set, GetSuccessors delegates to the per-cell slow path on every expansion
    // (creature has CanFly — Z-jumping is beyond the cache's static-only scope).
    private static bool _currentMobileNeedsSlowPath;

    // When set, diagonal corner-cut uses the strict AND-rule (BOTH cardinal partners
    // must be walkable) instead of the lenient creature OR-rule. Cache still applies —
    // partner bits live in the same source-cell mask byte. Non-GM players only.
    private static bool _currentMobilePlayerStrict;

    // Capability overlay applied to cache results. Layered each cell:
    //   effective = (walkMask & !cantWalk) | (wetMask & canSwim)
    // Reset at end of Find.
    private static bool _currentMobileCanSwim;
    private static bool _currentMobileCantWalk;

    // Dynamic-obstacle pass capability flags (per-mobile, captured in Find).
    // Mirrors MovementImpl.Check's per-mobile derivations so per-cell items/mobiles
    // checks can be evaluated without re-deriving.
    private static bool _currentMobileIgnoreDoors;
    private static bool _currentMobileIgnoreSpellFields;
    private static bool _currentMobileIgnoreMovableImpassables;

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
        // Mirrors MovementImpl: dead/spectral mobiles also ignore doors.
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
            if (++depth > MaxDepth)
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

            // Set MovementImpl globals so per-cell slow-path fallthroughs see the right state.
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

    private static int GetIndex(int x, int y, int z)
    {
        x -= _xOffset;
        y -= _yOffset;
        z += PlaneOffset;
        z /= PlaneHeight;

        return x + y * AreaSize + z * AreaSize * AreaSize;
    }

    /// <summary>
    /// One <see cref="StepCache.TryGetMask"/> call returns the 8-direction
    /// walkable mask + destination Zs. Diagonal corner-cut applies the lenient creature
    /// OR-rule using partner bits in the same mask byte — no neighbor-chunk lookup needed.
    /// On cache fallthrough or for non-default walkers, defers to
    /// <see cref="GetSuccessorsSlowPath"/> for THIS cell only.
    /// </summary>
    private static int GetSuccessors(int p, Mobile m, Map map)
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
            return GetSuccessorsSlowPath(m, map, px, py, p3D, vals);
        }

        // Capability overlay: walking allowed unless cantWalk; swimming allowed if canSwim.
        // Partner bits used for diagonal corner-cut also use the effective mask.
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

            // Diagonal corner-cut. Creatures (default): OR-rule — at least one cardinal
            // partner walkable. Non-GM players: AND-rule — BOTH partners must be walkable.
            // Partner bits live in the same source-cell mask byte either way.
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

            // Walking takes precedence over swimming when both apply (matches MovementImpl's
            // surface-selection: closest-to-startZ wins, and walk surface is always closer
            // when the creature is currently standing on land).
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

            // Dynamic-obstacle pass: items + mobiles at the target cell. Cache only
            // covers static walkability; dynamic state has to be checked at query time.
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
    /// Mirrors MovementImpl's dynamic-item / mobile collision phase for a target cell.
    /// Items: ImpassableSurface that overlap (z, z+PersonHeight), respecting capability
    /// overrides (CanOpenDoors → ignore door items; CanMoveOverObstacles → ignore movables;
    /// non-Felucca players → ignore spell fields). Mobiles: any other mobile whose Z range
    /// overlaps and which we can't move over.
    /// </summary>
    private static bool IsBlockedByDynamic(Mobile m, Map map, int x, int y, int z)
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

        return false;
    }

    /// <summary>
    /// Mirrors MovementImpl.CanMoveOver — true when m can step onto t's cell (dead bodies,
    /// hidden staff, etc.).
    /// </summary>
    private static bool CanMoveOver(Mobile m, Mobile t) =>
        !t.Alive || !m.Alive || t.IsDeadBondedPet || m.IsDeadBondedPet
        || t.Hidden && t.AccessLevel > AccessLevel.Player;

    /// <summary>
    /// Per-direction <see cref="CalcMoves.CheckMovement"/> loop for a single source cell.
    /// Runs on cache fallthrough or when <see cref="_currentMobileNeedsSlowPath"/> is set.
    /// </summary>
    private static int GetSuccessorsSlowPath(Mobile m, Map map, int px, int py, Point3D p3D, int[] vals)
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

            if (CalcMoves.CheckMovement(m, map, p3D, (Direction)i, out var z))
            {
                var idx = GetIndex(x + _xOffset, y + _yOffset, z);

                if (idx >= 0 && idx < NodeCount)
                {
                    _nodes[idx].z = z;
                    vals[count++] = idx;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// True for creatures whose movement rules the static cache can't model. Currently
    /// only CanFly — flying creatures Z-jump arbitrarily and the cache's source-Z guard
    /// would over-fire. CanSwim / CantWalk are handled via the capability overlay (walkMask
    /// + wetMask). CanOpenDoors / CanMoveOverObstacles only affect dynamic items and don't
    /// disqualify the cache.
    /// </summary>
    private static bool RequiresSlowPath(Mobile m) =>
        m is BaseCreature bc && bc.CanFly;
}
