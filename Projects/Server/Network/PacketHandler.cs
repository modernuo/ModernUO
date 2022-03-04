/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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

namespace Server.Network;

public delegate void OnPacketReceive(NetState state, CircularBufferReader reader, int packetLength);

public delegate bool ThrottlePacketCallback(int packetId, NetState state, out bool drop);

public class PacketHandler
{
    private int _length;

    public PacketHandler(int packetID, int length, bool ingame, OnPacketReceive onReceive)
    {
        _length = length;
        PacketID = packetID;
        Ingame = ingame;
        OnReceive = onReceive;
    }

    public int PacketID { get; }

    public virtual int GetLength(NetState ns) => _length;

    public OnPacketReceive OnReceive { get; }

    public ThrottlePacketCallback ThrottleCallback { get; set; }

    public bool Ingame { get; }
}
