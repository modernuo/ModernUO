/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Movement.cs                                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Movement
{
    public static class Movement
    {
        // Movement implementation algorithm
        public static IMovementImpl Impl { get; set; }
        public static int WalkFootDelay { get; set; } = 400;
        public static int RunFootDelay { get; set; } = 200;
        public static int WalkMountDelay { get; set; } = 200;
        public static int RunMountDelay { get; set; } = 100;

        public static bool EnableFastwalkPrevention { get; set; } = true;
        public static AccessLevel FastwalkExemptionLevel { get; set; } = AccessLevel.Counselor;

        // If this is changed during runtime, then the steps array needs resizing.
        public static int MaxSteps { get; private set; } = 3;

        public static void Configure()
        {
            EnableFastwalkPrevention = ServerConfiguration.GetOrUpdateSetting("movement.enableFastWalkPrevention", EnableFastwalkPrevention);
            MaxSteps = ServerConfiguration.GetOrUpdateSetting("movement.maxSteps", MaxSteps);
            FastwalkExemptionLevel = ServerConfiguration.GetOrUpdateSetting("movement.fastwalkExemptionLevel", FastwalkExemptionLevel);
            WalkFootDelay = ServerConfiguration.GetOrUpdateSetting("movement.delay.walkFoot", WalkFootDelay);
            RunFootDelay = ServerConfiguration.GetOrUpdateSetting("movement.delay.runFoot", RunFootDelay);
            WalkMountDelay = ServerConfiguration.GetOrUpdateSetting("movement.delay.walkMount", WalkMountDelay);
            RunMountDelay = ServerConfiguration.GetOrUpdateSetting("movement.delay.runMount", RunMountDelay);
        }

        public static bool CheckMovement(Mobile m, Direction d, out int newZ)
        {
            if (Impl != null)
            {
                return Impl.CheckMovement(m, d, out newZ);
            }

            newZ = m.Z;
            return false;
        }

        public static bool CheckMovement(Mobile m, Map map, Point3D loc, Direction d, out int newZ)
        {
            if (Impl != null)
            {
                return Impl.CheckMovement(m, map, loc, d, out newZ);
            }

            newZ = m.Z;
            return false;
        }

        public static void Offset(Direction d, ref int x, ref int y)
        {
            switch (d & Direction.Mask)
            {
                case Direction.North:
                    --y;
                    break;
                case Direction.South:
                    ++y;
                    break;
                case Direction.West:
                    --x;
                    break;
                case Direction.East:
                    ++x;
                    break;
                case Direction.Right:
                    ++x;
                    --y;
                    break;
                case Direction.Left:
                    --x;
                    ++y;
                    break;
                case Direction.Down:
                    ++x;
                    ++y;
                    break;
                case Direction.Up:
                    --x;
                    --y;
                    break;
            }
        }
    }

    public interface IMovementImpl
    {
        bool CheckMovement(Mobile m, Direction d, out int newZ);
        bool CheckMovement(Mobile m, Map map, Point3D loc, Direction d, out int newZ);
    }
}
