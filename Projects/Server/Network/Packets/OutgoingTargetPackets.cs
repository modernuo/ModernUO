/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingTargetPackets.cs                                        *
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
using Server.Targeting;

namespace Server.Network;

public static class OutgoingTargetPackets
{
    public static void SendMultiTargetReq(this NetState ns, MultiTarget t)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[ns.HighSeas ? 30 : 26]);
        writer.Write((byte)0x99); // Packet ID
        writer.Write(t.AllowGround);
        writer.Write(t.TargetID);
        writer.Write((byte)t.Flags);
        writer.Clear(11);
        writer.Write((short)t.MultiID);
        writer.Write((short)t.Offset.X);
        writer.Write((short)t.Offset.Y);
        writer.Write((short)t.Offset.Z);
        if (ns.HighSeas)
        {
            writer.Write(0);
        }

        ns.Send(writer.Span);
    }

    public static void SendCancelTarget(this NetState ns) =>
        ns?.Send(stackalloc byte[]
        {
            0x6C, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0
        });

    public static void SendTargetReq(this NetState ns, Target t)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[19]);
        writer.Write((byte)0x6C); // Packet ID
        writer.Write(t.AllowGround);
        writer.Write(t.TargetID);
        writer.Write((byte)t.Flags);
        writer.Clear(12);

        ns.Send(writer.Span);
    }
}
