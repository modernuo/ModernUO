/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingMovementPackets.cs                                      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network
{
    public static class IncomingMovementPackets
    {
        public static void Configure()
        {
            IncomingPackets.Register(0x02, 7, true, MovementReq);
        }

        public static void MovementReq(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            var dir = (Direction)reader.ReadByte();
            int seq = reader.ReadByte();

            if (!state.RemoveKey(reader.ReadUInt32()) || state.Sequence == 0 && seq != 0 || !from.Move(dir))
            {
                state.SendMovementRej(seq, from);
                state.Sequence = 0;
            }
            else
            {
                ++seq;

                if (seq == 256)
                {
                    seq = 1;
                }

                state.Sequence = seq;
            }
        }
    }
}
