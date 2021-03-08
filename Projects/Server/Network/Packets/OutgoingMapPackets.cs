/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingMapPackets.cs                                           *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Server.Network
{
    public static class OutgoingMapPackets
    {
        public static void SendMapPatches(this NetState ns)
        {
            if (ns == null || ns.ProtocolChanges >= ProtocolChanges.Version6000)
            {
                return;
            }

            int count;

            if (ns.HasFlag(ClientFlags.TerMur))
            {
                count = 6;
            }
            else if (ns.HasFlag(ClientFlags.Tokuno))
            {
                count = 5;
            }
            else if (ns.HasFlag(ClientFlags.Malas))
            {
                count = 4;
            }
            else if (ns.HasFlag(ClientFlags.Ilshenar))
            {
                count = 3;
            }
            else if (ns.HasFlag(ClientFlags.Trammel))
            {
                count = 2;
            }
            else if (ns.HasFlag(ClientFlags.Felucca))
            {
                count = 1;
            }
            else
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[9 + count * 8]);
            writer.Write((byte)0xBF); // Packet ID
            writer.Seek(2, SeekOrigin.Current);
            writer.Write((ushort)0x18); // Subpacket
            writer.Write(count);

            for (int i = 0; i < count; i++)
            {
                var map = Map.Maps[i];

                writer.Write(map.Tiles.Patch.StaticBlocks);
                writer.Write(map.Tiles.Patch.LandBlocks);
            }

            writer.WritePacketLength();

            ns.Send(writer.Span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendInvalidMap(this NetState ns) => ns?.Send(stackalloc byte[] { 0xC6 });

        public static void SendMapChange(this NetState ns, Map map)
        {
            if (map == null)
            {
                return;
            }

            ns?.Send(stackalloc byte[] { 0xBF, 0x00, 0x06, 0x00, 0x08, (byte)map.MapID });
        }
    }
}
