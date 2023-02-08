/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingCombatPackets.cs                                        *
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
using System.Runtime.CompilerServices;

namespace Server.Network;

public static class OutgoingCombatPackets
{
    public static void SendSwing(this NetState ns, Serial attacker, Serial defender)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[10]);
        writer.Write((byte)0x2F); // Packet ID
        writer.Write((byte)0);
        writer.Write(attacker);
        writer.Write(defender);

        ns.Send(writer.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void SendSetWarMode(this NetState ns, bool warmode) =>
        ns?.Send(stackalloc byte[] { 0x72, *(byte*)&warmode, 0x00, 0x32, 0x00 });

    public static void SendChangeCombatant(this NetState ns, Serial combatant)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[5]);
        writer.Write((byte)0xAA); // Packet ID
        writer.Write(combatant);

        ns.Send(writer.Span);
    }
}
