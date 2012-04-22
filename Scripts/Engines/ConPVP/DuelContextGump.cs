using System;
using System.Collections;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP
{
	public class DuelContextGump : Gump
	{
		private Mobile m_From;
		private DuelContext m_Context;

		public Mobile From{ get{ return m_From; } } 
		public DuelContext Context{ get{ return m_Context; } }

		public string Center( string text )
		{
			return String.Format( "<CENTER>{0}</CENTER>", text );
		}

		public void AddGoldenButton( int x, int y, int bid )
		{
			AddButton( x  , y  , 0xD2, 0xD2, bid, GumpButtonType.Reply, 0 );
			AddButton( x+3, y+3, 0xD8, 0xD8, bid, GumpButtonType.Reply, 0 );
		}

		public void AddGoldenButtonLabeled( int x, int y, int bid, string text )
		{
			AddGoldenButton( x, y, bid );
			AddHtml( x + 25, y, 200, 20, text, false, false );
		}

		public DuelContextGump( Mobile from, DuelContext context ) : base( 50, 50 )
		{
			m_From = from;
			m_Context = context;

			from.CloseGump( typeof( RulesetGump ) );
			from.CloseGump( typeof( DuelContextGump ) );
			from.CloseGump( typeof( ParticipantGump ) );

			int count = context.Participants.Count;

			if ( count < 3 )
				count = 3;

			int height = 35 + 10 + 22 + 30 + 22 + 22 + 2 + (count * 22) + 2 + 30;

			AddPage( 0 );

			AddBackground( 0, 0, 300, height, 9250 );
			AddBackground( 10, 10, 280, height - 20, 0xDAC );

			AddHtml( 35, 25, 230, 20, Center( "Duel Setup" ), false, false );

			int x = 35;
			int y = 47;

			AddGoldenButtonLabeled( x, y, 1, "Rules" ); y += 22;
			AddGoldenButtonLabeled( x, y, 2, "Start" ); y += 22;
			AddGoldenButtonLabeled( x, y, 3, "Add Participant" ); y += 30;

			AddHtml( 35, y, 230, 20, Center( "Participants" ), false, false ); y += 22;

			for ( int i = 0; i < context.Participants.Count; ++i )
			{
				Participant p = (Participant)context.Participants[i];

				AddGoldenButtonLabeled( x, y, 4 + i, String.Format( p.Count == 1 ? "Player {0}: {3}" : "Team {0}: {1}/{2}: {3}", 1 + i, p.FilledSlots, p.Count, p.NameList ) ); y += 22;
			}
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( !m_Context.Registered )
				return;

			int index = info.ButtonID;

			switch ( index )
			{
				case -1: // CloseGump
				{
					break;
				}
				case 0: // closed
				{
					m_Context.Unregister();
					break;
				}
				case 1: // Rules
				{
					//m_From.SendGump( new RulesetGump( m_From, m_Context.Ruleset, m_Context.Ruleset.Layout, m_Context ) );
					m_From.SendGump( new PickRulesetGump( m_From, m_Context, m_Context.Ruleset ) );
					break;
				}
				case 2: // Start
				{
					if ( m_Context.CheckFull() )
					{
						m_Context.CloseAllGumps();
						m_Context.SendReadyUpGump();
						//m_Context.SendReadyGump();
					}
					else
					{
						m_From.SendMessage( "You cannot start the duel before all participating players have been assigned." );
						m_From.SendGump( new DuelContextGump( m_From, m_Context ) );
					}

					break;
				}
				case 3: // New Participant
				{
					if ( m_Context.Participants.Count < 10 )
						m_Context.Participants.Add( new Participant( m_Context, 1 ) );
					else
						m_From.SendMessage( "The number of participating parties may not be increased further." );

					m_From.SendGump( new DuelContextGump( m_From, m_Context ) );

					break;
				}
				default: // Participant
				{
					index -= 4;

					if ( index >= 0 && index < m_Context.Participants.Count )
						m_From.SendGump( new ParticipantGump( m_From, m_Context, (Participant)m_Context.Participants[index] ) );

					break;
				}
			}
		}
	}
}