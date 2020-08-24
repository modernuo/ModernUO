/***************************************************************************
 *                          EncodedPacketHandler.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

namespace Server.Network
{
    public delegate void OnEncodedPacketReceive(NetState state, IEntity ent, EncodedReader pvSrc);

    public class EncodedPacketHandler
    {
        public EncodedPacketHandler(int packetID, bool ingame, OnEncodedPacketReceive onReceive)
        {
            PacketID = packetID;
            Ingame = ingame;
            OnReceive = onReceive;
        }

        public int PacketID { get; }

        public OnEncodedPacketReceive OnReceive { get; }

        public bool Ingame { get; }
    }
}
