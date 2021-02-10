/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: HousePackets.cs                                                 *
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
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Server.Network;

namespace Server.Multis
{
    public static class HousePackets
    {
        private const int MaxItemsPerStairBuffer = 750;

        public static void SendBeginHouseCustomization(this NetState ns, Serial house)
        {
            if (ns == null)
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[17]);
            writer.Write((byte)0xBF); // Packet Id
            writer.Write((ushort)17);
            writer.Write((short)0x20); // Sub-packet
            writer.Write(house);
            writer.Write((byte)0x04); // command
            writer.Write((ushort)0x0000);
            writer.Write((ushort)0xFFFF);
            writer.Write((ushort)0xFFFF);
            writer.Write((byte)0xFF);

            ns.Send(writer.Span);
        }

        public static void SendEndHouseCustomization(this NetState ns, Serial house)
        {
            if (ns == null)
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[17]);
            writer.Write((byte)0xBF); // Packet Id
            writer.Write((ushort)17);
            writer.Write((short)0x20); // Sub-packet
            writer.Write(house);
            writer.Write((byte)0x05); // command
            writer.Write((ushort)0x0000);
            writer.Write((ushort)0xFFFF);
            writer.Write((ushort)0xFFFF);
            writer.Write((byte)0xFF);

            ns.Send(writer.Span);
        }

        public static void SendDesignStateGeneral(this NetState ns, Serial house, int revision)
        {
            if (ns == null)
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[13]);
            writer.Write((byte)0xBF); // Packet Id
            writer.Write((ushort)13);
            writer.Write((short)0x1D); // Sub-packet
            writer.Write(house);
            writer.Write(revision);

            ns.Send(writer.Span);
        }

        public static void CreateDesignStateDetailed()
        {

        }
    }
}
