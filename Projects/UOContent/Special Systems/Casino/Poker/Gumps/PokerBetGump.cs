using System;
using System.Collections.Generic;

using Server;
using Server.Gumps;
using Server.Network;

namespace Server.Poker
{
	public class PokerBetGump : Gump
	{
		private const int COLOR_WHITE = 0xFFFFFF;
		private const int COLOR_YELLOW = 0xFFFF00;
		private const int COLOR_GOLD = 0xFFD700;
		private const int COLOR_BLACK = 0x000001;
		private const int COLOR_GREEN = 0x00FF00;
		private const int COLOR_OFFWHITE = 0xFFFACD;

		private bool m_CanCall;
		private PokerGame m_Game;
		private PokerPlayer m_Player;

		public PokerBetGump( PokerGame game, PokerPlayer player, bool canCall )
			: base( 460, 400 )
		{
			m_CanCall = canCall;
			m_Game = game;
			m_Player = player;

			this.Closable = false;
			this.Disposable = false;
			this.Draggable = true;
			this.Resizable = false;
			this.AddPage( 0 );

			//this.AddImageTiled( 0, 0, 170, 165, 2624 );
			//this.AddImageTiled( 2, 2, 166, 161, 3604 );
			//this.AddImageTiled( 4, 4, 162, 157, 3504 );
			//this.AddImageTiled( 6, 6, 158, 153, 3604 );
			//this.AddAlphaRegion( 6, 6, 158, 153 );
            this.AddBackground(0, 0, 160, 155, 9270);

			this.AddRadio( 14, 10, 9727, 9730, true, ( canCall ? (int)Buttons.Call : (int)Buttons.Check ) );
			this.AddRadio( 14, 40, 9727, 9730, false, (int)Buttons.Fold );
			this.AddRadio( 14, 70, 9727, 9730, false, (int)Buttons.AllIn );
			this.AddRadio( 14, 100, 9727, 9730, false, ( canCall ? (int)Buttons.Raise : (int)Buttons.Bet ) );

			this.AddHtml( 45, 14, 60, 45, Color( canCall ? "Call" : "Check", COLOR_WHITE ), false, false );

			if ( canCall )
				this.AddHtml( 75, 14, 60, 22, Color( Center( ( m_Game.CurrentBet - player.RoundBet >= player.Gold ? "all-in" : string.Format( "{0}", ( m_Game.CurrentBet - m_Player.RoundBet ).ToString( "#,###" ) ) ) ), COLOR_GREEN ), false, false );

			this.AddHtml( 45, 44, 60, 45, Color( "Fold", COLOR_WHITE ), false, false );
			this.AddHtml( 45, 74, 60, 45, Color( "All In", COLOR_WHITE ), false, false );
			this.AddHtml( 45, 104, 60, 45, Color( canCall ? "Raise" : "Bet", COLOR_WHITE ), false, false );
			this.AddTextEntry( 85, 104, 60, 22, 455, (int)Buttons.txtBet, game.Dealer.BigBlind.ToString() );

			this.AddButton( 95, 132, 247, 248, (int)Buttons.Okay, GumpButtonType.Reply, 0 );
		}

		public enum Buttons
		{
			None,
			Check,
			Call,
			Fold,
			Bet,
			Raise,
			AllIn,
			txtBet,
			Okay
		}

		public string Center( string text )
		{
			return string.Format( "<CENTER>{0}</CENTER>", text );
		}

		public string Color( string text, int color )
		{
			return string.Format( "<BASEFONT COLOR=#{0:X6}>{1}</BASEFONT>", color, text );
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			Mobile from = state.Mobile;

			if ( from == null )
				return;

			if ( m_Game.Players.Peek() != m_Player )
				return;

			if ( info.ButtonID == 8 ) //Okay
			{
				if ( info.IsSwitched( (int)Buttons.Check ) )
				{
					m_Player.Action = PlayerAction.Check;
				}
				else if ( info.IsSwitched( (int)Buttons.Call ) )
				{
					if ( m_Game.CurrentBet >= m_Player.Gold )
					{
						m_Player.Forced = true;
						m_Player.Action = PlayerAction.AllIn;
					}
					else
					{
						m_Player.Bet = m_Game.CurrentBet - m_Player.RoundBet;
						m_Player.Action = PlayerAction.Call;
					}

				}
				else if ( info.IsSwitched( (int)Buttons.Fold ) )
				{
					m_Player.Action = PlayerAction.Fold;
				}
				else if ( info.IsSwitched( (int)Buttons.Bet ) )
				{
					int bet = 0;
					TextRelay relay = info.GetTextEntry( (int)Buttons.txtBet);

					try { bet = Convert.ToInt32( info.GetTextEntry( (int)Buttons.txtBet).Text ); }
					catch { }

					if ( bet < m_Game.Dealer.BigBlind )
					{
						from.SendMessage( 0x22, "Your must bet at least {0}gp.", m_Game.BigBlind );

						from.CloseGump<PokerBetGump>();
						from.SendGump( new PokerBetGump( m_Game, m_Player, m_CanCall ) );
					}
					else if ( bet > m_Player.Gold )
					{
						from.SendMessage( 0x22, "You cannot bet more gold than you currently have!" );

						from.CloseGump<PokerBetGump>();
						from.SendGump( new PokerBetGump( m_Game, m_Player, m_CanCall ) );
					}
					else if ( bet == m_Player.Gold )
					{
						m_Player.Action = PlayerAction.AllIn;
					}
					else
					{
						m_Player.Bet = bet;
						m_Player.Action = PlayerAction.Bet;
					}
				}
				else if ( info.IsSwitched( (int)Buttons.Raise ) ) //Same as bet, but add value to current bet
				{
					int bet = 0;
					TextRelay relay = info.GetTextEntry( (int)Buttons.txtBet );

					try { bet = Convert.ToInt32( info.GetTextEntry( (int)Buttons.txtBet ).Text ); }
					catch { }

					if ( bet < 100 )
					{
						from.SendMessage( 0x22, "If you are going to raise a bet, it needs to be by at least 100gp." );

						from.CloseGump<PokerBetGump>();
						from.SendGump( new PokerBetGump( m_Game, m_Player, m_CanCall ) );
					}
					else if ( bet + m_Game.CurrentBet > m_Player.Gold )
					{
						from.SendMessage( 0x22, "You do not have enough gold to raise by that much." );

						from.CloseGump<PokerBetGump>();
						from.SendGump( new PokerBetGump( m_Game, m_Player, m_CanCall ) );
					}
					else if ( bet + m_Game.CurrentBet == m_Player.Gold )
					{
						m_Player.Action = PlayerAction.AllIn;
					}
					else
					{
						m_Player.Bet = bet;
						m_Player.Action = PlayerAction.Raise;
					}
				}
				else if ( info.IsSwitched( (int)Buttons.AllIn ) )
				{
					m_Player.Action = PlayerAction.AllIn;
				}
			}
		}
	}
}
