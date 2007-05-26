using System;
using Server;
using System.Diagnostics;
using System.IO;
using Server.Network;
using Server.Gumps;

namespace Server.Misc
{
	public class ClientVerification
	{
		private enum OldClientResponse
		{
			Ignore,
			Warn,
			Annoy,
			Kick
		}

		private static bool m_DetectClientRequirement = true;
		private static OldClientResponse m_OldClientResponse = OldClientResponse.Annoy;

		private static ClientVersion m_Required;
		private static bool m_AllowRegular = true, m_AllowUOTD = true, m_AllowGod = true;

		private static TimeSpan m_KickDelay = TimeSpan.FromSeconds( 10.0 );

		public static ClientVersion Required
		{
			get
			{
				return m_Required;
			}
			set
			{
				m_Required = value;
			}
		}

		public static bool AllowRegular
		{
			get
			{
				return m_AllowRegular;
			}
			set
			{
				m_AllowRegular = value;
			}
		}

		public static bool AllowUOTD
		{
			get
			{
				return m_AllowUOTD;
			}
			set
			{
				m_AllowUOTD = value;
			}
		}

		public static bool AllowGod
		{
			get
			{
				return m_AllowGod;
			}
			set
			{
				m_AllowGod = value;
			}
		}

		public static TimeSpan KickDelay
		{
			get
			{
				return m_KickDelay;
			}
			set
			{
				m_KickDelay = value;
			}
		}

		public static void Initialize()
		{
			EventSink.ClientVersionRecieved += new ClientVersionRecievedHandler( EventSink_ClientVersionRecieved );

			//ClientVersion.Required = null;
			//Required = new ClientVersion( "6.0.0.0" );

			if( m_DetectClientRequirement )
			{
				string path = Core.FindDataFile( "client.exe" );

				if( File.Exists( path ) )
				{
					FileVersionInfo info = FileVersionInfo.GetVersionInfo( path );

					if ( info.FileMajorPart != 0 || info.FileMinorPart != 0 || info.FileBuildPart != 0 || info.FilePrivatePart != 0 )
					{
						Required = new ClientVersion( info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart );
					}
				}
			}

			if( Required != null )
			{
				Utility.PushColor( ConsoleColor.White );
				Console.WriteLine( "Restricting client version to {0}. Action to be taken: {1}", Required, m_OldClientResponse );
				Utility.PopColor();
			}
		}

		private static void EventSink_ClientVersionRecieved( ClientVersionRecievedArgs e )
		{
			string kickMessage = null;
			NetState state = e.State;
			ClientVersion version = e.Version;

			if( Required != null && m_OldClientResponse == OldClientResponse.Kick && version < Required )
			{
				kickMessage = String.Format( "This server requires your client version be at least {0}.", Required );
			}
			else if( !AllowGod || !AllowRegular || !AllowUOTD )
			{
				if( !AllowGod && version.Type == ClientType.God )
					kickMessage = "This server does not allow god clients to connect.";
				else if( !AllowRegular && version.Type == ClientType.Regular )
					kickMessage = "This server does not allow regular clients to connect.";
				else if( !AllowUOTD && state.IsUOTDClient )
					kickMessage = "This server does not allow UO:TD clients to connect.";

				if( !AllowGod && !AllowRegular && !AllowUOTD )
				{
					kickMessage = "This server does not allow any clients to connect.";
				}
				else if( AllowGod && !AllowRegular && !AllowUOTD && version.Type != ClientType.God )
				{
					kickMessage = "This server requires you to use the god client.";
				}
				else if( kickMessage != null )
				{
					if( AllowRegular && AllowUOTD )
						kickMessage += " You can use regular or UO:TD clients.";
					else if( AllowRegular )
						kickMessage += " You can use regular clients.";
					else if( AllowUOTD )
						kickMessage += " You can use UO:TD clients.";
				}
			}

			if( kickMessage != null )
			{
				state.Mobile.SendMessage( 0x22, kickMessage );
				state.Mobile.SendMessage( 0x22, "You will be disconnected in {0} seconds.", KickDelay.TotalSeconds );

				Timer.DelayCall( KickDelay, delegate
				{
					if( state.Socket != null )
					{
						Console.WriteLine( "Client: {0}: Disconnecting, bad version", state );
						state.Dispose();
					}
				} );
			}
			else if( Required != null && version < Required )
			{
				switch( m_OldClientResponse )
				{
					case OldClientResponse.Warn:
					{
						state.Mobile.SendMessage( 0x22, "Your client is out of date. Please update your client.", Required );
						state.Mobile.SendMessage( 0x22, "This server reccomends that your client version be at least {0}.", Required );
						break;
					}
					case OldClientResponse.Annoy:
					{
						SendAnnoyGump( state.Mobile );
						break;
					}
				}
			}
		}

		private static void SendAnnoyGump( Mobile m )
		{
			if( m.NetState != null && m.NetState.Version < Required )
			{
				Gump g = new WarningGump( 1060637, 30720, String.Format( "Your client is out of date. Please update your client.<br>This server reccomends that your client version be at least {0}.<br> <br>You are currently using version {1}.<br> <br>To patch, run UOPatch.exe inside your Ultima Online folder.", Required, m.NetState.Version ), 0xFFC000, 480, 360,
					delegate( Mobile mob, bool selection, object o )
					{
						m.SendMessage( "You will be reminded of this again." );
						Timer.DelayCall( TimeSpan.FromMinutes( 20 ), delegate { SendAnnoyGump( m ); } );
					}, null, false );

				g.Dragable = false;

				m.SendGump( g );
			}
		}
	}
}