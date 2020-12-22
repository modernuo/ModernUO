/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingMobilePackets.cs                                        *
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
    public static class OutgoingMobilePackets
    {
        public const int BondedStatusPacketLength = 11;
        public const int DeathAnimationPacketLength = 13;

        public static void CreateBondedStatus(ref Span<byte> buffer, Serial serial, bool bonded)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)11); // Length
            writer.Write((ushort)0x19); // Subpacket ID
            writer.Write((byte)0); // Command
            writer.Write(serial);
            writer.Write(bonded);
        }

        public static void SendBondedStatus(this NetState ns, Serial serial, bool bonded)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)11); // Length
            writer.Write((ushort)0x19); // Subpacket ID
            writer.Write((byte)0); // Command
            writer.Write(serial);
            writer.Write(bonded);

            ns.Send(ref buffer, writer.Position);
        }

        public static void CreateDeathAnimation(ref Span<byte> buffer, Serial killed, Serial corpse)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xAF); // Packet ID
            writer.Write(killed);
            writer.Write(corpse);
            writer.Write(0); // ??
        }

        public static void SendDeathAnimation(this NetState ns, Serial killed, Serial corpse)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> span = stackalloc byte[DeathAnimationPacketLength];
            CreateDeathAnimation(ref span, killed, corpse);
            ns.Send(span);
        }
    }
}
