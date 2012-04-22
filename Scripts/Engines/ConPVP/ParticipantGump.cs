using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Gumps;
using Server.Network;
using Server.Targeting;
using Server.Mobiles;

namespace Server.Engines.ConPVP
{
	public class ParticipantGump : Gump
	{
		private Mobile m_From;
		private DuelContext m_Context;
		private Participant m_Participant;

		public Mobile From{ get{ return m_From; } } 
		public DuelContext Context{ get{ return m_Context; } }
		public Participant Participant{ get{ return m_Participant; } }

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

		public ParticipantGump( Mobile from, DuelContext context, Participant p ) : base( 50, 50 )
		{
			m_From = from;
			m_Context = context;
			m_Participant = p;

			from.CloseGump( typeof( RulesetGump ) );
			from.CloseGump( typeof( DuelContextGump ) );
			from.CloseGump( typeof( ParticipantGump ) );

			int count = p.Players.Length;

			if ( count < 4 )
				count = 4;

			AddPage( 0 );
			
			int height = 35 + 10 + 22 + 22 + 30 + 22 + 2 + (count * 22) + 2 + 30;

			AddBackground( 0, 0, 300, height, 9250 );
			AddBackground( 10, 10, 280, height - 20, 0xDAC );

			AddButton( 240, 25, 0xFB1, 0xFB3, 3, GumpButtonType.Reply, 0 );

			//AddButton( 223, 54, 0x265A, 0x265A, 4, GumpButtonType.Reply, 0 );

			AddHtml( 35, 25, 230, 20, Center( "Participant Setup" ), false, false );

			int x = 35;
			int y = 47;

			AddHtml( x, y, 200, 20, String.Format( "Team Size: {0}", p.Players.Length ), false, false ); y += 22;

			AddGoldenButtonLabeled( x + 20, y, 1, "Increase" ); y += 22;
			AddGoldenButtonLabeled( x + 20, y, 2, "Decrease" ); y += 30;

			AddHtml( 35, y, 230, 20, Center( "Players" ), false, false ); y += 22;

			for ( int i = 0; i < p.Players.Length; ++i )
			{
				DuelPlayer pl = p.Players[i];

				AddGoldenButtonLabeled( x, y, 5 + i, String.Format( "{0}: {1}", 1 + i, pl == null ? "Empty" : pl.Mobile.Name ) ); y += 22;
			}
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( !m_Context.Registered )
				return;

			int bid = info.ButtonID;

			if ( bid == 0 )
			{
				m_From.SendGump( new DuelContextGump( m_From, m_Context ) );
			}
			else if ( bid == 1 )
			{
				if ( m_Participant.Count < 8 )
					m_Participant.Resize( m_Participant.Count + 1 );
				else
					m_From.SendMessage( "You may not raise the team size any further." );

				m_From.SendGump( new ParticipantGump( m_From, m_Context, m_Participant ) );
			}
			else if ( bid == 2 )
			{
				if ( m_Participant.Count > 1 && m_Participant.Count > m_Participant.FilledSlots )
					m_Participant.Resize( m_Participant.Count - 1 );
				else
					m_From.SendMessage( "You may not lower the team size any further." );

				m_From.SendGump( new ParticipantGump( m_From, m_Context, m_Participant ) );
			}
			else if ( bid == 3 )
			{
				if ( m_Participant.FilledSlots > 0 )
				{
					m_From.SendMessage( "There is at least one currently active player. You must remove them first." );
					m_From.SendGump( new ParticipantGump( m_From, m_Context, m_Participant ) );
				}
				else if ( m_Context.Participants.Count > 2 )
				{
					/*Container cont = m_Participant.Stakes;

					if ( cont != null )
						cont.Delete();*/

					m_Context.Participants.Remove( m_Participant );
					m_From.SendGump( new DuelContextGump( m_From, m_Context ) );
				}
				else
				{
					m_From.SendMessage( "Duels must have at least two participating parties." );
					m_From.SendGump( new ParticipantGump( m_From, m_Context, m_Participant ) );
				}
			}
			/*else if ( bid == 4 )
			{
				m_From.SendGump( new ParticipantGump( m_From, m_Context, m_Participant ) );

				Container cont = m_Participant.Stakes;

				if ( cont != null && !cont.Deleted )
				{
					cont.DisplayTo( m_From );

					Item[] checks = cont.FindItemsByType( typeof( BankCheck ) );

					int gold = cont.TotalGold;

					for ( int i = 0; i < checks.Length; ++i )
						gold += ((BankCheck)checks[i]).Worth;

					m_From.SendMessage( "This container has {0} item{1} and {2} stone{3}. In gold or check form there is a total of {4:D}gp.", cont.TotalItems, cont.TotalItems==1?"":"s", cont.TotalWeight, cont.TotalWeight==1?"":"s", gold );
				}
			}*/
			else
			{
				bid -= 5;

				if ( bid >= 0 && bid < m_Participant.Players.Length )
				{
					if ( m_Participant.Players[bid] == null )
					{
						m_From.Target = new ParticipantTarget( m_Context, m_Participant, bid );
						m_From.SendMessage( "Target a player." );
					}
					else
					{
						m_Participant.Players[bid].Mobile.SendMessage( "You have been removed from the duel." );

						if ( m_Participant.Players[bid].Mobile is PlayerMobile  )
							((PlayerMobile)(m_Participant.Players[bid].Mobile)).DuelPlayer = null;

						m_Participant.Players[bid] = null;
						m_From.SendMessage( "They have been removed from the duel." );
						m_From.SendGump( new ParticipantGump( m_From, m_Context, m_Participant ) );
					}
				}
			}
		}

		private class ParticipantTarget : Target
		{
			private DuelContext m_Context;
			private Participant m_Participant;
			private int m_Index;

			public ParticipantTarget( DuelContext context, Participant p, int index ) : base( 12, false, TargetFlags.None )
			{
				m_Context = context;
				m_Participant = p;
				m_Index = index;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( !m_Context.Registered )
					return;

				int index = m_Index;

				if ( index < 0 || index >= m_Participant.Players.Length )
					return;

				Mobile mob = targeted as Mobile;

				if ( mob == null )
				{
					from.SendMessage( "That is not a player." );
				}
				else if ( !mob.Player )
				{
					if ( mob.Body.IsHuman )
						mob.SayTo( from, 1005443 ); // Nay, I would rather stay here and watch a nail rust.
					else
						mob.SayTo( from, 1005444 ); // The creature ignores your offer.
				}
				else if ( AcceptDuelGump.IsIgnored( mob, from ) || mob.Blessed )
				{
					from.SendMessage( "They ignore your offer." );
				}
				else
				{
					PlayerMobile pm = mob as PlayerMobile;

					if ( pm == null )
						return;

					if ( pm.DuelContext != null )
						from.SendMessage( "{0} cannot fight because they are already assigned to another duel.", pm.Name );
					else if ( DuelContext.CheckCombat( pm ) )
						from.SendMessage( "{0} cannot fight because they have recently been in combat with another player.", pm.Name );
					else if ( mob.HasGump( typeof( AcceptDuelGump ) ) )
						from.SendMessage( "{0} has already been offered a duel." );
					else
					{
						from.SendMessage( "You send {0} to {1}.", m_Participant.Find( from ) == null ? "a challenge" : "an invitation", mob.Name );
						mob.SendGump( new AcceptDuelGump( from, mob, m_Context, m_Participant, m_Index ) );
					}
				}
			}

			protected override void OnTargetFinish( Mobile from )
			{
				from.SendGump( new ParticipantGump( from, m_Context, m_Participant ) );
			}
		}
	}
}