using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Targeting;
using Server.Mobiles;

namespace Server.Engines.ConPVP
{
	public class AcceptDuelGump : Gump
	{
		private Mobile m_Challenger, m_Challenged;
		private DuelContext m_Context;
		private Participant m_Participant;
		private int m_Slot;

		public string Center( string text )
		{
			return String.Format( "<CENTER>{0}</CENTER>", text );
		}

		public string Color( string text, int color )
		{
			return String.Format( "<BASEFONT COLOR=#{0:X6}>{1}</BASEFONT>", color, text );
		}

		private const int LabelColor32 = 0xFFFFFF;
		private const int BlackColor32 = 0x000008;

		private bool m_Active = true;

		public AcceptDuelGump( Mobile challenger, Mobile challenged, DuelContext context, Participant p, int slot ) : base( 50, 50 )
		{
			m_Challenger = challenger;
			m_Challenged = challenged;
			m_Context = context;
			m_Participant = p;
			m_Slot = slot;

			challenged.CloseGump( typeof( AcceptDuelGump ) );

			Closable = false;

			AddPage( 0 );

			//AddBackground( 0, 0, 400, 220, 9150 );
			AddBackground( 1, 1, 398, 218, 3600 );
			//AddBackground( 16, 15, 369, 189, 9100 );

			AddImageTiled( 16, 15, 369, 189, 3604 );
			AddAlphaRegion( 16, 15, 369, 189 );

			AddImage( 215, -43, 0xEE40 );
			//AddImage( 330, 141, 0x8BA );

			AddHtml( 22-1, 22, 294, 20, Color( Center( "Duel Challenge" ), BlackColor32 ), false, false );
			AddHtml( 22+1, 22, 294, 20, Color( Center( "Duel Challenge" ), BlackColor32 ), false, false );
			AddHtml( 22, 22-1, 294, 20, Color( Center( "Duel Challenge" ), BlackColor32 ), false, false );
			AddHtml( 22, 22+1, 294, 20, Color( Center( "Duel Challenge" ), BlackColor32 ), false, false );
			AddHtml( 22, 22, 294, 20, Color( Center( "Duel Challenge" ), LabelColor32 ), false, false );

			string fmt;

			if ( p.Contains( challenger ) )
				fmt = "You have been asked to join sides with {0} in a duel. Do you accept?";
			else
				fmt = "You have been challenged to a duel from {0}. Do you accept?";

			AddHtml( 22-1, 50, 294, 40, Color( String.Format( fmt, challenger.Name ), BlackColor32 ), false, false );
			AddHtml( 22+1, 50, 294, 40, Color( String.Format( fmt, challenger.Name ), BlackColor32 ), false, false );
			AddHtml( 22, 50-1, 294, 40, Color( String.Format( fmt, challenger.Name ), BlackColor32 ), false, false );
			AddHtml( 22, 50+1, 294, 40, Color( String.Format( fmt, challenger.Name ), BlackColor32 ), false, false );
			AddHtml( 22, 50, 294, 40, Color( String.Format( fmt, challenger.Name ), 0xB0C868 ), false, false );

			AddImageTiled( 32, 88, 264, 1, 9107 );
			AddImageTiled( 42, 90, 264, 1, 9157 );

			AddRadio( 24, 100, 9727, 9730, true, 1 );
			AddHtml( 60-1, 105, 250, 20, Color( "Yes, I will fight this duel.", BlackColor32 ), false, false );
			AddHtml( 60+1, 105, 250, 20, Color( "Yes, I will fight this duel.", BlackColor32 ), false, false );
			AddHtml( 60, 105-1, 250, 20, Color( "Yes, I will fight this duel.", BlackColor32 ), false, false );
			AddHtml( 60, 105+1, 250, 20, Color( "Yes, I will fight this duel.", BlackColor32 ), false, false );
			AddHtml( 60, 105, 250, 20, Color( "Yes, I will fight this duel.", LabelColor32 ), false, false );

			AddRadio( 24, 135, 9727, 9730, false, 2 );
			AddHtml( 60-1, 140, 250, 20, Color( "No, I do not wish to fight.", BlackColor32 ), false, false );
			AddHtml( 60+1, 140, 250, 20, Color( "No, I do not wish to fight.", BlackColor32 ), false, false );
			AddHtml( 60, 140-1, 250, 20, Color( "No, I do not wish to fight.", BlackColor32 ), false, false );
			AddHtml( 60, 140+1, 250, 20, Color( "No, I do not wish to fight.", BlackColor32 ), false, false );
			AddHtml( 60, 140, 250, 20, Color( "No, I do not wish to fight.", LabelColor32 ), false, false );

			AddRadio( 24, 170, 9727, 9730, false, 3 );
			AddHtml( 60-1, 175, 250, 20, Color( "No, knave. Do not ask again.", BlackColor32 ), false, false );
			AddHtml( 60+1, 175, 250, 20, Color( "No, knave. Do not ask again.", BlackColor32 ), false, false );
			AddHtml( 60, 175-1, 250, 20, Color( "No, knave. Do not ask again.", BlackColor32 ), false, false );
			AddHtml( 60, 175+1, 250, 20, Color( "No, knave. Do not ask again.", BlackColor32 ), false, false );
			AddHtml( 60, 175, 250, 20, Color( "No, knave. Do not ask again.", LabelColor32 ), false, false );

			AddButton( 314, 173, 247, 248, 1, GumpButtonType.Reply, 0 );

			Timer.DelayCall( TimeSpan.FromSeconds( 15.0 ), new TimerCallback( AutoReject ) );
		}

		public void AutoReject()
		{
			if ( !m_Active )
				return;

			m_Active = false;

			m_Challenged.CloseGump( typeof( AcceptDuelGump ) );

			m_Challenger.SendMessage( "{0} seems unresponsive.", m_Challenged.Name );
			m_Challenged.SendMessage( "You decline the challenge." );
		}

		private static Hashtable m_IgnoreLists = new Hashtable();

		private class IgnoreEntry
		{
			public Mobile m_Ignored;
			public DateTime m_Expire;

			public Mobile Ignored{ get{ return m_Ignored; } }
			public bool Expired{ get{ return ( DateTime.Now >= m_Expire ); } }

			private static TimeSpan ExpireDelay = TimeSpan.FromMinutes( 15.0 );

			public void Refresh()
			{
				m_Expire = DateTime.Now + ExpireDelay;
			}

			public IgnoreEntry( Mobile ignored )
			{
				m_Ignored = ignored;
				Refresh();
			}
		}

		public static void BeginIgnore( Mobile source, Mobile toIgnore )
		{
			ArrayList list = (ArrayList)m_IgnoreLists[source];

			if ( list == null )
				m_IgnoreLists[source] = list = new ArrayList();

			for ( int i = 0; i < list.Count; ++i )
			{
				IgnoreEntry ie = (IgnoreEntry)list[i];

				if ( ie.Ignored == toIgnore )
				{
					ie.Refresh();
					return;
				}
				else if ( ie.Expired )
				{
					list.RemoveAt( i-- );
				}
			}

			list.Add( new IgnoreEntry( toIgnore ) );
		}

		public static bool IsIgnored( Mobile source, Mobile check )
		{
			ArrayList list = (ArrayList)m_IgnoreLists[source];

			if ( list == null )
				return false;

			for ( int i = 0; i < list.Count; ++i )
			{
				IgnoreEntry ie = (IgnoreEntry)list[i];

				if ( ie.Expired )
					list.RemoveAt( i-- );
				else if ( ie.Ignored == check )
					return true;
			}

			return false;
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( info.ButtonID != 1 || !m_Active || !m_Context.Registered )
				return;

			m_Active = false;

			if ( !m_Context.Participants.Contains( m_Participant ) )
				return;

			if ( info.IsSwitched( 1 ) )
			{
				PlayerMobile pm = m_Challenged as PlayerMobile;

				if ( pm == null )
					return;

				if ( pm.DuelContext != null )
				{
					if ( pm.DuelContext.Initiator == pm )
						pm.SendMessage( 0x22, "You have already started a duel." );
					else
						pm.SendMessage( 0x22, "You have already been challenged in a duel." );

					m_Challenger.SendMessage( "{0} cannot fight because they are already assigned to another duel.", pm.Name );
				}
				else if ( DuelContext.CheckCombat( pm ) )
				{
					pm.SendMessage( 0x22, "You have recently been in combat with another player and must wait before starting a duel." );
					m_Challenger.SendMessage( "{0} cannot fight because they have recently been in combat with another player.", pm.Name );
				}
				else if ( TournamentController.IsActive )
				{
					pm.SendMessage( 0x22, "A tournament is currently active and you may not duel." );
					m_Challenger.SendMessage( 0x22, "A tournament is currently active and you may not duel." );
				}
				else
				{
					bool added = false;

					if ( m_Slot >= 0 && m_Slot < m_Participant.Players.Length && m_Participant.Players[m_Slot] == null )
					{
						added = true;
						m_Participant.Players[m_Slot] = new DuelPlayer( m_Challenged, m_Participant );
					}
					else
					{
						for ( int i = 0; i < m_Participant.Players.Length; ++i )
						{
							if ( m_Participant.Players[i] == null )
							{
								added = true;
								m_Participant.Players[i] = new DuelPlayer( m_Challenged, m_Participant );
								break;
							}
						}
					}

					if ( added )
					{
						m_Challenger.SendMessage( "{0} has accepted the request.", m_Challenged.Name );
						m_Challenged.SendMessage( "You have accepted the request from {0}.", m_Challenger.Name );

						NetState ns = m_Challenger.NetState;

						if ( ns != null )
						{
							foreach ( Gump g in ns.Gumps )
							{
								if ( g is ParticipantGump )
								{
									ParticipantGump pg = (ParticipantGump)g;

									if ( pg.Participant == m_Participant )
									{
										m_Challenger.SendGump( new ParticipantGump( m_Challenger, m_Context, m_Participant ) );
										break;
									}
								}
								else if ( g is DuelContextGump )
								{
									DuelContextGump dcg = (DuelContextGump)g;

									if ( dcg.Context == m_Context )
									{
										m_Challenger.SendGump( new DuelContextGump( m_Challenger, m_Context ) );
										break;
									}
								}
							}
						}
					}
					else
					{
						m_Challenger.SendMessage( "The participant list was full and so {0} could not join.", m_Challenged.Name );
						m_Challenged.SendMessage( "The participant list was full and so you could not join the fight {1} {0}.", m_Challenger.Name, m_Participant.Contains( m_Challenger ) ? "with" : "against" );
					}
				}
			}
			else
			{
				if ( info.IsSwitched( 3 ) )
					BeginIgnore( m_Challenged, m_Challenger );

				m_Challenger.SendMessage( "{0} does not wish to fight.", m_Challenged.Name );
				m_Challenged.SendMessage( "You chose not to fight {1} {0}.", m_Challenger.Name, m_Participant.Contains( m_Challenger ) ? "with" : "against" );
			}
		}
	}
}