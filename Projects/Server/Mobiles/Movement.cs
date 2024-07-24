/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

using System.Runtime.CompilerServices;

namespace Server.Movement;

public static class Movement
{
    // Movement implementation algorithm
    public static IMovementImpl Impl { get; set; }
    public static int WalkFootDelay { get; set; }
    public static int RunFootDelay { get; set; }
    public static int WalkMountDelay { get; set; }
    public static int RunMountDelay { get; set; }
    public static int TurnDelay { get; set; }

    public static void Configure()
    {
        TurnDelay = ServerConfiguration.GetOrUpdateSetting("movement.delay.turn", 0);
        WalkFootDelay = ServerConfiguration.GetOrUpdateSetting("movement.delay.walkFoot", 400);
        RunFootDelay = ServerConfiguration.GetOrUpdateSetting("movement.delay.runFoot", 200);
        WalkMountDelay = ServerConfiguration.GetOrUpdateSetting("movement.delay.walkMount", 200);
        RunMountDelay = ServerConfiguration.GetOrUpdateSetting("movement.delay.runMount", 100);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Offset(Direction d, ref Point3D p, int count = 1)
    {
        var x = p.m_X;
        var y = p.m_Y;
        Offset(d, ref x, ref y, count);
        p = new Point3D(x, y, p.m_Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Offset(Direction d, ref int x, ref int y, int count = 1)
    {
        switch (d & Direction.Mask)
        {
            case Direction.North:
                {
                    y -= count;
                    break;
                }
            case Direction.South:
                {
                    y += count;
                    break;
                }
            case Direction.West:
                {
                    x -= count;
                    break;
                }
            case Direction.East:
                {
                    x += count;
                    break;
                }
            case Direction.Right:
                {
                    x += count;
                    y -= count;
                    break;
                }
            case Direction.Left:
                {
                    x -= count;
                    y += count;
                    break;
                }
            case Direction.Down:
                {
                    x += count;
                    y += count;

                    break;
                }
            case Direction.Up:
                {
                    x -= count;
                    y -= count;
                    break;
                }
        }
    }
}

public interface IMovementImpl
{
    bool CheckMovement(Mobile m, Direction d, out int newZ);
    bool CheckMovement(Mobile m, Map map, Point3D loc, Direction d, out int newZ);
}
