using System;
using Server;
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
	public class GuildDismissGump : GuildMobileListGump
	{
		public GuildDismissGump ( Mobile from, Guild guild ) : base( from, guild, true, guild.Members )
		{
		}

		protected override void Design()
		{
			AddHtmlLocalized( 20, 10, 400, 35, 1011124, false, false ); // Whom do you wish to dismiss?

			AddButton( 20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 400, 245, 30, 1011125, false, false ); // Kick them out!

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
						Mobile m = (Mobile)m_List[index];

						if ( m != null && !m.Deleted )
						{
							m_Guild.RemoveMember( m );

							if ( m_Mobile.AccessLevel >= AccessLevel.GameMaster || m_Mobile == m_Guild.Leader )
							{
								GuildGump.EnsureClosed( m_Mobile );
								m_Mobile.SendGump( new GuildmasterGump( m_Mobile, m_Guild ) );
							}
						}
					}
				}
			}
			else if ( info.ButtonID == 2 && (m_Mobile.AccessLevel >= AccessLevel.GameMaster || m_Mobile == m_Guild.Leader) )
			{
				GuildGump.EnsureClosed( m_Mobile );
				m_Mobile.SendGump( new GuildmasterGump( m_Mobile, m_Guild ) );
			}
		}
	}
}