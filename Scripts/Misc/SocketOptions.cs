using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Misc;
using Server.Network;

namespace Server
{
	public class SocketOptions
	{
		private const bool NagleEnabled = false; // Should the Nagle algorithm be enabled? This may reduce performance
		private const int CoalesceBufferSize = 512; // MSS that the core will use when buffering packets
		private const int PooledSockets = 32; // The number of sockets to initially pool. Ideal value is expected client count. 

		private static IPEndPoint[] m_ListenerEndPoints = new IPEndPoint[] {
			new IPEndPoint( IPAddress.Any, 2593 ), // Default: Listen on port 2593 on all IP addresses
			
			// Examples:
			// new IPEndPoint( IPAddress.Any, 80 ), // Listen on port 80 on all IP addresses
			// new IPEndPoint( IPAddress.Parse( "1.2.3.4" ), 2593 ), // Listen on port 2593 on IP address 1.2.3.4
		};

		public static void Initialize()
		{
			SendQueue.CoalesceBufferSize = CoalesceBufferSize;
			SocketPool.InitialCapacity = PooledSockets;

			EventSink.SocketConnect += new SocketConnectEventHandler( EventSink_SocketConnect );

			Listener.EndPoints = m_ListenerEndPoints;
		}

		private static void EventSink_SocketConnect( SocketConnectEventArgs e )
		{
			if ( !e.AllowConnection )
				return;

			if ( !NagleEnabled )
				e.Socket.SetSocketOption( SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1 ); // RunUO uses its own algorithm
		}
	}
}