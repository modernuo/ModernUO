/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingContainerPackets.cs                                     *
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
using System.IO;
using Server.Logging;

namespace Server.Network;

public static class OutgoingContainerPackets
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(OutgoingContainerPackets));

    public static void SendDisplaySpellbook(this NetState ns, Serial book) => ns.SendDisplayContainer(book, -1);

    public static void SendSpellbookContent(this NetState ns, Serial book, int graphic, int offset, ulong content)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        if (ObjectPropertyList.Enabled && ns.NewSpellbook)
        {
            ns.SendNewSpellbookContent(book, graphic, offset, content);
        }
        else
        {
            ns.SendOldSpellbookContent(book, offset, content);
        }
    }

    public static void SendNewSpellbookContent(this NetState ns, Serial book, int graphic, int offset, ulong content)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[23]);
        writer.Write((byte)0xBF);  // Packet ID
        writer.Write((ushort)23);  // Length
        writer.Write((short)0x1B); // Subpacket
        writer.Write((short)0x01); // Command

        writer.Write(book);
        writer.Write((short)graphic);
        writer.Write((short)offset);

        for (var i = 0; i < 8; ++i)
        {
            writer.Write((byte)(content >> (i * 8)));
        }

        ns.Send(writer.Span);
    }

    public static void SendOldSpellbookContent(this NetState ns, Serial book, int offset, ulong content)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var count = content.NumberOfSetBits();
        var length = 5 + count * (ns.ContainerGridLines ? 20 : 19);

        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0x3C); // Packet ID
        writer.Write((ushort)length);
        writer.Write((ushort)count);

        ulong mask = 1;
        for (var i = 0; i < 64; ++i, mask <<= 1)
        {
            if ((content & mask) != 0)
            {
                writer.Write(0x7FFFFFFF - i);
                writer.Write((ushort)0);            // child ItemID
                writer.Write((byte)0);              // ItemID offset
                writer.Write((ushort)(i + offset)); // Amount
                writer.Write(0);                    // X, Y
                if (ns.ContainerGridLines)
                {
                    writer.Write((byte)0); // Grid Location
                }
                writer.Write(book);
                writer.Write((short)0); // Quest Hue
            }
        }

        ns.Send(writer.Span);
    }

    public static void SendDisplayContainer(this NetState ns, Serial cont, int gumpId)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[ns.HighSeas ? 9 : 7]);
        writer.Write((byte)0x24); // Packet ID
        writer.Write(cont);
        writer.Write((ushort)gumpId);
        if (ns.HighSeas)
        {
            writer.Write((short)0x7D);
        }

        ns.Send(writer.Span);
    }

    public static void SendContainerContentUpdate(this NetState ns, Item item)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Serial parentSerial;

        if (item.Parent is Item parentItem)
        {
            parentSerial = parentItem.Serial;
        }
        else
        {
            logger.Warning(
                "ContainerContentUpdate on Item {Type} ({Serial}) where parent is not an Item",
                item.GetType().Name,
                item.Serial
            );

            parentSerial = Serial.Zero;
        }

        var writer = new SpanWriter(stackalloc byte[ns.ContainerGridLines ? 21 : 20]);
        writer.Write((byte)0x25); // Packet ID
        writer.Write(item.Serial);
        writer.Write((ushort)item.ItemID);
        writer.Write((byte)0); // signed, itemID offset
        writer.Write((ushort)Math.Min(item.Amount, ushort.MaxValue));
        writer.Write((short)item.X);
        writer.Write((short)item.Y);
        if (ns.ContainerGridLines)
        {
            writer.Write((byte)0); // Grid Location?
        }
        writer.Write(parentSerial);
        writer.Write((ushort)(item.QuestItem ? Item.QuestItemHue : item.Hue));

        ns.Send(writer.Span);
    }

    public static void SendContainerContent(this NetState ns, Mobile beholder, Item beheld)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var items = beheld.Items;
        var count = items.Count;

        var writer = new SpanWriter(stackalloc byte[5 + items.Count * (ns.ContainerGridLines ? 20 : 19)]);
        writer.Write((byte)0x3C);           // Packet ID
        writer.Seek(4, SeekOrigin.Current); // Length & written count

        var written = 0;

        for (var i = 0; i < count; ++i)
        {
            var child = items[i];

            if (!child.Deleted && beholder.CanSee(child))
            {
                var loc = child.Location;

                writer.Write(child.Serial);
                writer.Write((ushort)child.ItemID);
                writer.Write((byte)0); // signed, itemID offset
                writer.Write((ushort)Math.Min(child.Amount, ushort.MaxValue));
                writer.Write((short)loc.X);
                writer.Write((short)loc.Y);
                if (ns.ContainerGridLines)
                {
                    writer.Write((byte)0); // Grid Location?
                }
                writer.Write(beheld.Serial);
                writer.Write((ushort)(child.QuestItem ? Item.QuestItemHue : child.Hue));

                ++written;
            }
        }

        writer.Seek(1, SeekOrigin.Begin);
        writer.Write((ushort)writer.BytesWritten);
        writer.Write((ushort)written);
        writer.Seek(0, SeekOrigin.End);

        ns.Send(writer.Span);
    }
}
