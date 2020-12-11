/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingVendorBuyPackets.cs                                     *
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
using Server.Items;

namespace Server.Network
{
    public static class OutgoingVendorBuyPackets
    {
        public static void SendVendorBuyContent(this NetState ns, List<BuyItemState> list)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x3C); // Packet ID
            writer.Seek(2, SeekOrigin.Current);

            writer.Write((short)list.Count);

            for (var i = list.Count - 1; i >= 0; --i)
            {
                var bis = list[i];

                writer.Write(bis.MySerial);
                writer.Write((ushort)bis.ItemID);
                writer.Write((byte)0); // itemID offset
                writer.Write((ushort)bis.Amount);
                writer.Write((short)(i + 1)); // x
                writer.Write((short)1);  // y
                if (ns.ContainerGridLines)
                {
                    writer.Write((byte)0); // Grid Location?
                }
                writer.Write(bis.ContainerSerial);
                writer.Write((ushort)bis.Hue);
            }

            writer.WritePacketLength();
            ns.Send(ref buffer, writer.Position);
        }

        public static void SendDisplayBuyList(this NetState ns, Serial vendor)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x24); // Packet ID
            writer.Write(vendor);
            writer.Write((short)0x30); // Vendor Buy Window
            if (ns.HighSeas)
            {
                writer.Write((short)0x0);
            }

            ns.Send(ref buffer, writer.Position);
        }

        public static void SendVendorBuyList(this NetState ns, Mobile vendor, List<BuyItemState> list)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x74); // Packet ID
            writer.Seek(2, SeekOrigin.Current);
            writer.Write((vendor.FindItemOnLayer(Layer.ShopBuy) as Container)?.Serial ?? Serial.MinusOne);
            writer.Write((byte)list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                var bis = list[i];

                writer.Write(bis.Price);

                var desc = bis.Description ?? "";

                writer.Write((byte)desc.Length);
                writer.WriteAscii(desc); // Doesn't look like it is used anymore
            }

            writer.WritePacketLength();

            ns.Send(ref buffer, writer.Position);
        }

        public static void SendEndVendorBuy(this NetState ns, Serial vendor)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x3B); // Packet ID
            writer.Write((ushort)8);
            writer.Write(vendor);
            writer.Write((byte)0); // Buy count

            ns.Send(ref buffer, writer.Position);
        }
    }
}
