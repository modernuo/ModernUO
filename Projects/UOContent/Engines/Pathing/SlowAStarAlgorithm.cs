using System;
using Server.Mobiles;
using CalcMoves = Server.Movement.Movement;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.PathAlgorithms.SlowAStar
{
    public struct PathNode
    {
        public int x, y, z;
        public int g, h;
        public int px, py, pz;
        public int dir;
    }

    public class SlowAStarAlgorithm : PathAlgorithm
    {
        private const int MaxDepth = 300;
        private const int MaxNodes = MaxDepth * 16;
        public static PathAlgorithm Instance = new SlowAStarAlgorithm();

        private static readonly PathNode[] m_Closed = new PathNode[MaxNodes];
        private static readonly PathNode[] m_Open = new PathNode[MaxNodes];
        private static readonly PathNode[] m_Successors = new PathNode[8];
        private static readonly Direction[] m_Path = new Direction[MaxNodes];

        private Point3D m_Goal;

        public int Heuristic(int x, int y, int z)
        {
            x -= m_Goal.X;
            y -= m_Goal.Y;
            z -= m_Goal.Z;

            x *= 11;
            y *= 11;

            return x * x + y * y + z * z;
        }

        public override bool CheckCondition(Mobile m, Map map, Point3D start, Point3D goal) => false;

        public override Direction[] Find(Mobile m, Map map, Point3D start, Point3D goal)
        {
            m_Goal = goal;

            var bc = m as BaseCreature;

            PathNode curNode;

            var goalNode = new PathNode();
            goalNode.x = goal.X;
            goalNode.y = goal.Y;
            goalNode.z = goal.Z;

            var startNode = new PathNode();
            startNode.x = start.X;
            startNode.y = start.Y;
            startNode.z = start.Z;
            startNode.h = Heuristic(startNode.x, startNode.y, startNode.z);

            PathNode[] closed = m_Closed, open = m_Open, successors = m_Successors;
            var path = m_Path;

            int closedCount = 0, openCount = 0;
            var pathCount = 0;
            var depth = 0;

            var iBacktrack = 0;

            open[openCount++] = startNode;

            while (openCount > 0)
            {
                curNode = open[0];
                var curF = curNode.g + curNode.h;
                var popIndex = 0;

                for (var i = 1; i < openCount; ++i)
                {
                    if (open[i].g + open[i].h < curF)
                    {
                        curNode = open[i];
                        curF = curNode.g + curNode.h;
                        popIndex = i;
                    }
                }

                if (curNode.x == goalNode.x && curNode.y == goalNode.y && (curNode.z - goalNode.z).Abs() < 16)
                {
                    if (closedCount == MaxNodes)
                    {
                        break;
                    }

                    closed[closedCount++] = curNode;

                    var xBacktrack = curNode.px;
                    var yBacktrack = curNode.py;
                    var zBacktrack = curNode.pz;

                    path[pathCount++] = (Direction)curNode.dir;

                    // if (pathCount == MaxNodes)
                    // {
                    //     break;
                    // }

                    while (xBacktrack != startNode.x || yBacktrack != startNode.y || zBacktrack != startNode.z)
                    {
                        var found = false;

                        for (var j = 0; !found && j < closedCount; ++j)
                        {
                            if (closed[j].x == xBacktrack && closed[j].y == yBacktrack && closed[j].z == zBacktrack)
                            {
                                if (pathCount == MaxNodes)
                                {
                                    break;
                                }

                                curNode = closed[j];
                                path[pathCount++] = (Direction)curNode.dir;
                                xBacktrack = curNode.px;
                                yBacktrack = curNode.py;
                                zBacktrack = curNode.pz;
                                found = true;
                            }
                        }

                        if (!found)
                        {
                            Console.WriteLine("bugaboo..");
                            return null;
                        }

                        if (pathCount == MaxNodes)
                        {
                            break;
                        }
                    }

                    if (pathCount == MaxNodes)
                    {
                        break;
                    }

                    var dirs = new Direction[pathCount];

                    while (pathCount > 0)
                    {
                        dirs[iBacktrack++] = path[--pathCount];
                    }

                    return dirs;
                }

                --openCount;

                for (var i = popIndex; i < openCount; ++i)
                {
                    open[i] = open[i + 1];
                }

                var sucCount = 0;

                if (bc != null)
                {
                    MoveImpl.AlwaysIgnoreDoors = bc.CanOpenDoors;
                    MoveImpl.IgnoreMovableImpassables = bc.CanMoveOverObstacles;
                }

                MoveImpl.Goal = goal;

                int x;
                int y;
                int z;
                for (var i = 0; i < 8; ++i)
                {
                    switch (i)
                    {
                        default:
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

                    if (CalcMoves.CheckMovement(m, map, new Point3D(curNode.x, curNode.y, curNode.z), (Direction)i, out z))
                    {
                        successors[sucCount].x = x + curNode.x;
                        successors[sucCount].y = y + curNode.y;
                        successors[sucCount++].z = z;
                    }
                }

                MoveImpl.AlwaysIgnoreDoors = false;
                MoveImpl.IgnoreMovableImpassables = false;
                MoveImpl.Goal = Point3D.Zero;

                if (sucCount == 0 || ++depth > MaxDepth)
                {
                    break;
                }

                for (var i = 0; i < sucCount; ++i)
                {
                    x = successors[i].x;
                    y = successors[i].y;
                    z = successors[i].z;

                    successors[i].g = curNode.g + 1;

                    int openIndex = -1, closedIndex = -1;

                    for (var j = 0; openIndex == -1 && j < openCount; ++j)
                    {
                        if (open[j].x == x && open[j].y == y && open[j].z == z)
                        {
                            openIndex = j;
                        }
                    }

                    if (openIndex >= 0 && open[openIndex].g < successors[i].g)
                    {
                        continue;
                    }

                    for (var j = 0; closedIndex == -1 && j < closedCount; ++j)
                    {
                        if (closed[j].x == x && closed[j].y == y && closed[j].z == z)
                        {
                            closedIndex = j;
                        }
                    }

                    if (closedIndex >= 0 && closed[closedIndex].g < successors[i].g)
                    {
                        continue;
                    }

                    if (openIndex >= 0)
                    {
                        --openCount;

                        for (var j = openIndex; j < openCount; ++j)
                        {
                            open[j] = open[j + 1];
                        }
                    }

                    if (closedIndex >= 0)
                    {
                        --closedCount;

                        for (var j = closedIndex; j < closedCount; ++j)
                        {
                            closed[j] = closed[j + 1];
                        }
                    }

                    successors[i].px = curNode.x;
                    successors[i].py = curNode.y;
                    successors[i].pz = curNode.z;
                    successors[i].dir = (int)GetDirection(curNode.x, curNode.y, x, y);
                    successors[i].h = Heuristic(x, y, z);

                    if (openCount == MaxNodes)
                    {
                        break;
                    }

                    open[openCount++] = successors[i];
                }

                if (openCount == MaxNodes || closedCount == MaxNodes)
                {
                    break;
                }

                closed[closedCount++] = curNode;
            }

            return null;
        }
    }
}
