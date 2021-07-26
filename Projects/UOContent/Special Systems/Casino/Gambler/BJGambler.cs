using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Network;
using Server.Misc;

namespace Server.Mobiles
{
    public class BJGambler : BaseCreature
	{
		private int m_current_card = 53;
		private int [] Cardz = new int[53];
		private int [] dealercards = new int[5];
		private int [] playercards = new int[5];
		private int [] gamestats = new int[3];
		private int playerbet = 500;
		private int dwin = 0;
		private int pwin = 0;
		private int round = 0;
		private int goldbp = 0;
		private bool roundend;
		private bool dealercardhidden;
		private bool busy;
		private bool pbj = false;
		private bool dbj = false;
		private bool start = true;
		private string bjmsg = "";
		private Mobile m_player;

        [Constructible]
        public BJGambler() : base( AIType.AI_Melee, FightMode.None, 10, 1, 0.2, 0.4 ) 
        {
            SetStr( 10, 30 );
            SetDex( 10, 30 );
            SetInt( 10, 30 );
			Name = "Nestor";
            NameHue = 0x35;
			Title = "the gambler";
            Body = 0x190;
			Hue = 1521;
            Blessed = true;
			SpeechHue = 53;
            Item hair = new Item( 0x203C );
            hair.Hue = 1161;
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem( hair );
            Item beard = new Item( 0x204C );
            beard.Hue = hair.Hue;
            beard.Layer = Layer.FacialHair;
            beard.Movable = false;
            AddItem( beard );
            Item hat = new StrawHat();
			hat.Hue = 1175;
	    	AddItem( hat );
			Item pants = new LongPants();
			pants.Hue = 1161;
	    	AddItem( pants );
			Item shirt = new FancyShirt();
			shirt.Hue = 1175;
	    	AddItem( shirt );
			Item shoes = new Sandals();
			shoes.Hue = 1175;
	    	AddItem( shoes );
			Container pack = new Backpack();
			pack.DropItem( new Gold( 5, 500 ) );
			pack.Movable = false;
			pack.Visible = false;
			AddItem( pack );
			for ( int i = 0; i <= 2; ++i ){gamestats[i]=0;}
		}

		public override bool ClickTitle{ get{ return false; } }

		public override bool HandlesOnSpeech( Mobile from )
		{
			if ( from.InRange( this.Location, 3 ) ){return true;}
			return base.HandlesOnSpeech( from );
		}

		public override void OnSpeech( SpeechEventArgs e )
		{
			base.OnSpeech( e );
			Mobile from = e.Mobile;
			string message;
			checkgold(from);
			if ( from.InRange( this, 3 ))
			{
				if (m_player != null)
				{
					if ( m_player.NetState == null ){busy = false;}
				}
				if (e.Speech.ToLower() == "reset")
				{
                    if ( from.AccessLevel >= AccessLevel.Seer )
                    {
                    	busy = false;
                    	message = "I am no longer busy.";
                    	this.Say( message );
                    }
				}
				else if (e.Speech.ToLower() == "blackjack" || e.Speech.ToLower() == "Blackjack" )
				{
					if (!busy)
					{
						playerbet = 500;
						busy = true;
						roundend = true;
						m_current_card = 53;
						dealercardhidden = false;
						dwin = 0;
						pwin = 0;
						dealercards[0]=12;
						playercards[0]=13;
						dealercards[1]=11;
						playercards[1]=26;
						for ( int i = 2; i <= 4; ++i )
						{
							dealercards[i] = 0;
							playercards[i] = 0;
						}
						bjmsg = "Good Luck!";
						message = "So, you want to try your luck.";
						this.Say( message );
						m_player = from;
						playblackjack( from );
					}
					else if ( m_player.NetState == from.NetState )
					{
						message = "We are already playing cards.";
						this.Say( message );
					}
					else
					{
						message = "I am busy playing cards.";
						this.Say( message );
					}
				}
			}
		} 

		public void checkgold(Mobile from)
        {
			goldbp = from.Backpack.GetAmount(typeof(Gold));       	
        }
       	
		public void payplayer( Mobile from, int quantity)
        {
			from.AddToBackpack( new Gold( quantity ) );
			goldbp = from.Backpack.GetAmount(typeof(Gold));
        }

        public bool paydealer( Mobile from, int quantity)
        {
			goldbp = from.Backpack.GetAmount(typeof(Gold));
			if (goldbp >= quantity)
			{
				from.Backpack.ConsumeTotal( typeof( Gold ), quantity );
				goldbp = from.Backpack.GetAmount(typeof(Gold));
				return true;
			}
			else
			{
				return false;
			}	
        }

		public string CardSuit( int card )
		{
			if (card >= 1 && card <= 13){return "\u2663";}
			else if (card >= 14 && card <= 26){return "\u25C6";}
			else if (card >= 27 && card <= 39){return "\u2665";}
			else {return "\u2660";}
		}

		public string CardName( int card )
		{
			while (card>13){card -= 13;}
			if(card==1){return "A";}
			else if(card == 11){return "J";}
			else if(card == 12){return "Q";}
			else if(card == 13){return "K";}
			else {return "" + card;}
		}

		public int CardValue( int card )
		{
			while (card > 13){card -= 13;}
			if(card == 1){return 11;}
			if(card > 10){return 10;}
			return card;
		}

		public int cardcolor( string cardtemp )
		{
			if ( cardtemp == "\u25C6" || cardtemp == "\u2665" ){return 32;}
			return 0;
		}

		public int CardValue2( int card )
		{
			while (card > 13){card -= 13;}
			if(card == 1){return 14;}
			return card;
		}

		public void ShuffleCards( )
		{
			int i, tempcard , tempcard2;
			for ( i = 1; i < 53; ++i ){Cardz[i]=i;}
			for ( i = 52; i >= 1; --i )
			{
				tempcard = Utility.Random( i ) + 1;
				tempcard2 = Cardz[tempcard];
				Cardz[tempcard] = Cardz[i];
				Cardz[i] = tempcard2;
			}
			m_current_card = 1;
		}

		public int pickcard(Mobile from)
		{
			if (m_current_card == 53)
			{
				Effects.PlaySound( from.Location, from.Map, 0x3D );
				ShuffleCards( );
			}
			return Cardz[m_current_card++];
		}

		public void playblackjack( Mobile from )
		{
			from.SendGump( new BlackjackGump( this, this ) );
		}

		public override bool DisallowAllMoves
		{
			get { return true; }
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.AccessLevel >= AccessLevel.Seer ){from.SendGump( new BJGamblerStatsGump( this ) );}
			else{base.OnDoubleClick( from );}
		}

		public BJGambler( Serial serial ) : base( serial )
		{
		}

		public class BJGamblerStatsGump : Gump
		{
			private BJGambler m_From;

			public BJGamblerStatsGump( BJGambler gambler ) : base( 10, 10 )
			{
				m_From = gambler;
				AddPage( 0 );
				AddBackground( 30, 100, 90, 100, 5120 );
				AddLabel( 45, 100, 70, "Blackjack" );
				AddLabel( 45, 120, 600, "Wins: "+m_From.gamestats[0] );
				AddLabel( 45, 135, 600, "Loss: "+m_From.gamestats[1] );
				AddLabel( 45, 150, 600, "Tied: "+m_From.gamestats[2] );
				AddLabel(  45, 172, 1500, "Reset" );
				AddButton( 85, 176, 2117, 2118, 1, GumpButtonType.Reply, 0 );
			}

			public override void OnResponse( NetState sender, RelayInfo info )
			{
				switch ( info.ButtonID )
				{
					case 1:
					{
						for ( int i = 0; i <= 2; ++i ){m_From.gamestats[i] = 0;}
						break;
					}
				}
			}
		}

		public class BlackjackGump : Gump
		{
			private BJGambler m_From;

			public BlackjackGump( Mobile mobile, BJGambler gambler ) : base( 10, 10 )
			{
				m_From = gambler;
				int i, dealervalue = 0, temp = 0;
				string cardtemp, scoredmsg, scorepmsg;
				Closable = false;
				AddPage( 0 );
				AddImageTiled( 30, 100, 460, 280, 2624 );
				AddAlphaRegion( 99, 104, 460, 230 );
				AddImage( 10, 82, 2620 ); 
				AddImage( 490, 82, 2622 ); 
				AddImage( 10, 380, 2626 ); 
				AddImage( 490, 380, 2628 ); 
				AddImageTiled( 30, 82, 460, 18, 2621 ); 
				AddImageTiled( 30, 380, 460, 18, 2627 ); 
				AddImageTiled( 10, 100, 20, 280, 2623 ); 
				AddImageTiled( 490, 100, 20, 280, 2625 );
				AddLabel( 30, 111, 1500, "Nestor:" );
				AddLabel( 30, 231, 600, "You:" );
				AddImageTiled( 512, 399, 80, 93, 9274 );
				AddImage( 524, 423, 9811 );
				AddLabel( 532, 406, 600, ""+m_From.goldbp );
				AddItem ( 521, 456, 3823 );
				if (m_From.start == true)
				{
					for ( i = 2; i <= 4; ++i )
					{
						m_From.dealercards[i] = 0;
						m_From.playercards[i] = 0;
					}
				}
				for ( i = 0; i <= 4; ++i )
				{
					temp = m_From.dealercards[i];
					if (temp > 0)
					{
						if (m_From.start == true)
						{
							AddBackground( 70 + ((i+1)*45), 117, 35, 50, 2171 );
							AddImage( 73 + ((i+1)*45), 110, 10897 );
						}
						else if (!m_From.dealercardhidden || (m_From.dealercardhidden && i > 0))
						{
							cardtemp = m_From.CardSuit( temp ); //129
							AddBackground( 70 + ((i+1)*45), 117, 35, 50, 2171 );
							AddLabel( 85 + ((i+1)*45), 143, m_From.cardcolor( cardtemp ), cardtemp );
							AddLabel( 77 + ((i+1)*45), 122, 1500, m_From.CardName( temp ) );
						
							dealervalue += m_From.CardValue( temp );
						}
						else
						{
							AddBackground( 70 + ((i+1)*45), 117, 35, 50, 2171 );
							AddImage( 73 + ((i+1)*45), 110, 10897 );
						}
					}
				}
				for ( i = 0; i <= 4; ++i )
				{
					temp = m_From.playercards[i];
					if (temp > 0 && m_From.round == 0 && m_From.start == true)
					{
						cardtemp = m_From.CardSuit( temp );
						AddBackground( 70 + ((i+1)*45), 237, 35, 50, 2171 );
						AddImage( 73 + ((i+1)*45), 230, 10897 );
						AddButton( 35, 347, 2151, 2154, 1, GumpButtonType.Reply, 0 );
						AddLabel( 69, 351, 800, "Deal" );
						AddButton( 420, 188, 4500, 4500, 5, GumpButtonType.Reply, 0 );
						AddButton( 420, 270, 4504, 4504, 6, GumpButtonType.Reply, 0 );
						AddButton( 411, 114, 1027, 1028, 7, GumpButtonType.Reply, 0 );
						AddLabel( 418, 173, 800, "your bet:" );
						AddImage( 417, 233, 51 );
						AddLabel( 433, 246, 800, ""+m_From.playerbet );
					}
					else if (temp > 0 && m_From.round == 0)
					{
						cardtemp = m_From.CardSuit( temp );
						AddBackground( 70 + ((i+1)*45), 237, 35, 50, 2171 );
						AddLabel( 85 + ((i+1)*45), 263, m_From.cardcolor( cardtemp ), cardtemp );
						AddLabel( 77 + ((i+1)*45), 242, 600, m_From.CardName( temp ) );
						AddButton( 35, 347, 2151, 2154, 1, GumpButtonType.Reply, 0 );
						AddLabel( 69, 351, 800, "Deal" );
						AddButton( 420, 188, 4500, 4500, 5, GumpButtonType.Reply, 0 );
						AddButton( 420, 270, 4504, 4504, 6, GumpButtonType.Reply, 0 );
						AddButton( 411, 114, 1027, 1028, 7, GumpButtonType.Reply, 0 );
						AddLabel( 418, 173, 800, "your bet:" );
						AddImage( 417, 233, 51 );
						AddLabel( 433, 246, 800, ""+m_From.playerbet );
						if ((m_From.dwin > 0) && (m_From.pwin > 0))
						{
							AddLabel( 30, 132, 54, "$"+m_From.dwin );
							AddLabel( 30, 252, 54, "$"+m_From.pwin );
						}
						else if (m_From.dwin > 0)
						{
							AddLabel( 30, 132, 70, "+ $"+m_From.dwin );
							AddLabel( 30, 252, 38, "- $"+m_From.dwin );
						}
						else if (m_From.pwin > 0)
						{
							AddLabel( 30, 132, 38, "- $"+m_From.pwin );
							AddLabel( 30, 252, 70, "+ $"+m_From.pwin );
						}
					}
					else if (temp > 0 && m_From.round != 0)
					{
						cardtemp = m_From.CardSuit( temp );
						AddBackground( 70 + ((i+1)*45), 237, 35, 50, 2171 );
						AddLabel( 85 + ((i+1)*45), 263, m_From.cardcolor( cardtemp ), cardtemp );
						AddLabel( 77 + ((i+1)*45), 242, 600, m_From.CardName( temp ) );
						AddButton( 186, 343, 2117, 2118, 2, GumpButtonType.Reply, 0 );
						AddLabel( 206, 340, 800, "Hit" );
						AddButton( 245, 343, 2117, 2118, 3, GumpButtonType.Reply, 0 );
						AddLabel( 265, 340, 800, "Stand" );
						AddButton( 316, 343, 2117, 2118, 4, GumpButtonType.Reply, 0 );
						AddLabel( 336, 340, 800, "Double Down" );
					}
				}
				if (!m_From.dealercardhidden){dealervalue = dealercardvalue();}
				if (m_From.CardValue(m_From.dealercards[0]) + m_From.CardValue(m_From.dealercards[1]) == 21 && !m_From.dealercardhidden){scoredmsg = "BJ";}
				else {scoredmsg = dealervalue.ToString();}
				if (m_From.CardValue(m_From.playercards[1]) + m_From.CardValue(m_From.playercards[1]) == 21){scorepmsg = "BJ";}
				else{scorepmsg = playercardvalue().ToString();}
				if (m_From.start == true)
				{
					scoredmsg = "";
					scorepmsg = "";
				}
				AddLabel( 30, 152, 1500, "score "+scoredmsg );
				AddLabel( 30, 271, 600, "score "+scorepmsg );
				AddLabel( 191, 363, 1500, m_From.bjmsg );
			}

			public override void OnResponse( NetState sender, RelayInfo info )
			{
				Mobile from = sender.Mobile;
				int i = 0, temp = 0;
				switch ( info.ButtonID )
				{
					case 1:
					{
						m_From.bjmsg = "Good Luck!";
						m_From.round = 1;
						if (m_From.start == true){m_From.start = false;}
						if (!from.InRange( m_From.Location, 3 ))
						{
							m_From.roundend = true;
							m_From.busy = false;
						}
						else 
						{
							if (m_From.roundend)
							{
								if (m_From.paydealer( from, m_From.playerbet))
								{
									m_From.dwin = 0;
									m_From.pwin = 0;
									m_From.roundend = false;
									m_From.dealercardhidden = true;
									for ( i = 2; i <= 4; ++i  )
									{
										m_From.dealercards[i] = 0;
										m_From.playercards[i] = 0;
									}
									m_From.dealercards[0]=m_From.pickcard(from);
									m_From.playercards[0]=m_From.pickcard(from);
									m_From.dealercards[1]=m_From.pickcard(from);
									m_From.playercards[1]=m_From.pickcard(from);
									if (m_From.CardValue(m_From.dealercards[0]) + m_From.CardValue(m_From.dealercards[1]) == 21){m_From.dbj = true;}
									else if (m_From.CardValue(m_From.playercards[1]) + m_From.CardValue(m_From.playercards[1]) == 21){m_From.pbj = true;}
									if (m_From.pbj){finishgame(from);}
								}
								else 
								{
									m_From.bjmsg = "You need more money!";
									m_From.start = true;
									m_From.round = 0;
								}
							}
							from.SendGump( new BlackjackGump( from, m_From ) );
						}
						break;
					}
					case 2:
					{
						if (!m_From.roundend)
						{
							temp = 0;
							for ( i = 2; i <= 4; ++i  )
							{
								if (m_From.playercards[i]==0 && temp==0)
								{
									m_From.playercards[i]=m_From.pickcard(from);
									temp = i;
									i=6;
								}
							}
							if ((temp>0 && playercardvalue()<=21) && i!=5){from.SendGump( new BlackjackGump( from, m_From ) );}
							else {finishgame( from );}
						}
						else {from.SendGump( new BlackjackGump( from, m_From ) );}
						break;
					}
					case 3:
					{
						if (!m_From.roundend){finishgame(from );}
						else {from.SendGump( new BlackjackGump( from, m_From ) );}
						break;
					}
					case 4:
					{
						if (!m_From.roundend)
						{
							temp = 0;
							for ( i = 0; i <= 4; ++i )
							{
								if (m_From.playercards[i]>0){temp++;}
							}
							if (temp==2 && m_From.paydealer( from, m_From.playerbet)){m_From.playerbet *= 2;}
							m_From.playercards[2]=m_From.pickcard(from);
							finishgame(from);
						}
						else{from.SendGump( new BlackjackGump( from, m_From ) );}
						break;
					}
					case 5:
					{
						if (m_From.start == false){m_From.start = true;}
						if (m_From.roundend)
						{
							m_From.playerbet += 100;
							if (m_From.playerbet > 1000){m_From.playerbet = 100;}
						}
						from.SendGump( new BlackjackGump( from, m_From ) );
						break;
					}
					case 6:
					{
						if (m_From.start == false){m_From.start = true;}
						if (m_From.roundend)
						{
							m_From.playerbet -= 100;
							if (m_From.playerbet < 100){m_From.playerbet = 1000;}
						}
						from.SendGump( new BlackjackGump( from, m_From ) );
						break;
					}
					case 7:
					{
						m_From.roundend = true;
						m_From.busy = false;
						m_From.start = true;
						m_From.round = 0;
						Effects.PlaySound( from.Location, from.Map, 0x1e9 );
						break;
					}
				}
			}

			public void finishgame(Mobile from)
			{
				int i, temp, dealervalue = dealercardvalue(), playervalue = playercardvalue();
				temp = (m_From.playerbet/2);
				if (m_From.dbj && m_From.pbj)
				{
					m_From.dwin = temp;
					m_From.pwin = m_From.playerbet+temp;
					m_From.payplayer(from,m_From.pwin);
					m_From.gamestats[2] += 1;
					m_From.bjmsg = "We have a push.";
				}
				else if (m_From.dbj)
				{
					m_From.gamestats[0] += 1;
					m_From.bjmsg = "Looks like I won.";
					m_From.dwin = m_From.playerbet;
					m_From.pwin = 0;
				}
				else if (m_From.pbj)
				{
					m_From.dwin = 0;
					m_From.pwin = (m_From.playerbet*2)+temp;
					m_From.payplayer(from,m_From.pwin);
					m_From.gamestats[1] += 1;
					m_From.bjmsg = "You won this one.";
				}
				else
				{
					if (playervalue > 21 || (dealervalue > playervalue && dealervalue <= 21))
					{
						m_From.gamestats[0] += 1;
						m_From.bjmsg = "Looks like I won.";
						m_From.dwin = m_From.playerbet;
						m_From.pwin = 0;
					}
					else
					{
						if (dealervalue < 17)
						{
							for ( i = 2; i <= 4; ++i  )
							{
								if (m_From.dealercards[i]==0)
								{
									m_From.dealercards[i]=m_From.pickcard(from);
									dealervalue=dealercardvalue();
								}
								if (dealervalue >= 17){i=6;}
							}
						}
						if (playervalue > 21 || (dealervalue > playervalue && dealervalue <= 21))
						{
							m_From.gamestats[0] += 1;
							m_From.bjmsg = "I won this round.";
							m_From.dwin = m_From.playerbet;
							m_From.pwin = 0;
						}
						else if (dealervalue == playervalue)
						{
							m_From.dwin = temp;
							m_From.pwin = m_From.playerbet+temp;
							m_From.payplayer(from,m_From.pwin);
							m_From.gamestats[2] += 1;
							m_From.bjmsg = "We have a push.";
						}
						else
						{
							if (playervalue == 21)
							{
								m_From.dwin = 0;
								m_From.pwin = (m_From.playerbet*2);
								m_From.payplayer(from,m_From.pwin);
								m_From.gamestats[1] += 1;
								m_From.bjmsg = "You have won another round.";
							}
							else
							{
								m_From.dwin = 0;
								m_From.pwin = (m_From.playerbet*2);
								m_From.payplayer(from,m_From.pwin);
								m_From.gamestats[1] += 1;
								m_From.bjmsg = "You won this one.";
							}
						}
					}
				}
				m_From.dbj = false;
				m_From.pbj = false;
				m_From.pwin = (m_From.pwin-m_From.playerbet);
				m_From.dealercardhidden = false;
				m_From.roundend = true;
				m_From.round = 0;
				m_From.playerbet = 500;
				Effects.PlaySound( from.Location, from.Map, 0x36 );
				from.SendGump( new BlackjackGump( from, m_From ) );
			}

			public int dealercardvalue()
			{
				int i, tempcard = 0, gotace = 0, dealervalue = 0;

				for ( i = 0; i <= 4; ++i )
				{
					tempcard = m_From.CardValue( m_From.dealercards[i] );
					if (tempcard == 11){gotace++;}
					dealervalue += tempcard;
				}
				while (dealervalue > 21 && gotace > 0)
				{
					dealervalue -= 10;
					gotace--;
				}
				return dealervalue;
			}

			public int playercardvalue()
			{
				int i, tempcard = 0, gotace = 0, playervalue = 0;
				for ( i = 0; i <= 4; ++i )
				{
					tempcard = m_From.CardValue( m_From.playercards[i] );
					if (tempcard == 11){gotace++;}
					playervalue += tempcard;
				}
				while (playervalue > 21 && gotace > 0)
				{
					playervalue -= 10;
					gotace--;
				}
				return playervalue;
			}
		}

        public override void Serialize( IGenericWriter writer )
        {
            base.Serialize( writer );
            writer.Write( (int) 0 ); // version
            writer.Write( (bool) true );
            writer.Write( (bool) false );
			for ( int i = 0; i <= 2; ++i )
			{
				writer.Write( gamestats[i] );
			}
        }

        public override void Deserialize( IGenericReader reader )
        {
            base.Deserialize( reader );
            int version = reader.ReadInt();
            roundend = reader.ReadBool();
            busy = reader.ReadBool();
			for ( int i = 0; i <= 2; ++i )
			{
				gamestats[i]=reader.ReadInt();
			}
        }

		public override bool OnGoldGiven( Mobile from, Gold dropped )
		{
			string message = "Are you trying to bribe me to win?";
			this.Say( message );
			return false;
		}
    }
}
