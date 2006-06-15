using System;
using System.Collections;
using Server;
using Server.Guilds;
using Server.Network;
using Server.Factions;
using System.Collections.Generic;

namespace Server.Gumps
{
	public class GuildDeclareWarGump : GuildListGump
	{
		public GuildDeclareWarGump( Mobile from, Guild guild, List<Guild> list )
			: base( from, guild, true, list )
		{
		}

		protected override void Design()
		{
			AddHtmlLocalized( 20, 10, 400, 35, 1011065, false, false ); // Select the guild you wish to declare war on.

			AddButton( 20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 400, 245, 30, 1011068, false, false ); // Send the challenge!

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
						Guild g = m_List[index];

						if ( g != null )
						{
							if ( g == m_Guild )
							{
								m_Mobile.SendLocalizedMessage( 501184 ); // You cannot declare war against yourself!
							}
							else if ( (g.WarInvitations.Contains( m_Guild ) && m_Guild.WarDeclarations.Contains( g )) || m_Guild.IsWar( g ) )
							{
								m_Mobile.SendLocalizedMessage( 501183 ); // You are already at war with that guild.
							}
							else if( Faction.Find( m_Guild.Leader ) != null )
							{
								m_Mobile.SendLocalizedMessage( 1005288 ); // You cannot declare war while you are in a faction
							}
							else
							{
								if ( !m_Guild.WarDeclarations.Contains( g ) )
								{
									m_Guild.WarDeclarations.Add( g );
									m_Guild.GuildMessage( 1018019, true, "{0} ({1})", g.Name, g.Abbreviation ); // Guild Message: Your guild has sent an invitation for war:
								}

								if ( !g.WarInvitations.Contains( m_Guild ) )
								{
									g.WarInvitations.Add( m_Guild );
									g.GuildMessage( 1018021, true, "{0} ({1})", m_Guild.Name, m_Guild.Abbreviation ); // Guild Message: Your guild has received an invitation to war:
								}
							}

							GuildGump.EnsureClosed( m_Mobile );
							m_Mobile.SendGump( new GuildWarAdminGump( m_Mobile, m_Guild ) );
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