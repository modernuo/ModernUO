/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PacketHandler.cs                                                *
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

namespace Server.Network;

public unsafe class PacketHandler
{
    private readonly int _length;

    public PacketHandler(
        int packetID, delegate*<NetState, SpanReader, void> onReceive,
        int length = 0, bool inGameOnly = false, bool outGameOnly = false
    ) : this(packetID, length, inGameOnly, outGameOnly, onReceive)
    {

    }

    public PacketHandler(int packetID, int length, bool inGameOnly, bool outGameOnly, delegate*<NetState, SpanReader, void> onReceive)
    {
        _length = length;
        PacketID = packetID;
        InGameOnly = inGameOnly;
        OutOfGameOnly = outGameOnly;
        OnReceive = onReceive;
    }

    public int PacketID { get; }

    public virtual int GetLength(NetState ns) => _length;

    public delegate*<NetState, SpanReader, void> OnReceive { get; }

    public delegate*<int, NetState, bool> ThrottleCallback { get; set; }

    public bool InGameOnly { get; }

    public bool OutOfGameOnly { get; }
}
