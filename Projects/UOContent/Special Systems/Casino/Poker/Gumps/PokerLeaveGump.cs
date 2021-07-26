using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Poker
{
	public class PokerLeaveGump : Gump
	{
		private PokerGame m_Game;

		public PokerLeaveGump( Mobile from, PokerGame game )
			: base( 50, 50 )
		{
			m_Game = game;

			this.Closable = true;
			this.Disposable = true;
			this.Draggable = true;
			this.Resizable = false;
			this.AddPage( 0 );
			this.AddImageTiled( 18, 15, 350, 180, 9274 );
			//this.AddAlphaRegion( 23, 20, 340, 170 );
            this.AddBackground(0, 0, 390, 200, 9270);
			this.AddLabel( 133, 25, 28, @"Leave Poker Table" );
			this.AddImageTiled( 42, 47, 301, 3, 96 );
			this.AddLabel( 60, 62, 68, @"You are about to leave a game of Poker." );
			this.AddImage( 33, 38, 95, 68 );
			this.AddImage( 342, 38, 97, 68 );
			this.AddLabel( 43, 80, 68, @"Are you sure you want to cash-out and leave the" );
			this.AddLabel( 48, 98, 68, @"table? You will auto fold, and any current bets" );
			this.AddLabel( 40, 116, 68, @"will be lost. Winnings will be deposited in your bank." );
			this.AddButton( 163, 155, 247, 248, (int)Handlers.btnOkay, GumpButtonType.Reply, 0 );
		}

		public enum Handlers
		{
			None,
			btnOkay
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			Mobile from = state.Mobile;

			if ( from == null )
				return;

			PokerPlayer player = m_Game.GetPlayer( from );

			if ( player != null )
			{
				if ( info.ButtonID == 1 )
				{
					if ( m_Game.State == PokerGameState.Inactive )
					{
						if ( m_Game.Players.Contains( player ) )
							m_Game.RemovePlayer( player );
						return;
					}


					if ( player.RequestLeave )
						from.SendMessage( 0x22, "You have already submitted a request to leave." );
					else
					{
						from.SendMessage( 0x22, "You have submitted a request to leave the table." );
						player.RequestLeave = true;
					}
				}
			}
		}
	}
}
