using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
	public class GuildmasterGump : Gump
	{
		private Mobile m_Mobile;
		private Guild m_Guild;

		public GuildmasterGump( Mobile from, Guild guild ) : base( 20, 30 )
		{
			m_Mobile = from;
			m_Guild = guild;

			Dragable = false;

			AddPage( 0 );
			AddBackground( 0, 0, 550, 400, 5054 );
			AddBackground( 10, 10, 530, 380, 3000 );

			AddHtmlLocalized( 20, 15, 510, 35, 1011121, false, false ); // <center>GUILDMASTER FUNCTIONS</center>

			AddButton( 20, 40, 4005, 4007, 2, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 40, 470, 30, 1011107, false, false ); // Set the guild name.

			AddButton( 20, 70, 4005, 4007, 3, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 70, 470, 30, 1011109, false, false ); // Set the guild's abbreviation.

			AddButton( 20, 100, 4005, 4007, 4, GumpButtonType.Reply, 0 );
			switch ( m_Guild.Type )
			{
				case GuildType.Regular:
					AddHtmlLocalized( 55, 100, 470, 30, 1013059, false, false ); // Change guild type: Currently Standard
					break;
				case GuildType.Order:
					AddHtmlLocalized( 55, 100, 470, 30, 1013057, false, false ); // Change guild type: Currently Order
					break;
				case GuildType.Chaos:
					AddHtmlLocalized( 55, 100, 470, 30, 1013058, false, false ); // Change guild type: Currently Chaos
					break;
			}

			AddButton( 20, 130, 4005, 4007, 5, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 130, 470, 30, 1011112, false, false ); // Set the guild's charter.

			AddButton( 20, 160, 4005, 4007, 6, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 160, 470, 30, 1011113, false, false ); // Dismiss a member.

			AddButton( 20, 190, 4005, 4007, 7, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 190, 470, 30, 1011114, false, false ); // Go to the WAR menu.

			if ( m_Guild.Candidates.Count > 0 )
			{
				AddButton( 20, 220, 4005, 4007, 8, GumpButtonType.Reply, 0 );
				AddHtmlLocalized( 55, 220, 470, 30, 1013056, false, false ); // Administer the list of candidates
			}
			else
			{
				AddImage( 20, 220, 4020 );
				AddHtmlLocalized( 55, 220, 470, 30, 1013031, false, false ); // There are currently no candidates for membership.
			}

			AddButton( 20, 250, 4005, 4007, 9, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 250, 470, 30, 1011117, false, false ); // Set the guildmaster's title.

			AddButton( 20, 280, 4005, 4007, 10, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 280, 470, 30, 1011118, false, false ); // Grant a title to another member.

			AddButton( 20, 310, 4005, 4007, 11, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 310, 470, 30, 1011119, false, false ); // Move this guildstone.

			AddButton( 20, 360, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 360, 245, 30, 1011120, false, false ); // Return to the main menu.

			AddButton( 300, 360, 4005, 4007, 0, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 335, 360, 100, 30, 1011441, false, false ); // EXIT
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			if ( GuildGump.BadLeader( m_Mobile, m_Guild ) )
				return;

			switch ( info.ButtonID )
			{
				case 1: // Main menu
				{
					GuildGump.EnsureClosed( m_Mobile );
					m_Mobile.SendGump( new GuildGump( m_Mobile, m_Guild ) );

					break;
				}
				case 2: // Set guild name
				{
					m_Mobile.SendLocalizedMessage( 1013060 ); // Enter new guild name (40 characters max):
					m_Mobile.Prompt = new GuildNamePrompt( m_Mobile, m_Guild );

					break;
				}
				case 3: // Set guild abbreviation
				{
					m_Mobile.SendLocalizedMessage( 1013061 ); // Enter new guild abbreviation (3 characters max):
					m_Mobile.Prompt = new GuildAbbrvPrompt( m_Mobile, m_Guild );

					break;
				}
				case 4: // Change guild type
				{
					GuildGump.EnsureClosed( m_Mobile );
					m_Mobile.SendGump( new GuildChangeTypeGump( m_Mobile, m_Guild ) );

					break;
				}
				case 5: // Set charter
				{
					m_Mobile.SendLocalizedMessage( 1013071 ); // Enter the new guild charter (50 characters max):
					m_Mobile.Prompt = new GuildCharterPrompt( m_Mobile, m_Guild );

					break;
				}
				case 6: // Dismiss member
				{
					GuildGump.EnsureClosed( m_Mobile );
					m_Mobile.SendGump( new GuildDismissGump( m_Mobile, m_Guild ) );

					break;
				}
				case 7: // War menu
				{
					GuildGump.EnsureClosed( m_Mobile );
					m_Mobile.SendGump( new GuildWarAdminGump( m_Mobile, m_Guild ) );

					break;
				}
				case 8: // Administer candidates
				{
					GuildGump.EnsureClosed( m_Mobile );
					m_Mobile.SendGump( new GuildAdminCandidatesGump( m_Mobile, m_Guild ) );

					break;
				}
				case 9: // Set guildmaster's title
				{
					m_Mobile.SendLocalizedMessage( 1013073 ); // Enter new guildmaster title (20 characters max):
					m_Mobile.Prompt = new GuildTitlePrompt( m_Mobile, m_Mobile, m_Guild );

					break;
				}
				case 10: // Grant title
				{
					GuildGump.EnsureClosed( m_Mobile );
					m_Mobile.SendGump( new GrantGuildTitleGump( m_Mobile, m_Guild ) );

					break;
				}
				case 11: // Move guildstone
				{
					if ( m_Guild.Guildstone != null )
					{
						GuildTeleporter item = new GuildTeleporter( m_Guild.Guildstone );

						if ( m_Guild.Teleporter != null )
							m_Guild.Teleporter.Delete();

						m_Mobile.SendLocalizedMessage( 501133 ); // Use the teleporting object placed in your backpack to move this guildstone.

						m_Mobile.AddToBackpack( item );
						m_Guild.Teleporter = item;
					}

					GuildGump.EnsureClosed( m_Mobile );
					m_Mobile.SendGump( new GuildmasterGump( m_Mobile, m_Guild ) );

					break;
				}
			}
		}
	}
}