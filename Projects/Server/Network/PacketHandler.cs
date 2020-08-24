/***************************************************************************
 *                              PacketHandler.cs
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

using System;

namespace Server.Network
{
    public delegate void OnPacketReceive(NetState state, PacketReader pvSrc);

    public delegate TimeSpan ThrottlePacketCallback(NetState state);

    public class PacketHandler
    {
        public PacketHandler(int packetID, int length, bool ingame, OnPacketReceive onReceive)
        {
            PacketID = packetID;
            Length = length;
            Ingame = ingame;
            OnReceive = onReceive;
        }

        public int PacketID { get; }

        public int Length { get; }

        public OnPacketReceive OnReceive { get; }

        public ThrottlePacketCallback ThrottleCallback { get; set; }

        public bool Ingame { get; }
    }
}
