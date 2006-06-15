/***************************************************************************
 *                                Listener.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Server;

namespace Server.Network
{
	public class Listener : IDisposable
	{
		private Socket m_Listener;
		private bool m_Disposed;
		private int m_ThisPort;

		private Queue<Socket> m_Accepted;
		private object m_AcceptedSyncRoot;

		private AsyncCallback m_OnAccept;

		private static Socket[] m_EmptySockets = new Socket[0];

		public int UsedPort
		{
			get{ return m_ThisPort; }
		}

		private static int m_Port = 2593;

		public static int Port
		{
			get
			{
				return m_Port;
			}
			set
			{
				m_Port = value;
			}
		}

		public Listener( int port )
		{
			m_ThisPort = port;
			m_Disposed = false;
			m_Accepted = new Queue<Socket>();
			m_AcceptedSyncRoot = ((ICollection)m_Accepted).SyncRoot;
			m_OnAccept = new AsyncCallback( OnAccept );

			m_Listener = Bind( IPAddress.Any, port );

			try
			{
				IPHostEntry iphe = Dns.GetHostEntry( Dns.GetHostName() );

				Console.WriteLine( "Address: {0}:{1}", IPAddress.Loopback, port );

				IPAddress[] ip = iphe.AddressList;

				for ( int i = 0; i < ip.Length; ++i )
						Console.WriteLine( "Address: {0}:{1}", ip[i], port );
			}
			catch
			{
			}
		}

		private Socket Bind( IPAddress ip, int port )
		{
			IPEndPoint ipep = new IPEndPoint( ip, port );

			Socket s = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );

			try
			{
				s.LingerState.Enabled = false;
				s.ExclusiveAddressUse = false;

				s.Bind( ipep );
				s.Listen( 8 );

				IAsyncResult res = s.BeginAccept( m_OnAccept, s );

				return s;
			}
			catch ( Exception e )
			{
				Console.WriteLine( "Listener bind exception:" );
				Console.WriteLine( e );

				try { s.Shutdown( SocketShutdown.Both ); } 
				catch{}

				try { s.Close(); }
				catch{}

				return null;
			}
		}

		private void OnAccept( IAsyncResult asyncResult )
		{
			Socket listener = asyncResult.AsyncState as Socket;

			try
			{
				Socket socket = listener.EndAccept( asyncResult );

				if ( socket != null )
				{
					SocketConnectEventArgs e = new SocketConnectEventArgs( socket );
					EventSink.InvokeSocketConnect( e );

					if ( e.AllowConnection )
					{
						lock ( m_AcceptedSyncRoot )
							m_Accepted.Enqueue( socket );
					}
					else
					{
						try { socket.Shutdown( SocketShutdown.Both ); }
						catch { }

						try { socket.Close(); }
						catch { }
					}
				}
			}
			catch
			{
			}
			finally
			{
				IAsyncResult res = listener.BeginAccept( m_OnAccept, listener );
			}
		}

		public Socket[] Slice()
		{
			Socket[] array;

			lock ( m_AcceptedSyncRoot )
			{
				if ( m_Accepted.Count == 0 )
					return m_EmptySockets;

				array = m_Accepted.ToArray();
				m_Accepted.Clear();
			}

			return array;
		}

		public void Dispose()
		{
			if ( !m_Disposed )
			{
				m_Disposed = true;

				if ( m_Listener != null )
				{
					try { m_Listener.Shutdown( SocketShutdown.Both ); }
					catch {}

					try { m_Listener.Close(); }
					catch {}

					m_Listener = null;
				}
			}
		}
	}
}