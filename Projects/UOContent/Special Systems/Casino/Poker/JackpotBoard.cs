using System;
using System.Text;

using Server.Poker;
using Server.Network;

namespace Server.Items
{
	[Flippable( 0x1E5E, 0x1E5F )]
	public class JackpotBoard : Item
	{
		[Constructible]
		public JackpotBoard()
			: base( 0x1E5E )
		{
			Movable = false;
			Name = "Poker Jackpot";
			Hue = 1161;
		}

		public override void OnDoubleClick( Mobile from )
		{ 

			if ( from.InRange( this.Location, 4 ) && from.NetState != null )
			{
				if ( PokerDealer.Jackpot > 0 && PokerDealer.JackpotWinners != null && PokerDealer.JackpotWinners.Winners.Count > 0 )
				{
					if ( PokerDealer.JackpotWinners.Winners.Count > 1 )
					{
						StringBuilder sb = new StringBuilder( string.Format( "The jackpot is {0} gold. ", PokerDealer.Jackpot.ToString( "#,###" ) ) );

						sb.Append( "It is currently split by: " );

						for ( int i = 0; i < PokerDealer.JackpotWinners.Winners.Count; ++i )
						{
							if ( PokerDealer.JackpotWinners.Winners[i].Mobile != null )
								sb.Append( PokerDealer.JackpotWinners.Winners[i].Mobile.Name );
							else
								sb.Append( "(-null-)" );

							if ( PokerDealer.JackpotWinners.Winners.Count == 2 && i == 0 )
								sb.Append( " and " );
							else if ( i != PokerDealer.JackpotWinners.Winners.Count - 2 )
								sb.Append( ", " );
							else
								sb.Append( " and " );
						}

						sb.Append( string.Format( " leading with {0}", HandRanker.RankString( PokerDealer.JackpotWinners.Hand ) ) );

						DisplayMessage( from, sb.ToString() );
						return;
					}
					else if ( PokerDealer.JackpotWinners.Winners[0] != null && PokerDealer.JackpotWinners.Winners[0].Mobile != null )
					{
						DisplayMessage( from, string.Format( "The jackpot is {0} gold. {1} leads with {2}", PokerDealer.Jackpot.ToString( "#,###" ), PokerDealer.JackpotWinners.Winners[0].Mobile.Name, HandRanker.RankString( PokerDealer.JackpotWinners.Hand ) ) );
						return;
					}
				}

				DisplayMessage( from, "Currently No Jackpot" );
			}
			else
				from.SendMessage( 0x22, "That is too far away." );
		}

		private void DisplayMessage( Mobile from, string text )
		{
			//from.NetState.Send( new AsciiMessage( Serial, ItemID, MessageType.Regular, Hue, 3, Name, text ) );
            from.SendAsciiMessage((int)Hue, text);
		}

		public JackpotBoard( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );
		}
	}
}
