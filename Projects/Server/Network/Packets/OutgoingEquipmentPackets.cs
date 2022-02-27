/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingEquipmentPackets.cs                                     *
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

namespace Server.Network;

public class EquipInfoAttribute
{
    public EquipInfoAttribute(int number, int charges = -1)
    {
        Number = number;
        Charges = charges;
    }

    public int Number { get; }

    public int Charges { get; }
}

public static class OutgoingEquipmentPackets
{
    public static void SendDisplayEquipmentInfo(
        this NetState ns,
        Serial serial, int number, string crafterName, bool unidentified, List<EquipInfoAttribute> attrs
    )
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        crafterName = crafterName.DefaultIfNullOrEmpty("");

        var length = 17 +
                     (crafterName.Length > 0 ? 6 + crafterName.Length : 0) +
                     (unidentified ? 4 : 0) +
                     attrs.Count * 6;

        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0xBF); // Packet ID
        writer.Write((ushort)length);
        writer.Write((ushort)0x10); // Subpacket
        writer.Write(serial);
        writer.Write(number);

        if (crafterName.Length > 0)
        {
            writer.Write(-3); // crafted by

            writer.Write((ushort)crafterName.Length);
            writer.WriteAscii(crafterName);
        }

        if (unidentified)
        {
            writer.Write(-4);
        }

        for (var i = 0; i < attrs.Count; ++i)
        {
            var attr = attrs[i];
            writer.Write(attr.Number);
            writer.Write((short)attr.Charges);
        }

        writer.Write(-1);

        ns.Send(writer.Span);
    }

    public static void SendEquipUpdate(this NetState ns, Item item)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Serial parentSerial;

        var parent = item.Parent as Mobile;
        var hue = item.Hue;

        if (parent != null)
        {
            parentSerial = parent.Serial;

            if (parent.SolidHueOverride >= 0)
            {
                hue = parent.SolidHueOverride;
            }
        }
        else
        {
            Console.WriteLine("Warning: EquipUpdate on item with !(parent is Mobile)");
            parentSerial = Serial.Zero;
        }

        var writer = new SpanWriter(stackalloc byte[15]);
        writer.Write((byte)0x2E); // Packet ID
        writer.Write(item.Serial);
        writer.Write((short)item.ItemID);
        writer.Write((ushort)item.Layer);
        writer.Write(parentSerial);
        writer.Write((short)hue);

        ns.Send(writer.Span);
    }
}
