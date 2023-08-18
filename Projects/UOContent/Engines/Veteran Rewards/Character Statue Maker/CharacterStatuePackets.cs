/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CharacterStatuePackets.cs                                       *
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
using Server.Network;

namespace Server.Engines.VeteranRewards
{
    public static class CharacterStatuePackets
    {
        public const int StatueAnimationPacketLength = 17;

        public static void CreateStatueAnimation(Span<byte> buffer, Serial serial, int status, int anim, int frame)
        {
            if (buffer[0] != 0)
            {
                return;
            }

            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)17);
            writer.Write((short)0x19);
            writer.Write((byte)0x5);
            writer.Write(serial);
            writer.Write((byte)0);
            writer.Write((byte)0xFF);
            writer.Write((byte)status);
            writer.Write((byte)0);
            writer.Write((byte)anim);
            writer.Write((byte)0);
            writer.Write((byte)frame);
        }

        public static void SendStatueAnimation(this NetState ns, Serial serial, int status, int anim, int frame)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[StatueAnimationPacketLength].InitializePacket();
            CreateStatueAnimation(buffer, serial, status, anim, frame);
            ns.Send(buffer);
        }
    }
}
