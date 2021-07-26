using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Poker;

namespace Server.Poker
{
	public class PokerJoinGump : Gump
	{
		private PokerGame m_Game;

		public PokerJoinGump( Mobile from, PokerGame game )
			: base( 50, 50 )
		{
			m_Game = game;

			this.Closable = true;
			this.Disposable = true;
			this.Draggable = true;
			this.Resizable = false;
			this.AddPage( 0 );
            this.AddBackground(0, 0, 385, 393, 9270);
			this.AddImageTiled( 18, 15, 350, 320, 9274 );
			//this.AddAlphaRegion( 23, 19, 340, 310 );
            this.AddLabel(125, 10, 28, @"The Shard's Texas Hold-em");
			this.AddLabel( 133, 25, 28, @" Join Poker Table" );
			this.AddImageTiled( 42, 47, 301, 3, 96 );
			this.AddLabel( 65, 62, 68, @"You are about to join a game of Poker." );
			this.AddImage( 33, 38, 95, 68 );
			this.AddImage( 342, 38, 97, 68 );
			this.AddLabel( 52, 80, 68, @"All bets involve real gold and no refunds will be" );
			this.AddLabel( 54, 98, 68, @"given. If you feel uncomfortable losing gold or" );
			this.AddLabel( 40, 116, 68, @"are unfamiliar with the rules of Texas Hold'em, you" );
			this.AddLabel( 100, 134, 68, @"are advised against proceeding." );

			this.AddLabel( 122, 161, 1149, @"Small Blind:" );
			this.AddLabel( 129, 181, 1149, @"Big Blind:" );
			this.AddLabel( 123, 201, 1149, @"Min Buy-In:" );
			this.AddLabel( 120, 221, 1149, @"Max Buy-In:" );
			this.AddLabel( 110, 241, 1149, @"Bank Balance:" );
			this.AddLabel( 101, 261, 1149, @"Buy-In Amount:" );

			this.AddLabel( 200, 161, 148, m_Game.Dealer.SmallBlind.ToString( "#,###" ) + "gp" );
			this.AddLabel( 200, 181, 148, m_Game.Dealer.BigBlind.ToString( "#,###" ) + "gp" );
			this.AddLabel( 200, 201, 148, m_Game.Dealer.MinBuyIn.ToString( "#,###" ) + "gp" );
			this.AddLabel( 200, 221, 148, m_Game.Dealer.MaxBuyIn.ToString( "#,###" ) + "gp" );

			int balance = Banker.GetBalance( from );
			int balancehue = 28;
			int layout = 0;

			if ( balance >= m_Game.Dealer.MinBuyIn )
			{
				balancehue = 266;
				layout = 1;
			}

			this.AddLabel( 200, 241, balancehue, balance.ToString( "#,###" ) + "gp" );

			if ( layout == 0 )
			{
				this.AddLabel( 200, 261, 1149, "(not enough gold)" );
				this.AddButton( 163, 292, 242, 241, (int)Handlers.btnCancel, GumpButtonType.Reply, 0 );
			}
			else if ( layout == 1 )
			{
				this.AddImageTiled( 200, 261, 80, 19, 0xBBC );
				this.AddAlphaRegion( 200, 261, 80, 19 );
				this.AddTextEntry( 203, 261, 77, 19, 68, (int)Handlers.txtBuyInAmount, m_Game.Dealer.MinBuyIn.ToString() );
				this.AddButton( 123, 292, 247, 248, (int)Handlers.btnOkay, GumpButtonType.Reply, 0 );
				this.AddButton( 200, 292, 242, 241, (int)Handlers.btnCancel, GumpButtonType.Reply, 0 );
			}
		}

		public enum Handlers
		{
			btnOkay = 1,
			btnCancel,
			txtBuyInAmount
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			Mobile from = state.Mobile;
			int buyInAmount = 0;

			if ( info.ButtonID == 1 )
			{
				int balance = Banker.GetBalance( from );
				if ( balance >= m_Game.Dealer.MinBuyIn )
				{
					try
					{
						buyInAmount = Convert.ToInt32( ( info.TextEntries[0] ).Text );
					}
					catch
					{
						from.SendMessage( 0x22, "Use numbers without commas to input your buy-in amount (ie 25000)" );
						return;
					}

					if ( buyInAmount <= balance && buyInAmount >= m_Game.Dealer.MinBuyIn && buyInAmount <= m_Game.Dealer.MaxBuyIn )
					{
						PokerPlayer player = new PokerPlayer( from );
						player.Gold = buyInAmount;
						m_Game.AddPlayer( player );
					}
					else
						from.SendMessage( 0x22, "You may not join with that amount of gold. Minimum buy-in: " + Convert.ToString( m_Game.Dealer.MinBuyIn ) +
							", Maximum buy-in: " + Convert.ToString( m_Game.Dealer.MaxBuyIn ) );
				}
				else
					from.SendMessage( 0x22, "You may not join with that amount of gold. Minimum buy-in: " + Convert.ToString( m_Game.Dealer.MinBuyIn ) +
						", Maximum buy-in: " + Convert.ToString( m_Game.Dealer.MaxBuyIn ) );
			}
			else if ( info.ButtonID == 2 )
				return;
		}
	}
}
