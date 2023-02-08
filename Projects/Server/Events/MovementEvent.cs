/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MovementEvent.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server;

public class MovementEventArgs : EventArgs
{
    private static readonly Queue<MovementEventArgs> m_Pool = new();

    public MovementEventArgs(Mobile mobile, Direction dir)
    {
        Mobile = mobile;
        Direction = dir;
    }

    public Mobile Mobile { get; private set; }

    public Direction Direction { get; private set; }

    public bool Blocked { get; set; }

    public static MovementEventArgs Create(Mobile mobile, Direction dir)
    {
        MovementEventArgs args;

        if (m_Pool.Count > 0)
        {
            args = m_Pool.Dequeue();

            args.Mobile = mobile;
            args.Direction = dir;
            args.Blocked = false;
        }
        else
        {
            args = new MovementEventArgs(mobile, dir);
        }

        return args;
    }

    public void Free()
    {
        m_Pool.Enqueue(this);
    }
}

public static partial class EventSink
{
    public static event Action<MovementEventArgs> Movement;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeMovement(MovementEventArgs e) => Movement?.Invoke(e);
}
