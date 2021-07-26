using System;
using System.Collections.Generic;

using Server;

namespace Server.Poker
{
	public class Deck
	{
		private Stack<Card> m_Deck;
		private List<Card> m_UsedCards;

		public Deck()
		{
			InitDeck();
		}

		private void InitDeck()
		{
			m_Deck = new Stack<Card>( 52 );
			m_UsedCards = new List<Card>();

			foreach ( Suit s in Enum.GetValues( typeof( Suit ) ) )
				foreach ( Rank r in Enum.GetValues( typeof( Rank ) ) )
					m_Deck.Push( new Card( s, r ) );

			Shuffle( 5 );
		}

		public Card Pop() { m_UsedCards.Add( m_Deck.Peek() ); return m_Deck.Pop(); }

		public Card Peek() { return m_Deck.Peek(); }

		public void Shuffle( int count )
		{
			List<Card> deck = new List<Card>( m_Deck.ToArray() );

			for ( int i = 0; i < count; ++i )
			{
				for ( int j = 0; j < deck.Count; ++j )
				{
					int index = Utility.Random( deck.Count );
					Card temp = deck[index];

					deck[index] = deck[j];
					deck[j] = temp;
				}
			}

			m_Deck = new Stack<Card>( deck );
		}
	}
}