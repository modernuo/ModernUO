using System;
using System.Collections.Generic;

using Server;

namespace Server.Poker
{
	public class PlayerStructure
	{
		private List<PokerPlayer> m_Players;
		private List<PokerPlayer> m_Round;
		private List<PokerPlayer> m_Turn;
		private PokerGame m_Game;

		public List<PokerPlayer> Players { get { return m_Players; } set { m_Players = value; } }
		public List<PokerPlayer> Round { get { return m_Round; } set { m_Round = value; } }
		public List<PokerPlayer> Turn { get { return m_Turn; } set { m_Turn = value; } }
		public PokerGame Game { get { return m_Game; } }

		public int Count { get { return m_Players.Count; } }

		public PokerPlayer this[int index]
		{
			get { return m_Players[index]; }
			set { m_Players[index] = value; }
		}

		public PlayerStructure( PokerGame game )
		{
			m_Players = new List<PokerPlayer>();
			m_Round = new List<PokerPlayer>();
			m_Turn = new List<PokerPlayer>();
			m_Game = game;
		}

		public void Push( PokerPlayer player )
		{
			if(!m_Turn.Contains(player))
				m_Turn.Add( player ); ;
		}

		public PokerPlayer Peek()
		{
			return ( m_Turn.Count > 0 ? m_Turn[m_Turn.Count - 1] : null );
		}

		public PokerPlayer Prev()
		{
			return ( m_Turn.Count > 1 ? m_Turn[m_Turn.Count - 2] : null );
		}

		public PokerPlayer Next()
		{
			if ( m_Round.Count == 1 || m_Turn.Count == m_Round.Count )
				return null;

			if ( Peek() == null ) //No turns yet for this round
			{
				if ( m_Game.State == PokerGameState.PreFlop )
				{
					PokerPlayer blind = m_Game.BigBlind == null ? m_Game.SmallBlind : m_Game.BigBlind;

					if ( blind != null )
						return ( m_Round.IndexOf( blind ) == m_Round.Count - 1 ? m_Round[0] : m_Round[m_Round.IndexOf( blind ) + 1] );
				}

				PokerPlayer dealer = m_Game.DealerButton;

				if ( dealer == null )
					return null;

				return ( m_Round.IndexOf( dealer ) == m_Round.Count - 1 ? m_Round[0] : m_Round[m_Round.IndexOf( dealer ) + 1] );
			}

			return ( m_Round.IndexOf( Peek() ) == m_Round.Count - 1 ? m_Round[0] : m_Round[m_Round.IndexOf( Peek() ) + 1] );
		}

		public bool Contains( PokerPlayer player ) { return m_Players.Contains( player ); }

		public void Clear()
		{
			m_Turn.Clear();
			m_Round.Clear();
		}
	}
}