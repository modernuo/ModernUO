/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MobilePackets.cs                                                *
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

namespace Server.Tests.Network
{
    public sealed class BondedStatus : Packet
    {
        public BondedStatus(Serial serial, bool bonded) : base(0xBF)
        {
            EnsureCapacity(11);

            Stream.Write((short)0x19);
            Stream.Write((byte)0);
            Stream.Write(serial);
            Stream.Write((byte)(bonded ? 1 : 0));
        }
    }
}
