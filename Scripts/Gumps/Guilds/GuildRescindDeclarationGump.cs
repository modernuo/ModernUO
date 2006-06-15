using System;
using Server;
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
	public class GuildRescindDeclarationGump : GuildListGump
	{
		public GuildRescindDeclarationGump( Mobile from, Guild guild ) : base( from, guild, true, guild.WarDeclarations )
		{
		}

		protected override void Design()
		{
			AddHtmlLocalized( 20, 10, 400, 35, 1011150, false, false ); // Select the guild to rescind our invitations: 

			AddButton( 20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 400, 245, 30, 1011102, false, false ); // Rescind your war declarations.

			AddButton( 300, 400, 4005, 4007, 2, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 335, 400, 100, 35, 1011012, false, false ); // CANCEL
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			if ( GuildGump.BadLeader( m_Mobile, m_Guild ) )
				return;

			if ( info.ButtonID == 1 )
			{
				int[] switches = info.Switches;

				if ( switches.Length > 0 )
				{
					int index = switches[0];

					if ( index >= 0 && index < m_List.Count )
					{
						Guild g = (Guild)m_List[index];

						if ( g != null )
						{
							m_Guild.WarDeclarations.Remove( g );
							g.WarInvitations.Remove( m_Guild );

							GuildGump.EnsureClosed( m_Mobile );

							if ( m_Guild.WarDeclarations.Count > 0 )
								m_Mobile.SendGump( new GuildRescindDeclarationGump( m_Mobile, m_Guild ) );
							else
								m_Mobile.SendGump( new GuildmasterGump( m_Mobile, m_Guild ) );
						}
					}
				}
			}
			else if ( info.ButtonID == 2 )
			{
				GuildGump.EnsureClosed( m_Mobile );
				m_Mobile.SendGump( new GuildmasterGump( m_Mobile, m_Guild ) );
			}
		}
	}
}