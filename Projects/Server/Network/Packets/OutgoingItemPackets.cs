/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
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

namespace Server.Network
{
    public static class OutgoingItemPackets
    {
        public const int MaxWorldItemPacketLength = 26;

        public static int CreateWorldItem(ref Span<byte> buffer, Item item)
        {
            var itemID = item is BaseMulti ? item.ItemID | 0x4000 : item.ItemID & 0x3FFF;
            var hasAmount = item.Amount != 0;
            var amount = item.Amount;
            var serial = hasAmount ? item.Serial | 0x80000000 : item.Serial & 0x7FFFFFFF;
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

            if (amount != 0)
            {
                writer.Write((ushort)amount);
            }

            writer.Write((ushort)x);
            writer.Write((ushort)y);

            if (direction != 0)
            {
                writer.Write((byte)direction);
            }

            writer.Write((sbyte)loc.Z);

            if (hue != 0)
            {
                writer.Write((ushort)hue);
            }

            if (flags != 0)
            {
                writer.Write((byte)flags);
            }

            return writer.Position;
        }

        public static void SendWorldItem(this NetState ns, Item item)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[MaxWorldItemPacketLength];

            var length = ns.StygianAbyss ?
                CreateWorldItemNew(ref buffer, item, ns.HighSeas) :
                CreateWorldItem(ref buffer, item);

            ns.Send(buffer.Slice(0, length));
        }

        public static int CreateWorldItemNew(ref Span<byte> buffer, Item item, bool isHS)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xF3); // Packet ID
            writer.Write((short)0x1); // command

            var itemID = item.ItemID;

            if (item is BaseMulti)
            {
                writer.Write((byte)2);
                writer.Write(item.Serial);
                writer.Write((short)(itemID & 0x3FFF));
                writer.Write((byte)0);
            }
            else
            {
                writer.Write((byte)0);
                writer.Write(item.Serial);
                writer.Write((short)(itemID & (isHS ? 0xFFFF : 0x7FFF)));
                writer.Write((byte)0);
            }

            var amount = item.Amount;
            writer.Write((short)amount); // Min
            writer.Write((short)amount); // Max

            var loc = item.Location;
            writer.Write((short)loc.X);
            writer.Write((short)loc.Y);
            writer.Write((sbyte)loc.Z);

            writer.Write((byte)item.Light);
            writer.Write((short)item.Hue);
            writer.Write((byte)item.GetPacketFlags());

            if (isHS)
            {
                writer.Write((short)0);
            }

            return writer.Position;
        }
    }
}
