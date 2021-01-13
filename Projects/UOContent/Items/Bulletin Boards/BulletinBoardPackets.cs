/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: BulletinBoardPackets.cs                                         *
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

namespace Server.Network
{
    public static class BulletinBoardPackets
    {
        public static void SendBBDisplayBoard(this NetState ns, BaseBulletinBoard board)
        {
            if (ns == null)
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[38]);
            writer.Write((byte)0x71);
            writer.Write((ushort)38);
            writer.Write((byte)0); // Command
            writer.Write(board.Serial);

            ns.Send(writer.Span);
        }
    }
}
