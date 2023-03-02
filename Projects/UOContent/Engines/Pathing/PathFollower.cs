using System;
using CalcMoves = Server.Movement.Movement;

namespace Server
{
    public class PathFollower
    {
        private static bool Enabled;
        private static readonly TimeSpan RepathDelay = TimeSpan.FromSeconds(2.0);

        private readonly Mobile m_From;
        private int m_Index;
        private DateTime m_LastPathTime;
        private Point3D m_Next, m_LastGoalLoc;
        private MovementPath m_Path;

        public PathFollower(Mobile from, IPoint3D goal)
        {
            m_From = from;
            Goal = goal;
        }

        public MoveMethod Mover { get; set; }

        public IPoint3D Goal { get; }

        public static void Configure()
        {
            Enabled = ServerConfiguration.GetOrUpdateSetting("pathfinding.enable", true);
        }

        public MoveResult Move(Direction d) =>
            Mover?.Invoke(d) ?? (m_From.Move(d) ? MoveResult.Success : MoveResult.Blocked);

        public Point3D GetGoalLocation() => (Goal as Item)?.GetWorldLocation() ?? new Point3D(Goal);

        public void Advance(ref Point3D p, int index)
        {
            if (m_Path?.Success == true)
            {
                var dirs = m_Path.Directions;

                if (index >= 0 && index < dirs.Length)
                {
                    int x = p.X, y = p.Y;

                    CalcMoves.Offset(dirs[index], ref x, ref y);

                    p.X = x;
                    p.Y = y;
                }
            }
        }

        public void ForceRepath()
        {
            m_Path = null;
        }

        public bool CheckPath()
        {
            if (!Enabled)
            {
                return false;
            }

            var goal = GetGoalLocation();

            if (m_Path != null && (m_Path.Success && goal == m_LastGoalLoc || m_LastPathTime + RepathDelay > Core.Now) &&
                !(m_Path.Success && Check(m_From.Location, m_LastGoalLoc, 0)))
            {
                return false;
            }

            m_LastPathTime = Core.Now;
            m_LastGoalLoc = goal;

            m_Path = new MovementPath(m_From, goal);

            m_Index = 0;
            m_Next = m_From.Location;

            Advance(ref m_Next, m_Index);

            return true;
        }

        public static bool Check(Point3D loc, Point3D goal, int range) =>
            Utility.InRange(loc, goal, range) && (range > 1 || (loc.Z - goal.Z).Abs() < 16);

        public bool Follow(bool run, int range)
        {
            var goal = GetGoalLocation();
            Direction d;

            if (Check(m_From.Location, goal, range))
            {
                return true;
            }

            var repathed = CheckPath();

            if (!(Enabled && m_Path.Success))
            {
                d = m_From.GetDirectionTo(goal);
                m_From.SetDirection(d);
                Move(d);

                return Check(m_From.Location, goal, range);
            }

            d = m_From.GetDirectionTo(m_Next);
            m_From.SetDirection(d);
            var res = Move(d);

            if (res == MoveResult.Blocked)
            {
                if (repathed)
                {
                    return false;
                }

                m_Path = null;
                CheckPath();

                if (!m_Path!.Success)
                {
                    d = m_From.GetDirectionTo(goal);
                    m_From.SetDirection(d);
                    Move(d);

                    return Check(m_From.Location, goal, range);
                }

                d = m_From.GetDirectionTo(m_Next);
                m_From.SetDirection(d);

                if (Move(d) == MoveResult.Blocked)
                {
                    return false;
                }
            }

            if (m_From.X == m_Next.X && m_From.Y == m_Next.Y)
            {
                if (m_From.Z == m_Next.Z)
                {
                    ++m_Index;
                    Advance(ref m_Next, m_Index);
                }
                else
                {
                    m_Path = null;
                }
            }

            return Check(m_From.Location, goal, range);
        }
    }
}
