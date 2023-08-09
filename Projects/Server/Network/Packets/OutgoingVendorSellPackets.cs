/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace Server.Network;

public static class OutgoingVendorSellPackets
{
    public static void SendVendorSellList(this NetState ns, Serial vendor, HashSet<SellItemState> set)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var maxLength = 9;
        foreach (var sis in set)
        {
            var item = sis.Item;
            maxLength += 14 + Math.Max(item.Name?.Length ?? 0, sis.Name?.Length ?? 0);
        }

        var writer = new SpanWriter(stackalloc byte[maxLength]);
        writer.Write((byte)0x9E); // Packet ID
        writer.Seek(2, SeekOrigin.Current);

        writer.Write(vendor);
        writer.Write((ushort)set.Count);

        foreach (var sis in set)
        {
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
        ns.Send(writer.Span);
    }

    public static void SendEndVendorSell(this NetState ns, Serial vendor)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[8]);
        writer.Write((byte)0x3B); // Packet ID
        writer.Write((ushort)8);
        writer.Write(vendor);
        writer.Write((byte)0);

        ns.Send(writer.Span);
    }
}
