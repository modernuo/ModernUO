using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Network;

namespace Server.Misc
{
	public class ServerList
	{
		/* Address:
		 * 
		 * The default setting, a value of 'null', will attempt to detect your IP address automatically:
		 * private const string Address = null;
		 * 
		 * This detection, however, does not work for servers behind routers. If you're running behind a router, put in your IP:
		 * private const string Address = "12.34.56.78";
		 * 
		 * If you need to resolve a DNS host name, you can do that too:
		 * private const string Address = "shard.host.com";
		 */

		public static readonly string Address = null;
		public static readonly string ServerName = "RunUO TC";

		public static readonly bool AutoDetect = true;

		public static void Initialize()
		{
			Listener.Port = 2593;

			if ( Address == null ) {
				if ( AutoDetect )
					AutoDetection();
			}
			else {
				Resolve( Address, out m_PublicAddress );
			}

			EventSink.ServerList += new ServerListEventHandler( EventSink_ServerList );
		}

		private static IPAddress m_PublicAddress;

		private static void EventSink_ServerList( ServerListEventArgs e )
		{
			try
			{
				NetState ns = e.State;
				Socket s = ns.Socket;

				IPEndPoint ipep = (IPEndPoint)s.LocalEndPoint;

				IPAddress localAddress = ipep.Address;	
				int localPort = ipep.Port; 

				if ( IsPrivateNetwork( localAddress ) ) {
					ipep = (IPEndPoint)s.RemoteEndPoint;
					if ( !IsPrivateNetwork( ipep.Address ) && m_PublicAddress != null )
						localAddress = m_PublicAddress;
				}

				e.AddServer( ServerName, new IPEndPoint( localAddress, localPort ) );
			}
			catch
			{
				e.Rejected = true;
			}
		}

		private static void AutoDetection()
		{
			if ( !HasPublicIPAddress() ) {
				Console.Write( "ServerList: Auto-detecting public IP address..." );
				m_PublicAddress = FindPublicAddress();
				
				if ( m_PublicAddress != null )
					Console.WriteLine( "done ({0})", m_PublicAddress.ToString() );
				else
					Console.WriteLine( "failed" );
			}
		}

		private static bool Resolve( string addr, out IPAddress outValue )
		{
            if ( IPAddress.TryParse( addr, out outValue ) )
                return true;

			try {
				IPHostEntry iphe = Dns.GetHostEntry( addr );

				if ( iphe.AddressList.Length > 0 ) {
					outValue = iphe.AddressList[iphe.AddressList.Length - 1];
					return true;
				}
			}
			catch {}

			return false;
		}

		private static bool HasPublicIPAddress()
		{
			IPHostEntry iphe = Dns.GetHostEntry( Dns.GetHostName() );

			IPAddress[] ips = iphe.AddressList;

			for ( int i = 0; i < ips.Length; ++i )
				if ( !IsPrivateNetwork( ips[i] ) )
					return true;

			return false;
		}

		private static bool IsPrivateNetwork( IPAddress ip )
		{
			// 10.0.0.0/8
			// 172.16.0.0/12
			// 192.168.0.0/16

			if ( Utility.IPMatch( "192.168.*", ip ) )
				return true;
			else if ( Utility.IPMatch( "10.*", ip ) )
				return true;
			else if ( Utility.IPMatch( "172.16-31.*", ip ) )
				return true;
			else
				return false;
		}

		private static IPAddress FindPublicAddress()
		{
			try {
				WebRequest req = HttpWebRequest.Create( "http://www.runuo.com/ip.php" );
				req.Timeout = 15000;

				WebResponse res = req.GetResponse();

				Stream s = res.GetResponseStream();

				StreamReader sr = new StreamReader( s ); 

				IPAddress ip = IPAddress.Parse( sr.ReadLine() );

				sr.Close();
				s.Close();
				res.Close();

				return ip;
			} catch {
				return null;
			}
		}
	}
}