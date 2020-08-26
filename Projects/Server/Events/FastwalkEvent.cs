/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2020 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: FastWalkEvent.cs                                                *
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
using Server.Network;

namespace Server
{
    public class FastWalkEventArgs : EventArgs
    {
        public FastWalkEventArgs(NetState state)
        {
            NetState = state;
            Blocked = false;
        }

        public NetState NetState { get; }

        public bool Blocked { get; set; }
    }

    public static partial class EventSink
    {
        public static event Action<FastWalkEventArgs> FastWalk;
        public static void InvokeFastWalk(FastWalkEventArgs e) => FastWalk?.Invoke(e);
    }
}
