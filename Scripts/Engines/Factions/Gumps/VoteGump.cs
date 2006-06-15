using System;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Factions
{
	public class VoteGump : FactionGump
	{
		private PlayerMobile m_From;
		private Election m_Election;

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( info.ButtonID == 0 )
			{
				m_From.SendGump( new FactionStoneGump( m_From, m_Election.Faction ) );
			}
			else
			{
				if ( !m_Election.CanVote( m_From ) )
					return;

				int index = info.ButtonID - 1;

				if ( index >= 0 && index < m_Election.Candidates.Count )
					m_Election.Candidates[index].Voters.Add( new Voter( m_From, m_Election.Candidates[index].Mobile ) );

				m_From.SendGump( new VoteGump( m_From, m_Election ) );
			}
		}

		public VoteGump( PlayerMobile from, Election election ) : base( 50, 50 )
		{
			m_From = from;
			m_Election = election;

			bool canVote = election.CanVote( from );

			AddPage( 0 );

			AddBackground( 0, 0, 420, 350, 5054 );
			AddBackground( 10, 10, 400, 330, 3000 );

			AddHtmlText( 20, 20, 380, 20, election.Faction.Definition.Header, false, false );

			if ( canVote )
				AddHtmlLocalized( 20, 60, 380, 20, 1011428, false, false ); // VOTE FOR LEADERSHIP
			else
				AddHtmlLocalized( 20, 60, 380, 20, 1038032, false, false ); // You have already voted in this election.

			for ( int i = 0; i < election.Candidates.Count; ++i )
			{
				Candidate cd = election.Candidates[i];

				if ( canVote )
					AddButton( 20, 100 + (i * 20), 4005, 4007, i + 1, GumpButtonType.Reply, 0 );

				AddLabel( 55, 100 + (i * 20), 0, cd.Mobile.Name );
				AddLabel( 300, 100 + (i * 20), 0, cd.Votes.ToString() );
			}

			AddButton( 20, 310, 4005, 4007, 0, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 310, 100, 20, 1011012, false, false ); // CANCEL
		}
	}
}