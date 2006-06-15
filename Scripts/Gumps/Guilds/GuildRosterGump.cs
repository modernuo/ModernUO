using System;
using Server;
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
	public class GuildRosterGump : GuildMobileListGump
	{
		public GuildRosterGump( Mobile from, Guild guild ) : base( from, guild, false, guild.Members )
		{
		}

		protected override void Design()
		{
			AddHtml( 20, 10, 500, 35, String.Format( "<center>{0}</center>", m_Guild.Name ), false, false );

			AddButton( 20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 400, 300, 35, 1011120, false, false ); // Return to the main menu.
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			if ( GuildGump.BadMember( m_Mobile, m_Guild ) )
				return;

			if ( info.ButtonID == 1 )
			{
				GuildGump.EnsureClosed( m_Mobile );
				m_Mobile.SendGump( new GuildGump( m_Mobile, m_Guild ) );
			}
		}
	}
}