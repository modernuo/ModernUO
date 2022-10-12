/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingItemPackets.cs                                          *
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
using Server.Items;

namespace Server.Network;

public static class OutgoingItemPackets
{
    public static int CreateWorldItem(Span<byte> buffer, Item item)
    {
        if (buffer[0] != 0)
        {
            // This assumes the packet was sliced properly
            return buffer.Length;
        }

        var itemID = item is BaseMulti ? item.ItemID | 0x4000 : item.ItemID & 0x3FFF;
        var amount = item.Amount;
        var hasAmount = amount != 0;
        var serial = hasAmount ? item.Serial.Value | 0x80000000 : item.Serial.Value & 0x7FFFFFFF;
        var loc = item.Location;
        var hue = item.Hue;
        var flags = item.GetPacketFlags();
        var direction = (int)item.Direction;
        var hasDirection = direction != 0;
        var hasHue = hue != 0;
        var hasFlags = flags != 0;
        var x = hasDirection ? loc.X | 0x8000 : loc.X & 0x7FFF;
        var y = (loc.Y & 0x3FFF) | (hasHue ? 0x8000 : 0) | (hasFlags ? 0x4000 : 0);
        var length = 14 + (hasAmount ? 2 : 0) +
                     (hasDirection ? 1 : 0) +
                     (hasHue ? 2 : 0) +
                     (hasFlags ? 1 : 0);

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0x1A); // Packet ID
        writer.Write((ushort)length);
        writer.Write(serial);
        writer.Write((ushort)itemID);

        if (hasAmount)
        {
            writer.Write((ushort)amount);
        }

        writer.Write((ushort)x);
        writer.Write((ushort)y);

        if (hasDirection)
        {
            writer.Write((byte)direction);
        }

        writer.Write((sbyte)loc.Z);

        if (hasHue)
        {
            writer.Write((ushort)hue);
        }

        if (hasFlags)
        {
            writer.Write((byte)flags);
        }

        return writer.BytesWritten;
    }

    public static void SendWorldItem(this NetState ns, Item item)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> buffer = stackalloc byte[OutgoingEntityPackets.MaxWorldEntityPacketLength].InitializePacket();

        var length = ns.StygianAbyss ?
            OutgoingEntityPackets.CreateWorldEntity(buffer, item, ns.HighSeas) :
            CreateWorldItem(buffer, item);

        ns.Send(buffer[..length]);
    }
}
