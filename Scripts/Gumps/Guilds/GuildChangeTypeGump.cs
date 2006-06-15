using System;
using System.Collections;
using Server;
using Server.Guilds;
using Server.Network;
using Server.Factions;

namespace Server.Gumps
{
	public class GuildChangeTypeGump : Gump
	{
		private Mobile m_Mobile;
		private Guild m_Guild;

		public GuildChangeTypeGump( Mobile from, Guild guild ) : base( 20, 30 )
		{
			m_Mobile = from;
			m_Guild = guild;

			Dragable = false;

			AddPage( 0 );
			AddBackground( 0, 0, 550, 400, 5054 );
			AddBackground( 10, 10, 530, 380, 3000 );

			AddHtmlLocalized( 20, 15, 510, 30, 1013062, false, false ); // <center>Change Guild Type Menu</center>

			AddHtmlLocalized( 50, 50, 450, 30, 1013066, false, false ); // Please select the type of guild you would like to change to

			AddButton( 20, 100, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 85, 100, 300, 30, 1013063, false, false ); // Standard guild

			AddButton( 20, 150, 4005, 4007, 2, GumpButtonType.Reply, 0 );
			AddItem( 50, 143, 7109 );
			AddHtmlLocalized( 85, 150, 300, 300, 1013064, false, false ); // Order guild

			AddButton( 20, 200, 4005, 4007, 3, GumpButtonType.Reply, 0 );
			AddItem( 45, 200, 7107 );
			AddHtmlLocalized( 85, 200, 300, 300, 1013065, false, false ); // Chaos guild

			AddButton( 300, 360, 4005, 4007, 4, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 335, 360, 150, 30, 1011012, false, false ); // CANCEL
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			if ( GuildGump.BadLeader( m_Mobile, m_Guild ) )
				return;

			PlayerState pl = PlayerState.Find( m_Mobile );

			if ( pl != null )
			{
				m_Mobile.SendLocalizedMessage( 1010405 ); // You cannot change guild types while in a Faction!
			}
			else if ( m_Guild.TypeLastChange.AddDays( 7 ) > DateTime.Now )
			{
				m_Mobile.SendLocalizedMessage( 1005292 ); // Your guild type will be changed in one week.
			}
			else
			{

				GuildType newType;

				switch ( info.ButtonID )
				{
					default: return; // Close
					case 1: newType = GuildType.Regular; break;
					case 2: newType = GuildType.Order;   break;
					case 3: newType = GuildType.Chaos;   break;
				}

				if ( m_Guild.Type == newType )
					return;

				m_Guild.Type = newType;
				m_Guild.GuildMessage( 1018022, true, newType.ToString() ); // Guild Message: Your guild type has changed:
			}

			GuildGump.EnsureClosed( m_Mobile );
			m_Mobile.SendGump( new GuildmasterGump( m_Mobile, m_Guild ) );
		}
	}
}
