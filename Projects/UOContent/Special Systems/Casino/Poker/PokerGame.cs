using System;
using System.Collections.Generic;
using System.Text;

using Server;
using Server.Mobiles;

namespace Server.Poker
{
	public class GameBackup //Provides a protection for players so that if server crashes, they will be refunded money
	{
		public static List<PokerGame> PokerGames; //List of all poker games with players
	}

	public class PokerGame
	{
		public static void Initialize()
		{
			GameBackup.PokerGames = new List<PokerGame>();
            //EventSink.Crashed += new CrashedEventHandler( EventSink_Crashed );
            EventSink.ServerCrashed += EventSink_ServerCrashed;
		}

        private static void EventSink_ServerCrashed(ServerCrashedEventArgs obj)
        {
            foreach (PokerGame game in GameBackup.PokerGames)
            {
                List<PokerPlayer> toRemove = new List<PokerPlayer>();

                foreach (PokerPlayer player in game.Players.Players)
                    if (player.Mobile != null)
                        toRemove.Add(player);

                foreach (PokerPlayer player in toRemove)
                {
                    player.SendMessage(0x22, "The server has crashed, and you are now being removed from the poker game and being refunded the money that you currently have.");
                    game.RemovePlayer(player);
                }
            }
        }

        
		private bool m_NeedsGumpUpdate;
		private int m_CommunityGold;
		private int m_CurrentBet;
		private Deck m_Deck;
		private PokerGameState m_State;
		private PokerDealer m_Dealer;
		private PokerPlayer m_DealerButton;
		private PokerPlayer m_SmallBlind;
		private PokerPlayer m_BigBlind;
		private List<Card> m_CommunityCards;
		private PokerGameTimer m_Timer;
		private PlayerStructure m_Players;

		public bool NeedsGumpUpdate { get { return m_NeedsGumpUpdate; } set { m_NeedsGumpUpdate = value; } }
		public int CommunityGold { get { return m_CommunityGold; } set { m_CommunityGold = value; } }
		public int CurrentBet { get { return m_CurrentBet; } set { m_CurrentBet = value; } }
		public Deck Deck { get { return m_Deck; } set { m_Deck = value; } }
		public PokerGameState State { get { return m_State; } set { m_State = value; } }
		public PokerDealer Dealer { get { return m_Dealer; } set { m_Dealer = value; } }
		public PokerPlayer DealerButton { get { return m_DealerButton; } }
		public PokerPlayer SmallBlind { get { return m_SmallBlind; } }
		public PokerPlayer BigBlind { get { return m_BigBlind; } }
		public List<Card> CommunityCards { get { return m_CommunityCards; } set { m_CommunityCards = value; } }
		public PokerGameTimer Timer { get { return m_Timer; } set { m_Timer = value; } }
		public PlayerStructure Players { get { return m_Players; } }

		public bool IsBettingRound { get { return ( (int)m_State % 2 == 0 ); } }

		public PokerGame( PokerDealer dealer )
		{
			m_Dealer = dealer;
			m_NeedsGumpUpdate = false;
			m_CommunityCards = new List<Card>();
			m_State = PokerGameState.Inactive;
			m_Deck = new Deck();
			m_Timer = new PokerGameTimer( this );
			m_Players = new PlayerStructure( this );
		}

		public void PokerMessage( Mobile from, string message )
		{
			from.PublicOverheadMessage( Server.Network.MessageType.Regular, 0x9A, true, message );

			for ( int i = 0; i < m_Players.Count; ++i )
				if ( m_Players[i].Mobile != null )
					m_Players[i].Mobile.SendMessage( 0x9A, "[{0}]: {1}", from.Name, message );
		}

		public void PokerGame_PlayerMadeDecision( PokerPlayer player )
		{
			if ( m_Players.Peek() == player )
			{
				if ( player.Mobile == null )
					return;

				bool resetTurns = false;

				switch ( player.Action )
				{
					case PlayerAction.None: break;
					case PlayerAction.Bet:
						{
							PokerMessage( player.Mobile, string.Format( "I bet {0}.", player.Bet ) );
							m_CurrentBet = player.Bet;
							player.RoundBet = player.Bet;
							player.Gold -= player.Bet;
							player.RoundGold += player.Bet;
							m_CommunityGold += player.Bet;
							resetTurns = true;

							break;
						}
					case PlayerAction.Raise:
						{
							PokerMessage( player.Mobile, string.Format( "I raise by {0}.", player.Bet ) );
							m_CurrentBet += player.Bet;
							int diff = m_CurrentBet - player.RoundBet;
							player.Gold -= diff;
							player.RoundGold += diff;
							player.RoundBet += diff;
							m_CommunityGold += diff;
							player.Bet = diff;
							resetTurns = true;

							break;
						}
					case PlayerAction.Call:
						{
							PokerMessage( player.Mobile, "I call." );

							int diff = m_CurrentBet - player.RoundBet; //how much they owe in the pot
							player.Bet = diff;
							player.Gold -= diff;
							player.RoundGold += diff;
							player.RoundBet += diff;
							m_CommunityGold += diff;

							break;
						}
					case PlayerAction.Check:
						{
							if ( !player.LonePlayer )
								PokerMessage( player.Mobile, "Check." );

							break;
						}
					case PlayerAction.Fold:
						{
							PokerMessage( player.Mobile, "I fold." );

							if ( m_Players.Round.Contains( player ) )
								m_Players.Round.Remove( player );
							if ( m_Players.Turn.Contains( player ) )
								m_Players.Turn.Remove( player );

							if ( m_Players.Round.Count == 1 )
							{
								DoShowdown( true );
								return;
							}

							break;
						}
					case PlayerAction.AllIn:
						{
							if ( !player.IsAllIn )
							{
								if ( player.Forced )
									PokerMessage( player.Mobile, "I call: all-in." );
								else
									PokerMessage( player.Mobile, "All in." );

								int diff = player.Gold - m_CurrentBet;

								if ( diff > 0 )
									m_CurrentBet += diff;

								player.Bet = player.Gold;
								player.RoundGold += player.Gold;
								player.RoundBet += player.Gold;
								m_CommunityGold += player.Gold;
								player.Gold = 0;

								//We need to check to see if this is a follow up action, or a first call
								//before we reset the turns
								if ( m_Players.Prev() != null )
								{
									resetTurns = ( m_Players.Prev().Action == PlayerAction.Check );

									PokerPlayer prev = m_Players.Prev();

									if ( prev.Action == PlayerAction.Check ||
										( prev.Action == PlayerAction.Bet && prev.Bet < player.Bet ) ||
										( prev.Action == PlayerAction.AllIn && prev.Bet < player.Bet ) ||
										( prev.Action == PlayerAction.Call && prev.Bet < player.Bet ) ||
										( prev.Action == PlayerAction.Raise && prev.Bet < player.Bet ) )
										resetTurns = true;
								}
								else
									resetTurns = true;

								player.IsAllIn = true;
								player.Forced = false;
							}

							break;
						}
				}

				if ( resetTurns )
				{
					m_Players.Turn.Clear();
					m_Players.Push( player );
				}

				m_Timer.m_LastPlayer = null;
				m_Timer.hasWarned = false;

				if ( m_Players.Turn.Count == m_Players.Round.Count )
					m_State = (PokerGameState)( (int)m_State + 1 );
				else
					AssignNextTurn();

				m_NeedsGumpUpdate = true;
			}
		}

		public void Begin()
		{
			m_Players.Clear();
			m_CurrentBet = 0;

			List<PokerPlayer> dispose = new List<PokerPlayer>();

			foreach ( PokerPlayer player in m_Players.Players )
				if ( player.RequestLeave || !player.IsOnline() )
					dispose.Add( player );

			foreach ( PokerPlayer player in dispose )
				if ( m_Players.Contains( player ) )
					RemovePlayer( player );

			foreach ( PokerPlayer player in m_Players.Players )
			{
				player.ClearGame();
				player.Game = this;

				if ( player.Gold >= m_Dealer.BigBlind && player.IsOnline() )
					m_Players.Round.Add( player );
			}

			if ( m_DealerButton == null ) //First round / more player
			{
				if ( m_Players.Round.Count == 2 ) //Only use dealer button and small blind
				{
					m_DealerButton = m_Players.Round[0];
					m_SmallBlind = m_Players.Round[1];
					m_BigBlind = null;
				}
				else if ( m_Players.Round.Count > 2 )
				{
					m_DealerButton = m_Players.Round[0];
					m_SmallBlind = m_Players.Round[1];
					m_BigBlind = m_Players.Round[2];
				}
				else
					return;
			}
			else
			{
				if ( m_Players.Round.Count == 2 ) //Only use dealer button and small blind
				{
					if ( m_DealerButton == m_Players.Round[0] )
					{
						m_DealerButton = m_Players.Round[1];
						m_SmallBlind = m_Players.Round[0];
					}
					else
					{
						m_DealerButton = m_Players.Round[0];
						m_SmallBlind = m_Players.Round[1];
					}

					m_BigBlind = null;
				}
				else if ( m_Players.Round.Count > 2 )
				{
					int index = m_Players.Round.IndexOf( m_DealerButton );

					if ( index == -1 ) //Old dealer button was lost :(
					{
						m_DealerButton = null;
						Begin(); //Start over
						return;
					}

					if ( index == m_Players.Round.Count - 1 )
					{
						m_DealerButton = m_Players.Round[0];
						m_SmallBlind = m_Players.Round[1];
						m_BigBlind = m_Players.Round[2];
					}
					else if ( index == m_Players.Round.Count - 2 )
					{
						m_DealerButton = m_Players.Round[m_Players.Round.Count - 1];
						m_SmallBlind = m_Players.Round[0];
						m_BigBlind = m_Players.Round[1];
					}
					else if ( index == m_Players.Round.Count - 3 )
					{
						m_DealerButton = m_Players.Round[m_Players.Round.Count - 2];
						m_SmallBlind = m_Players.Round[m_Players.Round.Count - 1];
						m_BigBlind = m_Players.Round[0];
					}
					else
					{
						m_DealerButton = m_Players.Round[index + 1];
						m_SmallBlind = m_Players.Round[index + 2];
						m_BigBlind = m_Players.Round[index + 3];
					}
				}
				else
					return;
			}

			m_CommunityCards.Clear();
			m_Deck = new Deck();

			m_State = PokerGameState.DealHoleCards;

			if ( m_BigBlind != null )
			{
				m_BigBlind.Gold -= m_Dealer.BigBlind;
				m_CommunityGold += m_Dealer.BigBlind;
				m_BigBlind.RoundGold = m_Dealer.BigBlind;
				m_BigBlind.RoundBet = m_Dealer.BigBlind;
				m_BigBlind.Bet = m_Dealer.BigBlind;
			}

			m_SmallBlind.Gold -= m_BigBlind == null ? m_Dealer.BigBlind : m_Dealer.SmallBlind;
			m_CommunityGold += m_BigBlind == null ? m_Dealer.BigBlind : m_Dealer.SmallBlind;
			m_SmallBlind.RoundGold = m_BigBlind == null ? m_Dealer.BigBlind : m_Dealer.SmallBlind;
			m_SmallBlind.RoundBet = m_BigBlind == null ? m_Dealer.BigBlind : m_Dealer.SmallBlind;
			m_SmallBlind.Bet = m_BigBlind == null ? m_Dealer.BigBlind : m_Dealer.SmallBlind;

			if ( m_BigBlind != null )
			{
				//m_Players.Push( m_BigBlind );
				m_BigBlind.SetBBAction();
				m_CurrentBet = m_Dealer.BigBlind;
			}
			else
			{
				//m_Players.Push( m_SmallBlind );
				m_SmallBlind.SetBBAction();
				m_CurrentBet = m_Dealer.BigBlind;
			}

			if ( m_Players.Next() == null )
				return;

			m_NeedsGumpUpdate = true;
			m_Timer = new PokerGameTimer( this );
			m_Timer.Start();
		}

		public void End()
		{
			m_State = PokerGameState.Inactive;

			foreach ( PokerPlayer player in m_Players.Players )
			{
				player.Mobile.CloseGump<PokerTableGump>();
				player.SendGump( new PokerTableGump( this, player ) );
			}

			if ( m_Timer.Running )
				m_Timer.Stop();
		}

		public void DealHoleCards()
		{
			for ( int i = 0; i < 2; ++i ) //Simulate passing one card out at a time, going around the circle of players 2 times
				foreach ( PokerPlayer player in m_Players.Round )
					player.AddCard( m_Deck.Pop() );
		}

		public PokerPlayer AssignNextTurn()
		{
			PokerPlayer nextTurn = m_Players.Next();

			if ( nextTurn == null )
				return null;

			if ( nextTurn.RequestLeave )
			{
				m_Players.Push( nextTurn );
				nextTurn.BetStart = DateTime.Now;
				nextTurn.Action = PlayerAction.Fold;
				return nextTurn;
			}

			if ( nextTurn.IsAllIn )
			{
				m_Players.Push( nextTurn );
				nextTurn.BetStart = DateTime.Now;
				nextTurn.Action = PlayerAction.AllIn;
				return nextTurn;
			}

			if ( nextTurn.LonePlayer )
			{
				m_Players.Push( nextTurn );
				nextTurn.BetStart = DateTime.Now;
				nextTurn.Action = PlayerAction.Check;
				return nextTurn;
			}

			bool canCall = false;

			PokerPlayer currentTurn = m_Players.Peek();

			if ( currentTurn != null && currentTurn.Action != PlayerAction.Check && currentTurn.Action != PlayerAction.Fold )
				canCall = true;
			if ( currentTurn == null && m_State == PokerGameState.PreFlop )
				canCall = true;

			m_Players.Push( nextTurn );
			nextTurn.BetStart = DateTime.Now;

			ResultEntry entry = new ResultEntry( nextTurn );
			List<Card> bestCards;

			entry.Rank = nextTurn.GetBestHand( m_CommunityCards, out bestCards );
			entry.BestCards = bestCards;

			nextTurn.SendMessage( 0x22, string.Format( "You have {0}.", HandRanker.RankString( entry ) ) );
			nextTurn.Mobile.CloseGump<PokerBetGump>();
			nextTurn.SendGump( new PokerBetGump( this, nextTurn, canCall ) );

			m_NeedsGumpUpdate = true;

			return nextTurn;
		}

		public List<PokerPlayer> GetWinners( bool silent )
		{
			List<ResultEntry> results = new List<ResultEntry>();

			for ( int i = 0; i < m_Players.Round.Count; ++i )
			{
				ResultEntry entry = new ResultEntry( m_Players.Round[i] );
				List<Card> bestCards = new List<Card>();

				entry.Rank = HandRanker.GetBestHand( entry.Player.GetAllCards( m_CommunityCards ), out bestCards );
				entry.BestCards = bestCards;

				results.Add( entry );

				/*if ( !silent )
				{
					//Check if kickers needed
					PokerMessage( entry.Player.Mobile, String.Format( "I have {0}.", HandRanker.RankString( entry ) ) );
				}*/
			}

			results.Sort();

			if ( results.Count < 1 )
				return null;

			List<PokerPlayer> winners = new List<PokerPlayer>();

			for ( int i = 0; i < results.Count; ++i )
				if ( HandRanker.IsBetterThan( results[i], results[0] ) == RankResult.Same )
					winners.Add( results[i].Player );

			//IF NOT SILENT
			if ( !silent )
			{
				//Only hands that have made it past the showdown may be considered for the jackpot
				for ( int i = 0; i < results.Count; ++i )
				{
					if ( winners.Contains( results[i].Player ) )
					{
						if ( PokerDealer.JackpotWinners != null )
						{
							if ( HandRanker.IsBetterThan( results[i], PokerDealer.JackpotWinners.Hand ) == RankResult.Better )
							{
								PokerDealer.JackpotWinners = null;
								PokerDealer.JackpotWinners = new PokerDealer.JackpotInfo( winners, results[i], DateTime.Now );

								break;
							}
						}
						else
						{
							PokerDealer.JackpotWinners = new PokerDealer.JackpotInfo( winners, results[i], DateTime.Now );
							break;
						}
					}
				}

				results.Reverse();

				foreach ( ResultEntry entry in results )
				{
					//if ( !winners.Contains( entry.Player ) )
					PokerMessage( entry.Player.Mobile, string.Format( "I have {0}.", HandRanker.RankString( entry ) ) );
					/*else
					{
						if ( !HandRanker.UsesKicker( entry.Rank ) )
							PokerMessage( entry.Player, String.Format( "I have {0}.", HandRanker.RankString( entry ) ) );
						else //Hand rank uses a kicker
						{
							switch ( entry.Rank )
							{
							}
						}
					}*/
				}
			}

			return winners;
		}

		public void AwardPotToWinners( List<PokerPlayer> winners, bool silent )
		{
			//** Casino Rake - Will take a percentage of each pot awarded and place it towards
			//**				the casino jackpot for the highest ranked hand.

			if ( !silent ) //Only rake pots that have made it past the showdown.
			{
				int rake = Math.Min( (int)( m_CommunityGold * m_Dealer.Rake ), m_Dealer.RakeMax );

				if ( rake > 0 )
				{
					m_CommunityGold -= rake;
					PokerDealer.Jackpot += rake;
				}
			}
			//**

			int lowestBet = 0;

			foreach ( PokerPlayer player in winners )
				if ( player.RoundGold < lowestBet || lowestBet == 0 )
					lowestBet = player.RoundGold;

			foreach ( PokerPlayer player in m_Players.Round )
			{
				int diff = player.RoundGold - lowestBet;

				if ( diff > 0 )
				{
					player.Gold += diff;
					m_CommunityGold -= diff;
					PokerMessage( m_Dealer, string.Format( "{0}gp has been returned to {1}.", diff, player.Mobile.Name ) );
				}
			}

			int splitPot = m_CommunityGold / winners.Count;

			foreach ( PokerPlayer player in winners )
			{
				player.Gold += splitPot;
				PokerMessage( m_Dealer, string.Format( "{0} has won {1}gp.", player.Mobile.Name, splitPot ) );
			}

			m_CommunityGold = 0;
		}

		public void DoShowdown( bool silent )
		{
			List<PokerPlayer> winners = GetWinners( silent );

			if ( winners != null && winners.Count > 0 )
				AwardPotToWinners( winners, silent );

			End();

			Begin();
		}

		public void DoRoundAction() //Happens once State is changed (once per state)
		{
			if ( m_State == PokerGameState.Showdown )
				DoShowdown( false );
			else if ( m_State == PokerGameState.DealHoleCards )
			{
				DealHoleCards();
				m_State = PokerGameState.PreFlop;
				m_NeedsGumpUpdate = true;
			}
			else if ( !IsBettingRound )
			{
				int numberOfCards = 0;
				string round = string.Empty;

				switch ( m_State )
				{
					case PokerGameState.Flop: numberOfCards += 3; round = "flop"; m_State = PokerGameState.PreTurn; break;
					case PokerGameState.Turn: ++numberOfCards; round = "turn"; m_State = PokerGameState.PreRiver; break;
					case PokerGameState.River: ++numberOfCards; round = "river"; m_State = PokerGameState.PreShowdown; break;
				}

				if ( numberOfCards != 0 ) //Pop the appropriate number of cards from the top of the deck
				{
					StringBuilder sb = new StringBuilder();

					sb.Append( "The " + round + " shows: " );

					for ( int i = 0; i < numberOfCards; ++i )
					{
						Card popped = m_Deck.Pop();
						if ( i == 2 || numberOfCards == 1 )
							sb.Append( popped.Name + "." );
						else
							sb.Append( popped.Name + ", " );

						m_CommunityCards.Add( popped );
					}

					PokerMessage( m_Dealer, sb.ToString() );
					m_Players.Turn.Clear();
					//AssignNextTurn();
					m_NeedsGumpUpdate = true;
				}
			}
			else
			{
				if ( m_Players.Turn.Count == m_Players.Round.Count )
				{
					switch ( m_State )
					{
						case PokerGameState.PreFlop: m_State = PokerGameState.Flop; break;
						case PokerGameState.PreTurn: m_State = PokerGameState.Turn; break;
						case PokerGameState.PreRiver: m_State = PokerGameState.River; break;
						case PokerGameState.PreShowdown: m_State = PokerGameState.Showdown; break;
					}

					//m_Players.Turn.Clear();
				}
				else if ( m_Players.Turn.Count == 0 && m_State != PokerGameState.PreFlop ) //We need to initiate betting for this round
				{
					ResetPlayerActions();
					CheckLonePlayer();
					AssignNextTurn();
				}
				else if ( m_Players.Turn.Count == 0 && m_State == PokerGameState.PreFlop )
				{
					CheckLonePlayer();
					AssignNextTurn();
				}
			}
		}

		public void CheckLonePlayer()
		{
			int allInCount = 0;

			for ( int i = 0; i < m_Players.Round.Count; ++i )
				if ( m_Players.Round[i].IsAllIn )
					++allInCount;

			PokerPlayer loner = null;

			if ( allInCount == m_Players.Round.Count - 1 )
				for ( int i = 0; i < m_Players.Round.Count; ++i )
					if ( !m_Players.Round[i].IsAllIn )
						loner = m_Players.Round[i];

			if ( loner != null )
				loner.LonePlayer = true;
		}

		public void ResetPlayerActions()
		{
			for ( int i = 0; i < m_Players.Count; ++i )
			{
				m_Players[i].Action = PlayerAction.None;
				m_Players[i].RoundBet = 0;
			}
		}

		public int GetIndexFor( Mobile from )
		{
			for ( int i = 0; i < m_Players.Count; ++i )
				if ( m_Players[i].Mobile != null && from != null )
					if ( m_Players[i].Mobile.Serial == from.Serial )
						return i;

			return -1;
		}

		public PokerPlayer GetPlayer( Mobile from )
		{
			return GetIndexFor( from ) == -1 ? null : m_Players[GetIndexFor( from )];
		}

		public int GetIndexForPlayerInRound( Mobile from )
		{
			for ( int i = 0; i < m_Players.Round.Count; ++i )
				if ( m_Players.Round[i].Mobile != null && from != null )
					if ( m_Players.Round[i].Mobile.Serial == from.Serial )
						return i;

			return -1;
		}

		public void AddPlayer( PokerPlayer player )
		{
			Mobile from = player.Mobile;

			if ( from == null )
				return;

			if ( !m_Dealer.InRange( from.Location, 8 ) )
				from.PrivateOverheadMessage( Server.Network.MessageType.Regular, 0x22, true, "I am too far away to do that", from.NetState );
			else if ( GetIndexFor( from ) != -1 )
				from.SendMessage( 0x22, "You are already seated at this table" );
			else if ( m_Players.Count >= m_Dealer.MaxPlayers )
				from.SendMessage( 0x22, "Sorry, that table is full" );
			/*else if ( TournamentSystem.TournamentCore.SignedUpTeam( from ) != null || TournamentSystem.TournamentCore.FindTeam( from ) != null )
				from.SendMessage( 0x22, "You may not join a poker game while signed up for a tournament." );*/
			else if ( Banker.Withdraw( from, player.Gold ) )
			{
				Point3D seat = Point3D.Zero;

				foreach ( Point3D seats in m_Dealer.Seats )
					if ( !m_Dealer.SeatTaken( seats ) )
					{
						seat = seats;
						break;
					}

				if ( seat == Point3D.Zero )
				{
					from.SendMessage( 0x22, "Sorry, that table is full" );
					return;
				}

				player.Game = this;
				player.Seat = seat;
				player.TeleportToSeat();
				m_Players.Players.Add( player );

				( (PlayerMobile)from ).PokerGame = this;
				from.SendMessage( 0x22, "You have been seated at the table" );

				if ( m_Players.Count == 1 && !GameBackup.PokerGames.Contains( this ) )
					GameBackup.PokerGames.Add( this );
				else if ( m_State == PokerGameState.Inactive && m_Players.Count > 1 && !m_Dealer.TournamentMode )
					Begin();
				else if ( m_State == PokerGameState.Inactive && m_Players.Count >= m_Dealer.MaxPlayers && m_Dealer.TournamentMode )
				{
					m_Dealer.TournamentMode = false;
					Begin();
				}

				player.Mobile.CloseGump<PokerTableGump>();
				player.SendGump( new PokerTableGump( this, player ) );
				m_NeedsGumpUpdate = true;
			}
			else
				from.SendMessage( 0x22, "Your bank box lacks the funds to join this poker table" );
		}

		public void RemovePlayer( PokerPlayer player )
		{
			Mobile from = player.Mobile;

			if ( from != null && m_Players.Contains( player ) )
			{
				m_Players.Players.Remove( player );

				if ( m_Players.Peek() == player ) //It is currently their turn, fold them.
				{
					player.Mobile.CloseGump<PokerBetGump>();
					m_Timer.m_LastPlayer = null;
					player.Action = PlayerAction.Fold;
				}

				if ( m_Players.Round.Contains( player ) )
					m_Players.Round.Remove( player );
				if ( m_Players.Turn.Contains( player ) )
					m_Players.Turn.Remove( player );

				if ( m_Players.Round.Count == 0 )
				{
					player.Gold += m_CommunityGold;
					m_CommunityGold = 0;

					if ( GameBackup.PokerGames.Contains( this ) )
						GameBackup.PokerGames.Remove( this );
				}

				if ( player.Gold > 0 )
				{
					if ( from.BankBox == null ) //Should NEVER happen, but JUST IN CASE!
					{
						Utility.PushColor( ConsoleColor.Red );
						Console.WriteLine( "WARNING: Player \"{0}\" with account \"{1}\" had null bankbox while trying to deposit {2} gold. Player will NOT recieve their gold.", from.Name, ( from.Account == null ? "(-null-)" : from.Account.Username ), player.Gold );
						Utility.PopColor();

						try
						{
							using ( System.IO.StreamWriter op = new System.IO.StreamWriter( "poker_error.log", true ) )
								op.WriteLine( "WARNING: Player \"{0}\" with account \"{1}\" had null bankbox while poker script was trying to deposit {2} gold. Player will NOT recieve their gold.", from.Name, ( from.Account == null ? "(-null-)" : from.Account.Username ), player.Gold );
						}
						catch { }

						from.SendMessage( 0x22, "WARNING: Could not find your bankbox. All of your poker money has been lost in this error. Please contact a Game Master to resolve this issue." );
					}
					else
					{
						Banker.Deposit( from.BankBox, player.Gold );
						from.SendMessage( 0x22, "{0}gp has been deposited into your bankbox.", player.Gold );
					}
				}

				player.CloseAllGumps();
				( (PlayerMobile)from ).PokerGame = null;
				from.Location = m_Dealer.ExitLocation;
				from.Map = m_Dealer.ExitMap;
				from.SendMessage( 0x22, "You have left the table" );

				m_NeedsGumpUpdate = true;
			}
		}
	}

	public class ResultEntry : IComparable
	{
		private PokerPlayer m_Player;
		private List<Card> m_BestCards;
		private HandRank m_Rank;

		public PokerPlayer Player { get { return m_Player; } }
		public List<Card> BestCards { get { return m_BestCards; } set { m_BestCards = value; } }
		public HandRank Rank { get { return m_Rank; } set { m_Rank = value; } }

		public ResultEntry( PokerPlayer player )
		{
			m_Player = player;
		}

		#region IComparable Members

		public int CompareTo( object obj )
		{
			if ( obj is ResultEntry )
			{
				ResultEntry entry = (ResultEntry)obj;
				RankResult result = HandRanker.IsBetterThan( this, entry );

				if ( result == RankResult.Better )
					return -1;
				if ( result == RankResult.Worse )
					return 1;
			}

			return 0;
		}

		#endregion
	}

	public class PokerGameTimer : Timer
	{
		PokerGame m_Game;
		PokerGameState m_LastState;
		public PokerPlayer m_LastPlayer;
		public bool hasWarned;

		public PokerGameTimer( PokerGame game )
			: base( TimeSpan.FromSeconds( 1.0 ), TimeSpan.FromSeconds( 1.0 ) )
		{
			m_Game = game;
			m_LastState = PokerGameState.Inactive;
			m_LastPlayer = null;
		}

		protected override void OnTick()
		{
			if ( m_Game.State != PokerGameState.Inactive && m_Game.Players.Count < 2 )
				m_Game.End();

			for ( int i = 0; i < m_Game.Players.Count; ++i )
				if ( !m_Game.Players.Round.Contains( m_Game.Players[i] ) )
					if ( m_Game.Players[i].RequestLeave )
						m_Game.RemovePlayer( m_Game.Players[i] );

			if ( m_Game.NeedsGumpUpdate )
			{
				foreach ( PokerPlayer player in m_Game.Players.Players )
				{
					player.Mobile.CloseGump<PokerTableGump>();
					player.SendGump( new PokerTableGump( m_Game, player ) );
				}

				m_Game.NeedsGumpUpdate = false;
			}

			if ( m_Game.State != m_LastState && m_Game.Players.Round.Count > 1 )
			{
				m_LastState = m_Game.State;
				m_Game.DoRoundAction();
				m_LastPlayer = null;
			}

			if ( m_Game.Players.Peek() != null )
			{
				if ( m_LastPlayer == null )
					m_LastPlayer = m_Game.Players.Peek(); //Changed timer from 25.0 and 30.0 to 45.0 and 60.0

				if ( m_LastPlayer.BetStart.AddSeconds( 45.0 ) <= DateTime.Now /*&& m_LastPlayer.Mobile.HasGump( typeof( PokerBetGump ) )*/ && !hasWarned )
				{
					m_LastPlayer.SendMessage( 0x22, "You have 15 seconds left to make a choice. (You will automatically fold if no choice is made)" );
					hasWarned = true;
				}
				else if ( m_LastPlayer.BetStart.AddSeconds( 60.0 ) <= DateTime.Now /*&& m_LastPlayer.Mobile.HasGump( typeof( PokerBetGump ) )*/ )
				{
					PokerPlayer temp = m_LastPlayer;
					m_LastPlayer = null;

                    temp.Mobile.CloseGump<PokerBetGump>();
					temp.Action = PlayerAction.Fold;
					hasWarned = false;
				}
			}
		}
	}
}
