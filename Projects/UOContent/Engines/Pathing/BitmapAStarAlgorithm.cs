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
using CalcMoves = Server.Movement.Movement;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.PathAlgorithms.BitmapAStar;

/// <summary>
/// Plan 2D / 2F — unified A* pathfinder. For default-walker mobiles, `GetSuccessors`
/// issues ONE <see cref="StaticWalkabilityCache.TryGetMask"/> call per cell expansion
/// (returning the 8-direction mask + destination Zs) instead of 8 separate
/// <see cref="MovementImpl.CheckMovement"/> orchestrations. Per-cell savings target the
/// MovementImpl orchestrator overhead (cache lookup, source-Z guard, corner-cut indirection).
///
/// Non-default-walker mobiles (players below GameMaster, BaseCreature with
/// CanSwim/CanFly/CanOpenDoors/CanMoveOverObstacles) take the per-cell slow path inline:
/// <see cref="GetSuccessors"/> short-circuits to <see cref="GetSuccessorsSlowPath"/>
/// for every expansion when <see cref="_currentMobileNeedsSlowPath"/> is set. This
/// preserves correctness (player AND-rule, capability overlays) without delegating
/// to a separate algorithm.
///
/// Per-cell fall-through to the MovementImpl slow path also occurs for default walkers
/// when <see cref="StaticWalkabilityCache.TryGetMask"/> returns Fallthrough_MultiZ /
/// Fallthrough_OffMap / Fallthrough_SourceZMismatch. In that case GetSuccessorsSlowPath
/// runs the per-direction CalcMoves.CheckMovement loop for that single cell.
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

    // Plan 2F: when set, GetSuccessors always delegates to the per-cell slow path
    // (preserves player AND-rule and BaseCreature capability overlays). Reset at end of Find.
    private static bool _currentMobileNeedsSlowPath;

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

        // Plan 2F: non-default walkers (player below GM, or creature with overlay
        // capabilities) need the per-cell slow path on every expansion — the cache only
        // models the lenient creature OR-rule for default walkers. Set the flag and let
        // GetSuccessors short-circuit to GetSuccessorsSlowPath inline.
        _currentMobileNeedsSlowPath = !IsDefaultWalker(m);

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

            // MovementImpl globals are still set so per-cell slow-path fallthroughs
            // (multi-Z, off-map, source-Z mismatch) match FastAStar's behavior exactly.
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
                return dirs;
            }
        }

        _openQueue.Clear();
        _currentMobileNeedsSlowPath = false;
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
    /// Batched successor lookup. One <see cref="StaticWalkabilityCache.TryGetMask"/>
    /// call returns the 8-direction walkable mask + destination Zs. Diagonal corner-cut
    /// is applied inline (lenient creature OR-rule on the same source mask byte — partner
    /// bits live in the same mask, no neighbor-chunk lookup needed).
    /// On Fallthrough_*, defers to <see cref="GetSuccessorsSlowPath"/> for THIS cell only.
    /// </summary>
    private static int GetSuccessors(int p, Mobile m, Map map)
    {
        var px = p % AreaSize;
        var py = p / AreaSize % AreaSize;
        var pz = _nodes[p].z;

        var p3D = new Point3D(px + _xOffset, py + _yOffset, pz);

        var vals = _successors;

        // Plan 2F: non-default walkers always use the per-cell slow path (player AND-rule,
        // capability overlays). Skip the cache entirely for these mobiles.
        if (_currentMobileNeedsSlowPath)
        {
            return GetSuccessorsSlowPath(m, map, px, py, p3D, vals);
        }

        var count = 0;

        StaticWalkabilityCache.Instance.TryGetMask(
            map, p3D.X, p3D.Y, (sbyte)p3D.Z,
            out var mask,
            out var dN, out var dNE, out var dE, out var dSE,
            out var dS, out var dSW, out var dW, out var dNW,
            out var hitKind
        );

        if (hitKind is CacheHitKind.Fallthrough_MultiZ
            or CacheHitKind.Fallthrough_OffMap
            or CacheHitKind.Fallthrough_SourceZMismatch)
        {
            // Cache can't answer this cell — fall back to per-direction slow path
            // for this single source cell. The MovementImpl globals (AlwaysIgnoreDoors,
            // IgnoreMovableImpassables, Goal) are still set by the caller in Find.
            return GetSuccessorsSlowPath(m, map, px, py, p3D, vals);
        }

        // Cache hit — process all 8 directions inline.
        for (var i = 0; i < 8; ++i)
        {
            int x;
            int y;
            switch (i)
            {
                default: // 0 N
                    {
                        x = 0;
                        y = -1;
                        break;
                    }
                case 1: // NE
                    {
                        x = 1;
                        y = -1;
                        break;
                    }
                case 2: // E
                    {
                        x = 1;
                        y = 0;
                        break;
                    }
                case 3: // SE
                    {
                        x = 1;
                        y = 1;
                        break;
                    }
                case 4: // S
                    {
                        x = 0;
                        y = 1;
                        break;
                    }
                case 5: // SW
                    {
                        x = -1;
                        y = 1;
                        break;
                    }
                case 6: // W
                    {
                        x = -1;
                        y = 0;
                        break;
                    }
                case 7: // NW
                    {
                        x = -1;
                        y = -1;
                        break;
                    }
            }

            x += px;
            y += py;

            if (x is < 0 or >= AreaSize || y is < 0 or >= AreaSize)
            {
                continue;
            }

            var rawWalkable = (mask & (1 << i)) != 0;
            if (!rawWalkable)
            {
                continue;
            }

            // Diagonal corner-cut (creature OR-rule): partner bits in the same source mask byte.
            if ((i & 1) == 1)
            {
                var leftBit = 1 << ((i - 1) & 0x7);
                var rightBit = 1 << ((i + 1) & 0x7);
                if ((mask & leftBit) == 0 && (mask & rightBit) == 0)
                {
                    continue;
                }
            }

            var z = i switch
            {
                0 => dN,
                1 => dNE,
                2 => dE,
                3 => dSE,
                4 => dS,
                5 => dSW,
                6 => dW,
                7 => dNW,
                _ => (sbyte)0
            };

            var idx = GetIndex(x + _xOffset, y + _yOffset, z);

            if (idx >= 0 && idx < NodeCount)
            {
                _nodes[idx].z = z;
                vals[count++] = idx;
            }
        }

        return count;
    }

    /// <summary>
    /// Per-direction <see cref="CalcMoves.CheckMovement"/> loop for a single source cell.
    /// Used when the cache returns Fallthrough_* for that cell, or unconditionally when
    /// <see cref="_currentMobileNeedsSlowPath"/> is true (non-default-walker mobile).
    /// </summary>
    private static int GetSuccessorsSlowPath(Mobile m, Map map, int px, int py, Point3D p3D, int[] vals)
    {
        var count = 0;

        for (var i = 0; i < 8; ++i)
        {
            int x;
            int y;
            switch (i)
            {
                default: // 0 N
                    {
                        x = 0;
                        y = -1;
                        break;
                    }
                case 1: // NE
                    {
                        x = 1;
                        y = -1;
                        break;
                    }
                case 2: // E
                    {
                        x = 1;
                        y = 0;
                        break;
                    }
                case 3: // SE
                    {
                        x = 1;
                        y = 1;
                        break;
                    }
                case 4: // S
                    {
                        x = 0;
                        y = 1;
                        break;
                    }
                case 5: // SW
                    {
                        x = -1;
                        y = 1;
                        break;
                    }
                case 6: // W
                    {
                        x = -1;
                        y = 0;
                        break;
                    }
                case 7: // NW
                    {
                        x = -1;
                        y = -1;
                        break;
                    }
            }

            x += px;
            y += py;

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
    /// Mirror of <see cref="CachedMovementCheck.IsDefaultWalker"/>. A non-default walker
    /// causes <see cref="Find"/> to set <see cref="_currentMobileNeedsSlowPath"/> so that
    /// every <see cref="GetSuccessors"/> call short-circuits to the per-cell slow path.
    /// </summary>
    private static bool IsDefaultWalker(Mobile m)
    {
        // Non-GM Players use the strict AND-rule for diagonal corner-cut (Movement.cs:144-162);
        // the cache only models the lenient creature OR-rule. Route them to the slow path.
        if (m.Player && m.AccessLevel < AccessLevel.GameMaster)
        {
            return false;
        }

        if (m is not BaseCreature bc)
        {
            // Plain Mobile or staff (GM+) — default walker for the cache's purposes.
            return true;
        }

        return !bc.CanSwim && !bc.CanFly && !bc.CanOpenDoors && !bc.CanMoveOverObstacles;
    }
}
