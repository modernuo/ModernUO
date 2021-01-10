/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: PartyPackets.cs                                                 *
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
using System.Runtime.CompilerServices;
using Server.Network;

namespace Server.Engines.PartySystem
{
    public static class PartyPackets
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPartyMemberListPacketLength(Party p) => 7 + p.Count * 4;

        public static void CreatePartyMemberList(Span<byte> buffer, Party p)
        {
            if (buffer[0] != 0)
            {
                return;
            }

            var length = GetPartyMemberListPacketLength(p);
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)length);
            writer.Write((ushort)0x06); // Sub-packet
            writer.Write((byte)0x01); // Command
            writer.Write((byte)p.Count);

            for (var i = 0; i < p.Count; ++i)
            {
                writer.Write(p[i].Mobile.Serial);
            }
        }

        public static void SendPartyMemberList(this NetState ns, Party p)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[GetPartyMemberListPacketLength(p)];
            CreatePartyMemberList(buffer, p);

            ns.Send(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPartyRemoveMemberPacketLength(Party p) => 11 + p?.Count * 4 ?? 0;

        public static void SendPartyRemoveMember(this NetState ns, Serial m, Party p = null)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[GetPartyRemoveMemberPacketLength(p)];
            CreatePartyRemoveMember(buffer, m, p);

            ns.Send(buffer);
        }

        public static void CreatePartyRemoveMember(Span<byte> buffer, Serial m, Party p = null)
        {
            if (buffer[0] != 0)
            {
                return;
            }

            var length = GetPartyMemberListPacketLength(p);
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)length);
            writer.Write((ushort)0x06); // Sub-packet
            writer.Write((byte)0x02);   // Command

            var count = p?.Count ?? 0;
            writer.Write((byte)count);

            for (var i = 0; i < count; ++i)
            {
                writer.Write(p![i].Mobile.Serial);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPartyTextMessagePacketLength(string text) => 12 + text?.Length * 2 ?? 0;

        public static void SendPartyTextMessage(this NetState ns, Serial m, string text, bool toAll)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[GetPartyTextMessagePacketLength(text)];
            CreatePartyTextMessage(buffer, m, text, toAll);

            ns.Send(buffer);
        }

        public static void CreatePartyTextMessage(Span<byte> buffer, Serial m, string text, bool toAll)
        {
            if (buffer[0] != 0)
            {
                return;
            }

            var length = GetPartyTextMessagePacketLength(text);
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)length);
            writer.Write((ushort)0x06); // Sub-packet
            writer.Write((byte)(toAll ? 0x04 : 0x03));
            writer.Write(m);
            writer.WriteBigUniNull(text);
        }

        public static void SendPartyInvitation(this NetState ns, Serial leader)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[10];
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)10);
            writer.Write((ushort)0x06); // Sub-packet
            writer.Write(0x07); // command
            writer.Write(leader);

            ns.Send(buffer);
        }
    }
}
