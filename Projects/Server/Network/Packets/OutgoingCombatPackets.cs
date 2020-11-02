/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
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

namespace Server.Network
{
    public static class OutgoingCombatPackets
    {
        public static void SendSwing(this NetState ns, Serial attacker, Serial defender)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x2F); // Packet ID
            writer.Write((byte)0);
            writer.Write(attacker);
            writer.Write(defender);

            ns.Send(ref buffer, 10);
        }

        public static void SendSetWarMode(this NetState ns, bool warmode)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x72); // Packet ID
            // Warmode, 0x00, 0x32, 0x00
            writer.Write(warmode ? 0x01003200 : 0x00003200);

            ns.Send(ref buffer, 5);
        }

        public static void SendChangeCombatant(this NetState ns, Serial combatant)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xAA); // Packet ID
            writer.Write(combatant);

            ns.Send(ref buffer, 5);
        }
    }
}
