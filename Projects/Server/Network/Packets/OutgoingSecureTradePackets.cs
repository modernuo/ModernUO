/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingSecureTradePackets.cs                                   *
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
using Server.Items;

namespace Server.Network;

public enum TradeFlag : byte
{
    Display,
    Close,
    Update,
    UpdateGold,
    UpdateLedger
}

public static class OutgoingSecureTradePackets
{
    public static void SendDisplaySecureTrade(
        this NetState ns, Mobile them, Container first, Container second, string name
    )
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[47]);
        writer.Write((byte)0x6F); // Packet ID
        writer.Write((ushort)47); // Length
        writer.Write((byte)TradeFlag.Display);
        writer.Write(them.Serial);
        writer.Write(first.Serial);
        writer.Write(second.Serial);
        writer.Write(true);

        writer.WriteAscii(name ?? "", 30);

        ns.Send(writer.Span);
    }

    public static void SendCloseSecureTrade(this NetState ns, Container cont)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[17]);
        writer.Write((byte)0x6F); // Packet ID
        writer.Write((ushort)17); // Length
        writer.Write((byte)TradeFlag.Close);
        writer.Write(cont.Serial);
        writer.Write(0);
        writer.Write(0);
        writer.Write(false);

        ns.Send(writer.Span);
    }

    public static void SendUpdateSecureTrade(this NetState ns, Container cont, bool first, bool second) =>
        ns.SendUpdateSecureTrade(cont, TradeFlag.Update, first ? 1 : 0, second ? 1 : 0);

    public static void SendUpdateSecureTrade(this NetState ns, Container cont, TradeFlag flag, int first, int second)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[17]);
        writer.Write((byte)0x6F); // Packet ID
        writer.Write((ushort)17); // Length
        writer.Write((byte)flag);
        writer.Write(cont.Serial);
        writer.Write(first);
        writer.Write(second);
        writer.Write(false);

        ns.Send(writer.Span);
    }

    public static void SendSecureTradeEquip(this NetState ns, Item item, Mobile m)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[ns.ContainerGridLines ? 21 : 20]);
        writer.Write((byte)0x25); // Packet ID
        writer.Write(item.Serial);
        writer.Write((short)item.ItemID);
        writer.Write((byte)0);
        writer.Write((short)item.Amount);
        writer.Write((short)item.X);
        writer.Write((short)item.Y);
        if (ns.ContainerGridLines)
        {
            writer.Write((byte)0);
        }
        writer.Write(m.Serial);
        writer.Write((short)item.Hue);

        ns.Send(writer.Span);
    }
}
