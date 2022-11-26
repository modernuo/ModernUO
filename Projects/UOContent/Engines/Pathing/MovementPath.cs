using System;
using System.Diagnostics;
using Server.Items;
using Server.PathAlgorithms;
using Server.PathAlgorithms.FastAStar;
using Server.Spells;
using Server.Targeting;

namespace Server
{
    public sealed class MovementPath
    {
        public MovementPath(Mobile m, Point3D goal)
        {
            var start = m.Location;
            var map = m.Map;

            Map = map;
            Start = start;
            Goal = goal;

            if (map == null || map == Map.Internal)
            {
                return;
            }

            if (Utility.InRange(start, goal, 1))
            {
                return;
            }

            try
            {
                var alg = OverrideAlgorithm ?? FastAStarAlgorithm.Instance;

                if (alg?.CheckCondition(m, map, start, goal) == true)
                {
                    Directions = alg.Find(m, map, start, goal);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Warning: {0}: Pathing error from {1} to {2}", e.GetType().Name, start, goal);
            }
        }

        public Map Map { get; }

        public Point3D Start { get; }

        public Point3D Goal { get; }

        public Direction[] Directions { get; }

        public bool Success => Directions?.Length > 0;

        public static PathAlgorithm OverrideAlgorithm { get; set; }

        public static void Initialize()
        {
            CommandSystem.Register("Path", AccessLevel.GameMaster, Path_OnCommand);
        }

        public static void Path_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, true, TargetFlags.None, Path_OnTarget);
            e.Mobile.SendMessage("Target a location and a path will be drawn there.");
        }

        private static void Path(Mobile from, IPoint3D p, PathAlgorithm alg, string name, int zOffset)
        {
            OverrideAlgorithm = alg;

            var watch = new Stopwatch();
            watch.Start();
            var path = new MovementPath(from, new Point3D(p));
            watch.Stop();

            if (!path.Success)
            {
                from.SendMessage($"{name} path failed: {watch.ElapsedMilliseconds}ms");
            }
            else
            {
                from.SendMessage($"{name} path success: {watch.ElapsedMilliseconds}ms");

                var x = from.X;
                var y = from.Y;
                var z = from.Z;

                WayPoint waypoint = null;

                for (var i = 0; i < path.Directions.Length; ++i)
                {
                    Movement.Movement.Offset(path.Directions[i], ref x, ref y);

                    waypoint = new WayPoint(waypoint);
                    waypoint.MoveToWorld(new Point3D(x, y, z + zOffset), from.Map);
                }
            }
        }

        public static void Path_OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D p)
            {
                return;
            }

            SpellHelper.GetSurfaceTop(ref p);

            Path(from, p, FastAStarAlgorithm.Instance, "Fast", 0);
            OverrideAlgorithm = null;
        }
    }
}
