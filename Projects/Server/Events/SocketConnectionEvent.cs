/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SocketConnectionEvent.cs                                        *
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
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Server;

public class SocketConnectEventArgs
{
    public SocketConnectEventArgs(Socket c)
    {
        Connection = c;
        AllowConnection = true;
    }

    public Socket Connection { get; }

    public bool AllowConnection { get; set; }
}

public static partial class EventSink
{
    public static event Action<SocketConnectEventArgs> SocketConnect;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeSocketConnect(SocketConnectEventArgs e) => SocketConnect?.Invoke(e);
}
