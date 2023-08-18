/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using Server.Items;

namespace Server.Network;

public static class OutgoingVendorBuyPackets
{
    public static void SendVendorBuyContent(this NetState ns, List<BuyItemState> list)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var length = 5 + list.Count * (ns.ContainerGridLines ? 20 : 19);
        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0x3C); // Packet ID
        writer.Write((ushort)length);
        writer.Write((short)list.Count);

        for (var i = list.Count - 1; i >= 0; --i)
        {
            var bis = list[i];

            writer.Write(bis.MySerial);
            writer.Write((ushort)bis.ItemID);
            writer.Write((byte)0); // itemID offset
            writer.Write((ushort)bis.Amount);
            writer.Write((short)(i + 1)); // x
            writer.Write((short)1);       // y
            if (ns.ContainerGridLines)
            {
                writer.Write((byte)0); // Grid Location?
            }
            writer.Write(bis.ContainerSerial);
            writer.Write((ushort)bis.Hue);
        }

        ns.Send(writer.Span);
    }

    public static void SendDisplayBuyList(this NetState ns, Serial vendor)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[ns.HighSeas ? 9 : 7]);
        writer.Write((byte)0x24); // Packet ID
        writer.Write(vendor);
        writer.Write((short)0x30); // Vendor Buy Window
        if (ns.HighSeas)
        {
            writer.Write((short)0x0);
        }

        ns.Send(writer.Span);
    }

    public static void SendVendorBuyList(this NetState ns, Mobile vendor, List<BuyItemState> list)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var length = 8;
        for (int i = 0; i < list.Count; i++)
        {
            length += 6 + list[i].Description?.Length ?? 0;
        }

        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0x74); // Packet ID
        writer.Write((ushort)length);
        writer.Write(vendor.FindItemOnLayer<Container>(Layer.ShopBuy)?.Serial ?? Serial.MinusOne);
        writer.Write((byte)list.Count);

        for (var i = 0; i < list.Count; ++i)
        {
            var bis = list[i];

            writer.Write(bis.Price);

            var desc = bis.Description ?? "";

            writer.Write((byte)(desc.Length + 1));
            writer.WriteAsciiNull(desc);
        }

        ns.Send(writer.Span);
    }

    public static void SendEndVendorBuy(this NetState ns, Serial vendor)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[8]);
        writer.Write((byte)0x3B); // Packet ID
        writer.Write((ushort)8);
        writer.Write(vendor);
        writer.Write((byte)0); // Buy count

        ns.Send(writer.Span);
    }
}
