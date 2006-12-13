using System;
using Server;
using Server.Mobiles;
using Server.Gumps;
using Server.Network;
using Server.Factions;
using Server.Prompts;

namespace Server.Guilds
{
	public class GuildInfoGump : BaseGuildGump
	{
		private bool m_IsResigning;

		public GuildInfoGump( PlayerMobile pm, Guild g ) : this( pm, g, false )
		{
		}
		public GuildInfoGump( PlayerMobile pm, Guild g, bool isResigning ) : base( pm, g )
		{
			m_IsResigning = isResigning;
			PopulateGump();
		}

		public override void PopulateGump()
		{
			bool isLeader = IsLeader( player, guild );
			base.PopulateGump();

			AddHtmlLocalized( 96, 43, 110, 26, 1063014, 0xF, false, false ); // My Guild

			AddImageTiled( 65, 80, 160, 26, 0xA40 );
			AddImageTiled( 67, 82, 156, 22, 0xBBC );
			AddHtmlLocalized( 70, 83, 150, 20, 1062954, 0x0, false, false ); // <i>Guild Name</i>
			AddHtml( 233, 84, 320, 26, guild.Name, false, false );

			AddImageTiled( 65, 114, 160, 26, 0xA40 );
			AddImageTiled( 67, 116, 156, 22, 0xBBC );
			AddHtmlLocalized( 70, 117, 150, 20, 1063025, 0x0, false, false ); // <i>Alliance</i>

			if( guild.Alliance != null && guild.Alliance.IsMember( guild ) )
			{
				AddHtml( 233, 118, 320, 26, guild.Alliance.Name, false, false );
				AddButton( 40, 120, 0x4B9, 0x4BA, 6, GumpButtonType.Reply, 0 );	//Alliance Roster
			}

			AddImageTiled( 65, 148, 160, 26, 0xA40 );
			AddImageTiled( 67, 150, 156, 22, 0xBBC );
			AddHtmlLocalized( 70, 151, 150, 20, 1063084, 0x0, false, false ); // <i>Guild Faction</i>
		
			Faction f = Faction.Find( guild.Leader );
			if( f != null )
				AddHtml( 233, 152, 320, 26, f.ToString(), false, false );

			AddImageTiled( 65, 196, 480, 4, 0x238D );


			string s = guild.Charter;
			if( String.IsNullOrEmpty( s ) )
				s = "The guild leader has not yet set the guild charter.";

			AddHtml( 65, 216, 480, 80, s, true, true );
			if( isLeader )
				AddButton( 40, 251, 0x4B9, 0x4BA, 4, GumpButtonType.Reply, 0 );	//Charter Edit button

			s = guild.Website;
			if( string.IsNullOrEmpty( s ) )
				s = "Guild website not yet set.";
			AddHtml( 65, 306, 480, 30, s, true, false );
			if( isLeader )
				AddButton( 40, 313, 0x4B9, 0x4BA, 5, GumpButtonType.Reply, 0 );	//Website Edit button

			AddCheck( 65, 370, 0xD2, 0xD3, player.DisplayGuildTitle, 0 );
			AddHtmlLocalized( 95, 370, 150, 26, 1063085, 0x0, false, false ); // Show Guild Title
			AddBackground( 450, 370, 100, 26, 0x2486 );

			AddButton( 455, 375, 0x845, 0x846, 7, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 480, 373, 60, 26, 3006115, (m_IsResigning) ? 0x5000 : 0, false, false ); // Resign
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			base.OnResponse( sender, info );

			PlayerMobile pm = sender.Mobile as PlayerMobile;

			if( !IsMember( pm, guild ) )
				return;

			
			pm.DisplayGuildTitle = info.IsSwitched( 0 );
			
			switch( info.ButtonID )
			{
				//1-3 handled by base.OnResponse
				case 4:
				{
					if( IsLeader( pm, guild ) )
					{
						pm.SendLocalizedMessage( 1013071 ); // Enter the new guild charter (50 characters max):

						pm.BeginPrompt( new PromptCallback( SetCharter_Callback ), true );	//Have the same callback handle both canceling and deletion cause the 2nd callback would just get a text of ""
					}
					break;
				}
				case 5:
				{
					if( IsLeader( pm, guild ) )
					{
						pm.SendLocalizedMessage( 1013072 ); // Enter the new website for the guild (50 characters max):
						pm.BeginPrompt( new PromptCallback( SetWebsite_Callback ), true );	//Have the same callback handle both canceling and deletion cause the 2nd callback would just get a text of ""
					}
					break;
				}
				case 6:
				{
					//Alliance Roster
					if( guild.Alliance != null && guild.Alliance.IsMember( guild ) )
						pm.SendGump( new AllianceInfo.AllianceRosterGump( pm, guild, guild.Alliance ) );

					break;
				}
				case 7:
				{
					//Resign
					if( !m_IsResigning )
					{
						pm.SendLocalizedMessage( 1063332 ); // Are you sure you wish to resign from your guild?
						pm.SendGump( new GuildInfoGump( pm, guild, true ) );
					}
					else
					{
						guild.RemoveMember( pm, 1063411 ); // You resign from your guild.
					}
					break;
				}
			}
		}

		public void SetCharter_Callback( Mobile from, string text )
		{
			if( !IsLeader( from, guild ) )
				return;

			string charter = Utility.FixHtml( text.Trim() );

			if( charter.Length > 50 )
			{
				from.SendLocalizedMessage( 1070774, "50" ); // Your guild charter cannot exceed ~1_val~ characters.
			}
			else
			{
				guild.Charter = charter;
				from.SendLocalizedMessage( 1070775 ); // You submit a new guild charter.
				return;
			}
		}

		public void SetWebsite_Callback( Mobile from, string text )
		{
			if( !IsLeader( from, guild ) )
				return;

			string site = Utility.FixHtml( text.Trim() );

			if( site.Length > 50 )
				from.SendLocalizedMessage( 1070777, "50" ); // Your guild website cannot exceed ~1_val~ characters.
			else
			{
				guild.Website = site;
				from.SendLocalizedMessage( 1070778 ); // You submit a new guild website.
				return;
			}
		}
	}
}
