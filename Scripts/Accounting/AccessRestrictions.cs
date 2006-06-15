using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Misc;

namespace Server
{
	public class AccessRestrictions
	{
		public static void Initialize()
		{
			EventSink.SocketConnect += new SocketConnectEventHandler( EventSink_SocketConnect );
		}
			
		private static void EventSink_SocketConnect( SocketConnectEventArgs e )
		{
			try
			{
				IPAddress ip = ((IPEndPoint)e.Socket.RemoteEndPoint).Address;

				if ( Firewall.IsBlocked( ip ) )
				{
					Console.WriteLine( "Client: {0}: Firewall blocked connection attempt.", ip );
					e.AllowConnection = false;
					return;
				}
				else if ( IPLimiter.SocketBlock && !IPLimiter.Verify( ip ) )
				{
					Console.WriteLine( "Client: {0}: Past IP limit threshold", ip );

					using ( StreamWriter op = new StreamWriter( "ipLimits.log", true ) )
						op.WriteLine( "{0}\tPast IP limit threshold\t{1}", ip, DateTime.Now );
	
					e.AllowConnection = false;
					return;
				}
			}
			catch
			{
				e.AllowConnection = false;
			}
		}
	}
}