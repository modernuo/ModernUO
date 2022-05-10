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
using System.Runtime.CompilerServices;

namespace Server.Network;

public static class OutgoingMapPackets
{
    private static byte[] _mapPatchesPacket = new byte[41];

    public static void SendMapPatches(this NetState ns)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        if (_mapPatchesPacket[0] == 0)
        {
            var writer = new SpanWriter(_mapPatchesPacket);
            writer.Write((byte)0xBF);   // Packet ID
            writer.Write((ushort)41);   // Length
            writer.Write((ushort)0x18); // Subpacket
            writer.Write(4);

            for (int i = 0; i < 4; i++)
            {
                var map = Map.Maps[i];

                writer.Write(map?.Tiles.Patch.StaticBlocks ?? 0);
                writer.Write(map?.Tiles.Patch.LandBlocks ?? 0);
            }
        }

        ns.Send(_mapPatchesPacket);
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
