using System.Collections.Generic;
using Server.Accounting;

namespace Server.Engines.Chat
{
	public class ChatUser
	{
		public ChatUser( Mobile m )
		{
			Mobile = m;
			Ignored = new List<ChatUser>();
			Ignoring = new List<ChatUser>();
		}

		public Mobile Mobile { get; }

		public List<ChatUser> Ignored { get; }

		public List<ChatUser> Ignoring { get; }

		public string Username
		{
			get
			{
				if ( Mobile.Account is Account acct )
					return acct.GetTag( "ChatName" );

				return null;
			}
			set
			{
				if ( Mobile.Account is Account acct )
					acct.SetTag( "ChatName", value );
			}
		}

		public Channel CurrentChannel { get; set; }

		public bool IsOnline => ( Mobile.NetState != null );

		public bool Anonymous { get; set; }

		public bool IgnorePrivateMessage { get; set; }

		public const char NormalColorCharacter = '0';
		public const char ModeratorColorCharacter = '1';
		public const char VoicedColorCharacter = '2';

		public char GetColorCharacter()
		{
			if ( CurrentChannel != null && CurrentChannel.IsModerator( this ) )
				return ModeratorColorCharacter;

			if ( CurrentChannel != null && CurrentChannel.IsVoiced( this ) )
				return VoicedColorCharacter;

			return NormalColorCharacter;
		}

		public bool CheckOnline()
		{
			if ( IsOnline )
				return true;

			RemoveChatUser( this );
			return false;
		}

		public void SendMessage( int number )
		{
			SendMessage( number, null, null );
		}

		public void SendMessage( int number, string param1 )
		{
			SendMessage( number, param1, null );
		}

		public void SendMessage( int number, string param1, string param2 )
		{
			if ( Mobile.NetState != null )
				Mobile.Send( new ChatMessagePacket( Mobile, number, param1, param2 ) );
		}

		public void SendMessage( int number, Mobile from, string param1, string param2 )
		{
			if ( Mobile.NetState != null )
				Mobile.Send( new ChatMessagePacket( from, number, param1, param2 ) );
		}

		public bool IsIgnored( ChatUser check )
		{
			return Ignored.Contains( check );
		}

		public bool IsModerator => ( CurrentChannel != null && CurrentChannel.IsModerator( this ) );

		public void AddIgnored( ChatUser user )
		{
			if ( IsIgnored( user ) )
			{
				SendMessage( 22, user.Username ); // You are already ignoring %1.
			}
			else
			{
				Ignored.Add( user );
				user.Ignoring.Add( this );

				SendMessage( 23, user.Username ); // You are now ignoring %1.
			}
		}

		public void RemoveIgnored( ChatUser user )
		{
			if ( IsIgnored( user ) )
			{
				Ignored.Remove( user );
				user.Ignoring.Remove( this );

				SendMessage( 24, user.Username ); // You are no longer ignoring %1.

				if ( Ignored.Count == 0 )
					SendMessage( 26 ); // You are no longer ignoring anyone.
			}
			else
			{
				SendMessage( 25, user.Username ); // You are not ignoring %1.
			}
		}

		private static List<ChatUser> m_Users = new List<ChatUser>();
		private static Dictionary<Mobile, ChatUser> m_Table = new Dictionary<Mobile, ChatUser>();

		public static ChatUser AddChatUser( Mobile from )
		{
			ChatUser user = GetChatUser( from );

			if ( user == null )
			{
				user = new ChatUser( from );

				m_Users.Add( user );
				m_Table[from] = user;

				Channel.SendChannelsTo( user );

				List<Channel> list = Channel.Channels;

				for ( int i = 0; i < list.Count; ++i )
				{
					Channel c = list[i];

					if ( c.AddUser( user ) )
						break;
				}

				//ChatSystem.SendCommandTo( user.m_Mobile, ChatCommand.AddUserToChannel, user.GetColorCharacter() + user.Username );
			}

			return user;
		}

		public static void RemoveChatUser( ChatUser user )
		{
			if ( user == null )
				return;

			for ( int i = 0; i < user.Ignoring.Count; ++i )
				user.Ignoring[i].RemoveIgnored( user );

			if ( m_Users.Contains( user ) )
			{
				ChatSystem.SendCommandTo( user.Mobile, ChatCommand.CloseChatWindow );

				user.CurrentChannel?.RemoveUser( user );

				m_Users.Remove( user );
				m_Table.Remove( user.Mobile );
			}
		}

		public static void RemoveChatUser( Mobile from )
		{
			ChatUser user = GetChatUser( from );

			RemoveChatUser( user );
		}

		public static ChatUser GetChatUser( Mobile from )
		{
			m_Table.TryGetValue( from, out ChatUser c );
			return c;
		}

		public static ChatUser GetChatUser( string username )
		{
			for ( int i = 0; i < m_Users.Count; ++i )
			{
				ChatUser user = m_Users[i];

				if ( user.Username == username )
					return user;
			}

			return null;
		}

		public static void GlobalSendCommand( ChatCommand command )
		{
			GlobalSendCommand( command, null, null, null );
		}

		public static void GlobalSendCommand( ChatCommand command, string param1 )
		{
			GlobalSendCommand( command, null, param1, null );
		}

		public static void GlobalSendCommand( ChatCommand command, string param1, string param2 )
		{
			GlobalSendCommand( command, null, param1, param2 );
		}

		public static void GlobalSendCommand( ChatCommand command, ChatUser initiator )
		{
			GlobalSendCommand( command, initiator, null, null );
		}

		public static void GlobalSendCommand( ChatCommand command, ChatUser initiator, string param1 )
		{
			GlobalSendCommand( command, initiator, param1, null );
		}

		public static void GlobalSendCommand( ChatCommand command, ChatUser initiator, string param1, string param2 )
		{
			for ( int i = 0; i < m_Users.Count; ++i )
			{
				ChatUser user = m_Users[i];

				if ( user == initiator )
					continue;

				if ( user.CheckOnline() )
					ChatSystem.SendCommandTo( user.Mobile, command, param1, param2 );
			}
		}
	}
}
