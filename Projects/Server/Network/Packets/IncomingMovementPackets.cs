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

namespace Server.Network;

public static class IncomingMovementPackets
{
    public static void Configure()
    {
        IncomingPackets.Register(0x02, 7, true, MovementReq);
        // Not used by OSI, and interferes with ClassicUO/Razor protocol extensions
        // IncomingPackets.Register(0xF0, 0, true, NewMovementReq);
        // IncomingPackets.Register(0xF1, 9, true, TimeSyncReq);
    }

    public static void NewMovementReq(NetState ns, CircularBufferReader reader)
    {
        var from = ns.Mobile;

        if (from == null)
        {
            return;
        }

        var steps = reader.ReadByte();
        for (int i = 0; i < steps; i++)
        {
            var t1 = reader.ReadUInt64(); // start time?
            var t2 = reader.ReadUInt64(); // end time?
            int seq = reader.ReadByte();
            var dir = (Direction)reader.ReadByte();
            var mode = reader.ReadInt32(); // 1 = walk, 2 = run
            if (mode == 2)
            {
                dir |= Direction.Running;
            }

            // Location
            reader.ReadInt32(); // x
            reader.ReadInt32(); // y
            reader.ReadInt32(); // z

            if (ns.Sequence == 0 && seq != 0 || !from.Move(dir))
            {
                ns.SendMovementRej(seq, from);
                ns.Sequence = 0;
            }
            else
            {
                ++seq;

                if (seq == 256)
                {
                    seq = 1;
                }

                ns.Sequence = seq;
            }
        }
    }

    public static void TimeSyncReq(NetState ns, CircularBufferReader reader)
    {
        reader.ReadUInt64(); // Client Time?

        ns.SendTimeSyncResponse();
    }

    public static void MovementReq(NetState state, CircularBufferReader reader, int packetLength)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        var dir = (Direction)reader.ReadByte();
        int seq = reader.ReadByte();
        var key = reader.ReadUInt32();

        if (state.Sequence == 0 && seq != 0 || !from.Move(dir))
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
