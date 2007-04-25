using System;
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

		public const string ServerName = "RunUO TC";

		public static void Initialize()
		{
			Listener.Port = 2593;

			EventSink.ServerList += new ServerListEventHandler( EventSink_ServerList );
		}

		public static void EventSink_ServerList( ServerListEventArgs e )
		{
			try
			{
				IPAddress ipAddr;

				if ( Resolve( Address != null && !IsLocalMachine( e.State ) ? Address : Dns.GetHostName(), out ipAddr ) )
					e.AddServer( ServerName, new IPEndPoint( ipAddr, Listener.Port ) );
				else
					e.Rejected = true;
			}
			catch
			{
				e.Rejected = true;
			}
		}

		public static bool Resolve( string addr, out IPAddress outValue )
		{

            if ( IPAddress.TryParse( addr, out outValue ) )
                return true;

			try
			{
				IPHostEntry iphe = Dns.GetHostEntry( addr );

				if ( iphe.AddressList.Length > 0 )
				{
					outValue = iphe.AddressList[iphe.AddressList.Length - 1];
					return true;
				}
			}
			catch
			{
			}

			outValue = IPAddress.None;
			return false;
		}

		private static bool IsLocalMachine( NetState state )
		{
			IPAddress theirAddress = state.Address;

			if ( IPAddress.IsLoopback( theirAddress ) )
				return true;

			bool contains = false;

			IPHostEntry iphe = Dns.GetHostEntry( Dns.GetHostName() );

			for ( int i = 0; !contains && i < iphe.AddressList.Length; ++i )
				contains = theirAddress.Equals( iphe.AddressList[i] );

			return contains;
		}
	}
}