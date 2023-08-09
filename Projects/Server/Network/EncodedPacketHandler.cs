/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EncodedPacketHandler.cs                                         *
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

public unsafe class EncodedPacketHandler
{
    public EncodedPacketHandler(int packetID, bool ingame, delegate*<NetState, IEntity, EncodedReader, void> onReceive)
    {
        PacketID = packetID;
        Ingame = ingame;
        OnReceive = onReceive;
    }

    public int PacketID { get; }

    public delegate*<NetState, IEntity, EncodedReader, void> OnReceive { get; }

    public bool Ingame { get; }
}
