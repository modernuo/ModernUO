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

        if (_currentMobileNeedsSlowPath)
        {
            return GetSuccessorsSlowPath(m, map, px, py, p3D, vals);
        }

        var count = 0;

        StepCache.Instance.TryGetMask(
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
            return GetSuccessorsSlowPath(m, map, px, py, p3D, vals);
        }

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

            // Diagonal corner-cut (creature OR-rule): partner bits live in the same mask byte.
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
    /// Default walker = the cache's baked rules apply directly (lenient OR-rule for
    /// diagonal corner-cut, no capability overlays). Non-GM players (strict AND-rule)
    /// and creatures with swim/fly/door/clip capabilities require the slow path.
    /// </summary>
    private static bool IsDefaultWalker(Mobile m)
    {
        if (m.Player && m.AccessLevel < AccessLevel.GameMaster)
        {
            return false;
        }

        if (m is not BaseCreature bc)
        {
            return true;
        }

        return !bc.CanSwim && !bc.CanFly && !bc.CanOpenDoors && !bc.CanMoveOverObstacles;
    }
}
