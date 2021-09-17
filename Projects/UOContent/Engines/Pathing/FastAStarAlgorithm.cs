using System.Collections;
using Server.Mobiles;
using CalcMoves = Server.Movement.Movement;
using MoveImpl = Server.Movement.MovementImpl;

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
        private static readonly int[] _successors = new int[8];

        private static int _xOffset;
        private static int _yOffset;
        private static int _openList;

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

        private void RemoveFromChain(int node)
        {
            if (node is < 0 or >= NodeCount)
            {
                return;
            }

            if (!_touched[node] || !_onOpen[node])
            {
                return;
            }

            var prev = _nodes[node].prev;
            var next = _nodes[node].next;

            if (_openList == node)
            {
                _openList = next;
            }

            if (prev != -1)
            {
                _nodes[prev].next = next;
            }

            if (next != -1)
            {
                _nodes[next].prev = prev;
            }

            _nodes[node].prev = -1;
            _nodes[node].next = -1;
        }

        private void AddToChain(int node)
        {
            if (node is < 0 or >= NodeCount)
            {
                return;
            }

            RemoveFromChain(node);

            if (_openList != -1)
            {
                _nodes[_openList].prev = node;
            }

            _nodes[node].next = _openList;
            _nodes[node].prev = -1;

            _openList = node;

            _touched[node] = true;
            _onOpen[node] = true;
        }

        public override Direction[] Find(Mobile m, Map map, Point3D start, Point3D goal)
        {
            if (!Utility.InRange(start, goal, AreaSize))
            {
                return null;
            }

            _touched.SetAll(false);
            _onOpen.SetAll(false);

            _goal = goal;

            _xOffset = (start.X + goal.X - AreaSize) / 2;
            _yOffset = (start.Y + goal.Y - AreaSize) / 2;

            var fromNode = GetIndex(start.X, start.Y, start.Z);
            var destNode = GetIndex(goal.X, goal.Y, goal.Z);

            _openList = fromNode;

            _nodes[_openList].cost = 0;
            _nodes[_openList].total = Heuristic(start.X - _xOffset, start.Y - _yOffset, start.Z);
            _nodes[_openList].parent = -1;
            _nodes[_openList].next = -1;
            _nodes[_openList].prev = -1;
            _nodes[_openList].z = start.Z;

            _onOpen[_openList] = true;
            _touched[_openList] = true;

            var bc = m as BaseCreature;

            int backtrack = 0, depth = 0;

            var path = _path;

            while (_openList != -1)
            {
                var bestNode = FindBest(_openList);

                if (++depth > MaxDepth)
                {
                    break;
                }

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
                    break;
                }

                for (var i = 0; i < count; ++i)
                {
                    var newNode = vals[i];

                    var wasTouched = _touched[newNode];

                    if (wasTouched)
                    {
                        continue;
                    }

                    var newCost = _nodes[bestNode].cost + 1;
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

                    AddToChain(newNode);

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

        private int FindBest(int node)
        {
            var least = _nodes[node].total;
            var leastNode = node;

            while (node != -1)
            {
                if (_nodes[node].total < least)
                {
                    least = _nodes[node].total;
                    leastNode = node;
                }

                node = _nodes[node].next;
            }

            RemoveFromChain(leastNode);

            _touched[leastNode] = true;
            _onOpen[leastNode] = false;

            return leastNode;
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
