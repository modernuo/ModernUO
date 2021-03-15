/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: UOGateway.cs                                                    *
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
using System.Text;
using Server.Misc;
using Server.Text;

namespace Server.Network
{
    public static class UOGateway
    {
        public static void Configure()
        {
            var enabled = ServerConfiguration.GetOrUpdateSetting("uogateway.enabled", true);

            if (enabled)
            {
                FreeshardProtocol.Register(0xFE, false, QueryCompactShardStats);
                FreeshardProtocol.Register(0xFF, false, QueryExtendedShardStats);
            }
        }

        public static void QueryCompactShardStats(NetState ns, CircularBufferReader reader, ref int packetLength)
        {
            ns.SendCompactShardStats(
                (uint)(Core.TickCount / 1000),
                TcpServer.Instances.Count,
                World.Items.Count,
                World.Mobiles.Count,
                GC.GetTotalMemory(false)
            );
        }

        public static void QueryExtendedShardStats(NetState ns, CircularBufferReader reader, ref int packetLength)
        {
            const long ticksInHour = 1000 * 60 * 60;
            ns.SendExtendedShardStats(
                ServerList.ServerName,
                (int)(Core.TickCount / ticksInHour),
                TcpServer.Instances.Count,
                World.Items.Count,
                World.Mobiles.Count,
                (int)(GC.GetTotalMemory(false) / 1024)
            );
        }

        public static void SendCompactShardStats(
            this NetState ns, uint age, int clients, int items, int mobiles, long mem
        )
        {
            if (ns == null)
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[27]);
            writer.Write((byte)0x51); // Packet ID
            writer.Write((ushort)27); // Length
            writer.Write(clients);
            writer.Write(items);
            writer.Write(mobiles);
            writer.Write(age);
            writer.Write(mem);

            ns.Send(writer.Span);
        }

        public static void SendExtendedShardStats(
            this NetState ns, string name, int age, int clients, int items, int mobiles, int mem
        )
        {
            if (ns == null)
            {
                return;
            }

            var str =
                $"ModernUO, Name={name}, Age={age}, Clients={clients}, Items={items}, Chars={mobiles}, Mem={mem}, Ver=2";

            var length = Encoding.UTF8.GetMaxByteCount(str.Length);

            Span<byte> span = stackalloc byte[length + 1];
            Encoding.UTF8.GetBytes(str, span);
            span[^1] = 0; // Terminator

            ns.Send(span);
        }
    }

    /**
     *     public sealed class CompactServerInfo : Packet
    {
        public CompactServerInfo()
            : base(0x51)
        {
            EnsureCapacity(1 + 2 + (4 * 4) + 8);

            m_Stream.Write(NetState.Instances.Count - 1);                      // Clients
            m_Stream.Write(World.Items.Count);                                 // Items
            m_Stream.Write(World.Mobiles.Count);                               // Mobiles
            m_Stream.Write((uint)(DateTime.UtcNow - Clock.ServerStart).TotalSeconds);  // Age (seconds)

            long memory = GC.GetTotalMemory(false);
            m_Stream.Write((uint)(memory >> 32));                                   // Memory high bytes
            m_Stream.Write((uint)memory);                                           // Memory low bytes
        }
    }
     */
}
