/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FastAStarAlgorithm.cs                                           *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using System.Collections;
using Server.Mobiles;
using CalcMoves = Server.Movement.Movement;
using MoveImpl = Server.Movement.MovementImpl;
using System.Collections.Generic;

namespace Server.PathAlgorithms.FastAStar
{
    public struct PathNode
    {
        public int cost, total;
        public int parent, next, prev;
        public int z;
    }

    public class FastAStarAlgorithm : PathAlgorithm
    {
        private const int MaxDepth = 300;
        private const int AreaSize = 38;

        private const int NodeCount = AreaSize * AreaSize * PlaneCount;

        private const int PlaneOffset = 128;
        private const int PlaneCount = 13;
        private const int PlaneHeight = 20;
        public static PathAlgorithm Instance = new FastAStarAlgorithm();

        private static readonly Direction[] _path = new Direction[AreaSize * AreaSize];
        private static readonly PathNode[] _nodes = new PathNode[NodeCount];
        private static readonly BitArray _touched = new(NodeCount);
        private static readonly BitArray _onOpen = new(NodeCount);
        private static readonly BitArray _closed = new BitArray(NodeCount);
        private static readonly int[] _successors = new int[8];

        private static int _xOffset;
        private static int _yOffset;

        private Point3D _goal;

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

            _touched.SetAll(false);
            _onOpen.SetAll(false);
            _closed.SetAll(false);

            _goal = goal;

            _xOffset = (start.X + goal.X - AreaSize) / 2;
            _yOffset = (start.Y + goal.Y - AreaSize) / 2;

            var fromNode = GetIndex(start.X, start.Y, start.Z);
            var destNode = GetIndex(goal.X, goal.Y, goal.Z);

            var openQueue = new PriorityQueue<int, int>();

            _nodes[fromNode].cost = 0;
            _nodes[fromNode].total = Heuristic(start.X - _xOffset, start.Y - _yOffset, start.Z);
            _nodes[fromNode].parent = -1;
            _nodes[fromNode].z = start.Z;

            _onOpen[fromNode] = true;
            _touched[fromNode] = true;

            openQueue.Enqueue(fromNode, _nodes[fromNode].total);

            var bc = m as BaseCreature;

            int backtrack = 0, depth = 0;

            var path = _path;

            while (openQueue.Count > 0)
            {
                if (++depth > MaxDepth)
                {
                    break;
                }

                var bestNode = openQueue.Dequeue();
                _onOpen[bestNode] = false;
                _closed[bestNode] = true;

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

                    if (_closed[newNode])
                    {
                        continue;
                    }

                    var wasTouched = _touched[newNode];

                    if (wasTouched)
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

                    _nodes[newNode].parent = bestNode;
                    _nodes[newNode].cost = newCost;
                    _nodes[newNode].total = newTotal;

                    if (_onOpen[newNode])
                    {
                        continue;
                    }

                    openQueue.Enqueue(newNode, newTotal);
                    _onOpen[newNode] = true;
                    _touched[newNode] = true;

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

                    return dirs;
                }
            }

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

        public int GetSuccessors(int p, Mobile m, Map map)
        {
            var px = p % AreaSize;
            var py = p / AreaSize % AreaSize;
            var pz = _nodes[p].z;

            var p3D = new Point3D(px + _xOffset, py + _yOffset, pz);

            var vals = _successors;
            var count = 0;

            for (var i = 0; i < 8; ++i)
            {
                int x;
                int y;
                switch (i)
                {
                    default: // 0
                        x = 0;
                        y = -1;
                        break;
                    case 1:
                        x = 1;
                        y = -1;
                        break;
                    case 2:
                        x = 1;
                        y = 0;
                        break;
                    case 3:
                        x = 1;
                        y = 1;
                        break;
                    case 4:
                        x = 0;
                        y = 1;
                        break;
                    case 5:
                        x = -1;
                        y = 1;
                        break;
                    case 6:
                        x = -1;
                        y = 0;
                        break;
                    case 7:
                        x = -1;
                        y = -1;
                        break;
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
    }
}
