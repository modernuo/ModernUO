/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2020 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: ServerCrashedEvent.cs                                           *
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

namespace Server
{
    public class ServerCrashedEventArgs : EventArgs
    {
        public ServerCrashedEventArgs(Exception e) => Exception = e;

        public Exception Exception { get; }

        public bool Close { get; set; }
    }

    public static partial class EventSink
    {
        public static event Action<ServerCrashedEventArgs> ServerCrashed;
        public static void InvokeServerCrashed(ServerCrashedEventArgs e) => ServerCrashed?.Invoke(e);
    }
}
