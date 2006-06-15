using System;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Factions
{
	public class ElectionManagementGump : Gump
	{
		public string Right( string text )
		{
			return String.Format( "<DIV ALIGN=RIGHT>{0}</DIV>", text );
		}

		public string Center( string text )
		{
			return String.Format( "<CENTER>{0}</CENTER>", text );
		}

		public string Color( string text, int color )
		{
			return String.Format( "<BASEFONT COLOR=#{0:X6}>{1}</BASEFONT>", color, text );
		}

		public static string FormatTimeSpan( TimeSpan ts )
		{
			return String.Format( "{0:D2}:{1:D2}:{2:D2}:{3:D2}", ts.Days, ts.Hours % 24, ts.Minutes % 60, ts.Seconds % 60 );
		}

		public const int LabelColor = 0xFFFFFF;

		private Election m_Election;
		private Candidate m_Candidate;
		private int m_Page;

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			Mobile from = sender.Mobile;
			int bid = info.ButtonID;

			if ( m_Candidate == null )
			{
				if ( bid == 0 )
				{
				}
				else if ( bid == 1 )
				{
				}
				else
				{
					bid -= 2;

					if ( bid >= 0 && bid < m_Election.Candidates.Count )
						from.SendGump( new ElectionManagementGump( m_Election, m_Election.Candidates[bid], 0 ) );
				}
			}
			else
			{
				if ( bid == 0 )
				{
					from.SendGump( new ElectionManagementGump( m_Election ) );
				}
				else if ( bid == 1 )
				{
					m_Election.RemoveCandidate( m_Candidate.Mobile );
					from.SendGump( new ElectionManagementGump( m_Election ) );
				}
				else if ( bid == 2 && m_Page > 0 )
				{
					from.SendGump( new ElectionManagementGump( m_Election, m_Candidate, m_Page - 1 ) );
				}
				else if ( bid == 3 && (m_Page + 1) * 10 < m_Candidate.Voters.Count )
				{
					from.SendGump( new ElectionManagementGump( m_Election, m_Candidate, m_Page + 1 ) );
				}
				else
				{
					bid -= 4;

					if ( bid >= 0 && bid < m_Candidate.Voters.Count )
					{
						m_Candidate.Voters.RemoveAt( bid );
						from.SendGump( new ElectionManagementGump( m_Election, m_Candidate, m_Page ) );
					}
				}
			}
		}

		public ElectionManagementGump( Election election ) : this( election, null, 0 )
		{
		}

		public ElectionManagementGump( Election election, Candidate candidate, int page ) : base( 40, 40 )
		{
			m_Election = election;
			m_Candidate = candidate;
			m_Page = page;

			AddPage( 0 );

			if ( candidate != null )
			{
				AddBackground( 0, 0, 448, 354, 9270 );
				AddAlphaRegion( 10, 10, 428, 334 );

				AddHtml( 10, 10, 428, 20, Color( Center( "Candidate Management" ), LabelColor ), false, false );

				AddHtml(  45, 35, 100, 20, Color( "Player Name:", LabelColor ), false, false );
				AddHtml( 145, 35, 100, 20, Color( candidate.Mobile == null ? "null" : candidate.Mobile.Name, LabelColor ), false, false );

				AddHtml(  45, 55, 100, 20, Color( "Vote Count:", LabelColor ), false, false );
				AddHtml( 145, 55, 100, 20, Color( candidate.Votes.ToString(), LabelColor ), false, false );

				AddButton( 12, 73, 4005, 4007, 1, GumpButtonType.Reply, 0 );
				AddHtml(  45, 75, 100, 20, Color( "Drop Candidate", LabelColor ), false, false );

				AddImageTiled( 13, 99, 422, 242, 9264 );
				AddImageTiled( 14, 100, 420, 240, 9274 );
				AddAlphaRegion( 14, 100, 420, 240 );

				AddHtml( 14, 100, 420, 20, Color( Center( "Voters" ), LabelColor ), false, false );

				if ( page > 0 )
					AddButton( 397, 104, 0x15E3, 0x15E7, 2, GumpButtonType.Reply, 0 );
				else
					AddImage( 397, 104, 0x25EA );

				if ( (page + 1) * 10 < candidate.Voters.Count )
					AddButton( 414, 104, 0x15E1, 0x15E5, 3, GumpButtonType.Reply, 0 );
				else
					AddImage( 414, 104, 0x25E6 );


				AddHtml( 14, 120, 30, 20, Color( Center( "DEL" ), LabelColor ), false, false );
				AddHtml( 47, 120, 150, 20, Color( "Name", LabelColor ), false, false );
				AddHtml( 195, 120, 100, 20, Color( Center( "Address" ), LabelColor ), false, false );
				AddHtml( 295, 120, 80, 20, Color( Center( "Time" ), LabelColor ), false, false );
				AddHtml( 355, 120, 60, 20, Color( Center( "Legit" ), LabelColor ), false, false );

				int idx = 0;

				for ( int i = page*10; i >= 0 && i < candidate.Voters.Count && i < (page+1)*10; ++i, ++idx )
				{
					Voter voter = (Voter)candidate.Voters[i];

					AddButton( 13, 138 + (idx * 20), 4002, 4004, 4 + i, GumpButtonType.Reply, 0 );

					object[] fields = voter.AcquireFields();

					int x = 45;

					for ( int j = 0; j < fields.Length; ++j )
					{
						object obj = fields[j];

						if ( obj is Mobile )
						{
							AddHtml( x + 2, 140 + (idx * 20), 150, 20, Color( ((Mobile)obj).Name, LabelColor ), false, false );
							x += 150;
						}
						else if ( obj is System.Net.IPAddress )
						{
							AddHtml( x, 140 + (idx * 20), 100, 20, Color( Center( obj.ToString() ), LabelColor ), false, false );
							x += 100;
						}
						else if ( obj is DateTime )
						{
							AddHtml( x, 140 + (idx * 20), 80, 20, Color( Center( FormatTimeSpan( ((DateTime)obj) - election.LastStateTime ) ), LabelColor ), false, false );
							x += 80;
						}
						else if ( obj is int )
						{
							AddHtml( x, 140 + (idx * 20), 60, 20, Color( Center( (int)obj + "%" ), LabelColor ), false, false );
							x += 60;
						}
					}
				}
			}
			else
			{
				AddBackground( 0, 0, 288, 334, 9270 );
				AddAlphaRegion( 10, 10, 268, 314 );

				AddHtml( 10, 10, 268, 20, Color( Center( "Election Management" ), LabelColor ), false, false );

				AddHtml(  45, 35, 100, 20, Color( "Current State:", LabelColor ), false, false );
				AddHtml( 145, 35, 100, 20, Color( election.State.ToString(), LabelColor ), false, false );

				AddButton( 12, 53, 4005, 4007, 1, GumpButtonType.Reply, 0 );
				AddHtml(  45, 55, 100, 20, Color( "Transition Time:", LabelColor ), false, false );
				AddHtml( 145, 55, 100, 20, Color( FormatTimeSpan( election.NextStateTime ), LabelColor ), false, false );

				AddImageTiled( 13, 79, 262, 242, 9264 );
				AddImageTiled( 14, 80, 260, 240, 9274 );
				AddAlphaRegion( 14, 80, 260, 240 );

				AddHtml( 14, 80, 260, 20, Color( Center( "Candidates" ), LabelColor ), false, false );
				AddHtml( 14, 100, 30, 20, Color( Center( "-->" ), LabelColor ), false, false );
				AddHtml( 47, 100, 150, 20, Color( "Name", LabelColor ), false, false );
				AddHtml( 195, 100, 80, 20, Color( Center( "Votes" ), LabelColor ), false, false );

				for ( int i = 0; i < election.Candidates.Count; ++i )
				{
					Candidate cd = election.Candidates[i];
					Mobile mob = cd.Mobile;

					if ( mob == null )
						continue;

					AddButton( 13, 118 + (i * 20), 4005, 4007, 2 + i, GumpButtonType.Reply, 0 );
					AddHtml( 47, 120 + (i * 20), 150, 20, Color( mob.Name, LabelColor ), false, false );
					AddHtml( 195, 120 + (i * 20), 80, 20, Color( Center( cd.Votes.ToString() ), LabelColor ), false, false );
				}
			}
		}
	}
}