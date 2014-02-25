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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using Server;

namespace Server.Network
{
	public class Listener : IDisposable
	{
		private Socket m_Listener;

		private Queue<Socket> m_Accepted;
		private object m_AcceptedSyncRoot;

#if NewAsyncSockets
		private SocketAsyncEventArgs m_EventArgs;
#else
		private AsyncCallback m_OnAccept;
#endif

		private static Socket[] m_EmptySockets = new Socket[0];

		private static IPEndPoint[] m_EndPoints;

		public static IPEndPoint[] EndPoints {
			get { return m_EndPoints; }
			set { m_EndPoints = value; }
		}

		public Listener( IPEndPoint ipep )
		{
			m_Accepted = new Queue<Socket>();
			m_AcceptedSyncRoot = ((ICollection)m_Accepted).SyncRoot;

			m_Listener = Bind( ipep );

			if ( m_Listener == null )
				return;

			DisplayListener();

#if NewAsyncSockets
			m_EventArgs = new SocketAsyncEventArgs();
			m_EventArgs.Completed += new EventHandler<SocketAsyncEventArgs>( Accept_Completion );
			Accept_Start();
#else
			m_OnAccept = new AsyncCallback( OnAccept );
			try {
				IAsyncResult res = m_Listener.BeginAccept( m_OnAccept, m_Listener );
			} catch ( SocketException ex ) {
				NetState.TraceException( ex );
			} catch ( ObjectDisposedException ) {
			}
#endif
		}

		private Socket Bind( IPEndPoint ipep )
		{
			Socket s = new Socket( ipep.AddressFamily, SocketType.Stream, ProtocolType.Tcp );

			try
			{
				s.LingerState.Enabled = false;
				s.ExclusiveAddressUse = false;

				s.Bind( ipep );
				s.Listen( 8 );

				return s;
			}
			catch ( Exception e )
			{
				if ( e is SocketException ) {
					SocketException se = (SocketException)e;

					if ( se.ErrorCode == 10048 ) { // WSAEADDRINUSE
						Console.WriteLine( "Listener Failed: {0}:{1} (In Use)", ipep.Address, ipep.Port ); 
					}
					else if ( se.ErrorCode == 10049 ) { // WSAEADDRNOTAVAIL
						Console.WriteLine( "Listener Failed: {0}:{1} (Unavailable)", ipep.Address, ipep.Port );
					}
					else {
						Console.WriteLine( "Listener Exception:" );
						Console.WriteLine( e );
					}
				}

				return null;
			}
		}

		private void DisplayListener()
		{
			IPEndPoint ipep = m_Listener.LocalEndPoint as IPEndPoint;

			if ( ipep == null )
				return;

			if ( ipep.Address.Equals( IPAddress.Any ) || ipep.Address.Equals( IPAddress.IPv6Any ) ) {
				NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
				foreach ( NetworkInterface adapter in adapters ) {
					IPInterfaceProperties properties = adapter.GetIPProperties();
					foreach ( IPAddressInformation unicast in properties.UnicastAddresses ) {
						if ( ipep.AddressFamily == unicast.Address.AddressFamily )
							Console.WriteLine( "Listening: {0}:{1}", unicast.Address, ipep.Port );
					}
				}
				/*
				try {
					Console.WriteLine( "Listening: {0}:{1}", IPAddress.Loopback, ipep.Port );
					IPHostEntry iphe = Dns.GetHostEntry( Dns.GetHostName() );
					IPAddress[] ip = iphe.AddressList;
					for ( int i = 0; i < ip.Length; ++i )
						Console.WriteLine( "Listening: {0}:{1}", ip[i], ipep.Port );
				}
				catch { }
				*/
			}
			else {
				Console.WriteLine( "Listening: {0}:{1}", ipep.Address, ipep.Port );
			}
		}

#if NewAsyncSockets
		private void Accept_Start()
		{
			bool result = false;

			do {
				try {
					result = !m_Listener.AcceptAsync( m_EventArgs );
				} catch ( SocketException ex ) {
					NetState.TraceException( ex );
					break;
				} catch ( ObjectDisposedException ) {
					break;
				}

				if ( result )
					Accept_Process( m_EventArgs );
			} while ( result );
		}

		private void Accept_Completion( object sender, SocketAsyncEventArgs e )
		{
			Accept_Process( e );

			Accept_Start();
		}

		private void Accept_Process( SocketAsyncEventArgs e )
		{
			if ( e.SocketError == SocketError.Success && VerifySocket( e.AcceptSocket ) ) {
				Enqueue( e.AcceptSocket );
			} else {
				Release( e.AcceptSocket );
			}

			e.AcceptSocket = null;
		}

#else

		private void OnAccept( IAsyncResult asyncResult ) {
			Socket listener = (Socket) asyncResult.AsyncState;

			Socket accepted = null;

			try {
				accepted = listener.EndAccept( asyncResult );
			} catch ( SocketException ex ) {
				NetState.TraceException( ex );
			} catch ( ObjectDisposedException ) {
				return;
			}

			if ( accepted != null ) {
				if ( VerifySocket( accepted ) ) {
					Enqueue( accepted );
				} else {
					Release( accepted );
				}
			}

			try {
				listener.BeginAccept( m_OnAccept, listener );
			} catch ( SocketException ex ) {
				NetState.TraceException( ex );
			} catch ( ObjectDisposedException ) {
			}
		}
#endif

		private bool VerifySocket( Socket socket ) {
			try {
				SocketConnectEventArgs args = new SocketConnectEventArgs( socket );

				EventSink.InvokeSocketConnect( args );

				return args.AllowConnection;
			} catch ( Exception ex ) {
				NetState.TraceException( ex );

				return false;
			}
		}

		private void Enqueue( Socket socket ) {
			lock ( m_AcceptedSyncRoot ) {
				m_Accepted.Enqueue( socket );
			}

			Core.Set();
		}

		private void Release( Socket socket ) {
			try {
				socket.Shutdown( SocketShutdown.Both );
			} catch ( SocketException ex ) {
				NetState.TraceException( ex );
			}

			try {
				socket.Close();
			} catch ( SocketException ex ) {
				NetState.TraceException( ex );
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

		public void Dispose(bool disposing)
		{
			if (disposing)
			{
				Socket socket = Interlocked.Exchange<Socket>(ref m_Listener, null);

				if (socket != null)
				{
					socket.Close();
				}
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}