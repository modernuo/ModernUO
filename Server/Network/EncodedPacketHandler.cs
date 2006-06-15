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

using System;

namespace Server.Network
{
	public delegate void OnEncodedPacketReceive( NetState state, IEntity ent, EncodedReader pvSrc );

	public class EncodedPacketHandler
	{
		private int m_PacketID;
		private bool m_Ingame;
		private OnEncodedPacketReceive m_OnReceive;

		public EncodedPacketHandler( int packetID, bool ingame, OnEncodedPacketReceive onReceive )
		{
			m_PacketID = packetID;
			m_Ingame = ingame;
			m_OnReceive = onReceive;
		}

		public int PacketID
		{
			get
			{
				return m_PacketID;
			}
		}

		public OnEncodedPacketReceive OnReceive
		{
			get
			{
				return m_OnReceive;
			}
		}

		public bool Ingame
		{
			get
			{
				return m_Ingame;
			}
		}
	}
}