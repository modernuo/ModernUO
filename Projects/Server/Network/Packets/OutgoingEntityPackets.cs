/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingEntityPackets.cs                                        *
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

namespace Server.Network
{
    public static class OutgoingEntityPackets
    {
        public const int OPLPacketLength = 9;
        public const int RemoveEntityLength = 5;

        public static void CreateOPLInfo(ref Span<byte> buffer, Item item) =>
            CreateOPLInfo(ref buffer, item.Serial, item.PropertyList.Hash);

        public static void CreateOPLInfo(ref Span<byte> buffer, Serial serial, int hash)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xDC); // Packet ID
            writer.Write(serial);
            writer.Write(hash);
        }

        public static void SendOPLInfo(this NetState ns, IPropertyListObject obj) =>
            ns.SendOPLInfo(obj.Serial, obj.PropertyList.Hash);

        public static void SendOPLInfo(this NetState ns, Serial serial, int hash)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[OPLPacketLength];
            CreateOPLInfo(ref buffer, serial, hash);

            ns.Send(buffer);
        }

        public static void CreateRemoveEntity(ref Span<byte> buffer, Serial serial)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x1D); // Packet ID
            writer.Write(serial);
        }

        public static void SendRemoveEntity(this NetState ns, Serial serial)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[RemoveEntityLength];
            CreateRemoveEntity(ref buffer, serial);

            ns.Send(buffer);
        }
    }
}
