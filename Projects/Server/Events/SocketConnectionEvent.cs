/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2020 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: SocketConnectionEvent.cs                                        *
 * Created: 2020/04/11 - Updated: 2020/04/11                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using Microsoft.AspNetCore.Connections;

namespace Server
{
    public class SocketConnectEventArgs : EventArgs
    {
        public SocketConnectEventArgs(ConnectionContext c)
        {
            Context = c;
            AllowConnection = true;
        }

        public ConnectionContext Context { get; }

        public bool AllowConnection { get; set; }
    }

    public static partial class EventSink
    {
        public static event Action<SocketConnectEventArgs> SocketConnect;
        public static void InvokeSocketConnect(SocketConnectEventArgs e) => SocketConnect?.Invoke(e);
    }
}
