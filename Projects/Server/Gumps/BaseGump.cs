/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseGump.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Network;
using System;
using System.Runtime.CompilerServices;

namespace Server.Gumps
{
    public abstract class BaseGump
    {
        private static Serial nextSerial = (Serial)1;

        public int TypeID { get; }
        public Serial Serial { get; protected set; }
        public abstract int Switches { get; }
        public abstract int TextEntries { get; }

        public BaseGump()
        {
            Serial = nextSerial++;
            TypeID = GetTypeId(GetType());
        }

        public abstract void SendTo(NetState ns);

        public virtual void OnResponse(NetState sender, in RelayInfo info)
        {
        }

        public virtual void OnServerClose(NetState owner)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetTypeId(Type type)
        {
            return type.FullName?.GetHashCode(StringComparison.Ordinal) ?? -1;
        }
    }
}
