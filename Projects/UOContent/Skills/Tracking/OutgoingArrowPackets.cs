/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingArrowPackets.cs                                         *
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

namespace Server.Network
{
    public static class OutgoingArrowPackets
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendCancelArrow(this NetState ns, int x, int y, Serial s) => ns.SendArrow(0, x, y, s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendSetArrow(this NetState ns, int x, int y, Serial s) => ns.SendArrow(1, x, y, s);

        public static void SendArrow(this NetState ns, byte command, int x, int y, Serial s)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[10]);
            writer.Write((byte)0xBA); // Packet ID
            writer.Write(command);

            if (ns.HighSeas)
            {
                writer.Write((short)x);
                writer.Write((short)y);
                writer.Write(s);
            }
            else if (command == 1)
            {
                writer.Write((short)x);
                writer.Write((short)y);
            }
            else
            {
                writer.Write((short)-1);
                writer.Write((short)-1);
            }

            ns.Send(writer.Span);
        }
    }
}
