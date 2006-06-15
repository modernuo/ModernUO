using System;
using Server;
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
	public class GuildCandidatesGump : GuildMobileListGump
	{
		public GuildCandidatesGump( Mobile from, Guild guild ) : base( from, guild, false, guild.Candidates )
		{
		}

		protected override void Design()
		{
			AddHtmlLocalized( 20, 10, 500, 35, 1013030, false, false ); // <center> Candidates </center>

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