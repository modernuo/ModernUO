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

namespace Server.Network
{
    public static class OutgoingMapPackets
    {
        public static void SendMapPatches(this NetState ns)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)41); // Length
            writer.Write((ushort)0x18); // Subpacket
            writer.Write(4); // Map count?

            writer.Write(Map.Felucca.Tiles.Patch.StaticBlocks);
            writer.Write(Map.Felucca.Tiles.Patch.LandBlocks);

            writer.Write(Map.Trammel.Tiles.Patch.StaticBlocks);
            writer.Write(Map.Trammel.Tiles.Patch.LandBlocks);

            writer.Write(Map.Ilshenar.Tiles.Patch.StaticBlocks);
            writer.Write(Map.Ilshenar.Tiles.Patch.LandBlocks);

            writer.Write(Map.Malas.Tiles.Patch.StaticBlocks);
            writer.Write(Map.Malas.Tiles.Patch.LandBlocks);

            ns.Send(ref buffer, writer.Position);
        }

        public static void SendInvalidMap(this NetState ns)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            buffer[0] = 0xC6; // Packet ID

            ns.Send(ref buffer, 1);
        }

        public static void SendMapChange(this NetState ns, Map map)
        {
            if (ns == null || map == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xBF);   // Packet ID
            writer.Write((ushort)6);   // Length
            writer.Write((ushort)0x08); // Subpacket
            writer.Write((byte)map.MapID);

            ns.Send(ref buffer, writer.Position);
        }
    }
}
