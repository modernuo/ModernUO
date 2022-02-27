/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingDamagePackets.cs                                        *
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

namespace Server.Network;

public static class OutgoingDamagePackets
{
    public static void SendDamage(this NetState ns, Serial serial, int amount)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[ns.DamagePacket ? 7 : 11]);

        if (ns.DamagePacket)
        {
            writer.Write((byte)0x0B); // Packet ID
            writer.Write(serial);
            writer.Write((ushort)Math.Clamp(amount, 0, 0xFFFF));
        }
        else
        {
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)11); // Length
            writer.Write((ushort)0x22);
            writer.Write((byte)1);
            writer.Write(serial);
            writer.Write((byte)Math.Clamp(amount, 0, 0xFF));
        }

        ns.Send(writer.Span);
    }
}
