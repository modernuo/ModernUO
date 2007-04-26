/***************************************************************************
 *                               SocketPool.cs
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
using System.Collections.Generic;
using System.Net.Sockets;

namespace Server.Network
{
	public class SocketPool
	{
		private static bool m_Created = false;

		public static bool Created
		{
			get { return m_Created; }
		}

		private static int m_Misses = 0;
		private static int m_InitialCapacity = 32;

		public static int InitialCapacity
		{
			get { return m_InitialCapacity; }
			set {
				if ( m_Created )
					return;

				m_InitialCapacity = value;
			}
		}

		private static Queue<Socket> m_FreeSockets;

		public static void Create()
		{
			if ( m_Created )
				return;

			m_FreeSockets = new Queue<Socket>( m_InitialCapacity );
			
			for ( int i = 0; i < m_InitialCapacity; ++i )
				m_FreeSockets.Enqueue( new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) );
		
			m_Created = true;
		}

		public static void Destroy()
		{
			if ( !m_Created )
				return;

			while ( m_FreeSockets.Count > 0 )
				m_FreeSockets.Dequeue().Close();

			m_FreeSockets = null;
		}

		public static Socket AcquireSocket()
		{
			lock ( m_FreeSockets )
			{
				if ( m_FreeSockets.Count > 0 )
					return m_FreeSockets.Dequeue();

				++m_Misses;

				for ( int i = 0; i < m_InitialCapacity; ++i )
					m_FreeSockets.Enqueue( new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) );

				return m_FreeSockets.Dequeue();
			}
		}

		public static void ReleaseSocket( Socket s )
		{
			/*if ( s == null )
				return;

			s.Close();

			lock ( m_FreeSockets )
				m_FreeSockets.Enqueue( s );*/
		}
	}
}