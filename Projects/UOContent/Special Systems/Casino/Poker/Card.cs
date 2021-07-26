using System;

namespace Server.Poker
{
	public class Card : IComparable
	{
		public const int Red = 0x26;
		public const int Black = 0x00;

		private Suit m_Suit;
		private Rank m_Rank;

		public Suit Suit { get { return m_Suit; } }
		public Rank Rank { get { return m_Rank; } }

		public string Name { get { return string.Format( "{0} of {1}", m_Rank, m_Suit ).ToLower(); } }
		public string RankString { get { return m_Rank.ToString().ToLower(); } }

		public Card( Suit suit, Rank rank )
		{
			m_Suit = suit;
			m_Rank = rank;
		}

		public string GetRankLetter()
		{
			if ( (int)m_Rank < 11 )
				return ( (int)m_Rank ).ToString();
			else
			{
				switch ( m_Rank )
				{
					case Rank.Jack: return "J";
					case Rank.Queen: return "Q";
					case Rank.King: return "K";
					case Rank.Ace: return "A";
				}
			}
			return "?";
		}

		public int GetSuitColor() { return ( (int)m_Suit < 3 ? Red : Black ); }

		public string GetSuitString()
		{
			switch ( (int)m_Suit )
			{
				case 1:
					return "\u25C6";
				case 2:
					return "\u2665";
				case 3:
					return "\u2663";
				case 4:
					return "\u2660";
			}
			return "?";
		}

		public override string ToString()
		{
			return string.Format( "{0} of {1}", m_Rank, m_Suit );
		}

		#region IComparable Members

		public int CompareTo( object obj )
		{
			if ( obj is Card )
			{
				Card card = (Card)obj;

				if ( m_Rank < card.Rank )
					return 1;
				if ( m_Rank > card.Rank )
					return -1;
			}

			return 0;
		}

		#endregion
	}
}
