/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingVendorSellPackets.cs                                    *
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
using System.Collections.Generic;
using System.IO;

namespace Server.Network
{
    public static class OutgoingVendorSellPackets
    {
        public static void SendVendorSellList(this NetState ns, Serial vendor, List<SellItemState> list)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x9E); // Packet ID
            writer.Seek(2, SeekOrigin.Current);

            writer.Write(vendor);
            writer.Write((ushort)list.Count);

            for (var i = 0; i < list.Count; i++)
            {
                var sis = list[i];
                var item = sis.Item;
                writer.Write(item.Serial);
                writer.Write((ushort)item.ItemID);
                writer.Write((ushort)item.Hue);
                writer.Write((ushort)item.Amount);
                writer.Write((ushort)sis.Price);

                var name = (item.Name?.Trim()).DefaultIfNullOrEmpty(sis.Name ?? "");

                writer.Write((ushort)name.Length);
                writer.WriteAscii(name);
            }

            writer.WritePacketLength();
            ns.Send(ref buffer, writer.Position);
        }

        public static void SendEndVendorSell(this NetState ns, Serial vendor)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x3B); // Packet ID
            writer.Write((ushort)8);
            writer.Write(vendor);
            writer.Write((byte)0);

            ns.Send(ref buffer, writer.Position);
        }
    }
}
