using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Network;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Mobiles
{
    public class PGambler : BaseCreature
    {
		public static bool NewCards = false;
		private int m_current_card = 53;
		private int [] Cardz = new int[53];
		private int [] dealercards = new int[5];
		private int [] playercards = new int[5];
		private int [] gamestats = new int[3];
		private int playerbet = 500;
		private int playerraise = 0;
		private int playerraise2 = 0;
		private int gamepot = 0;
		private int ftnc = 0;
		private int countnc = 0;
		private int countsr = 0;
		private int betround = 0;
		private int pwin = 0;
		private int goldbp = 0;
		private bool showmsg = false;
		private bool startgump = true;
		private bool hiddencards = true;
		private bool roundend;
		private bool busy;
		private string playermsg = "";
		private string dealermsg = "";
		private string gamemsg = "";
		private string ncmsg = "";
		private Mobile m_player;

		[Constructible]
        public PGambler() : base( AIType.AI_Melee, FightMode.None, 10, 1, 0.2, 0.4 ) 
        {
            SetStr( 10, 30 );
            SetDex( 10, 30 );
            SetInt( 10, 30 );
			Name = "Harold";
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
			for ( int i = 0; i <= 2; ++i ){gamestats[i] = 0;}
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
				else if  (e.Speech.ToLower() == "poker" || e.Speech.ToLower() == "Poker")
				{
					if (!busy)
					{
						playerbet = 500;
						playerraise = 0;
						playerraise2 = 0;
						busy = true;
						roundend = true;
						m_current_card = 53;
						pwin = 0;
						betround = 0;
						for ( int i = 0; i <= 4; ++i )
						{
							playercards[i] = 35+i;
							dealercards[i] = 2+i;
						}
						gamemsg = "Good luck!";
						dealermsg = "";
						playermsg = "";
						m_player = from;
						playpoker( from );
						message = "So, you want to try your luck.";
						this.Say( message );
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

		public int cardcolor( string cardtemp )
		{
			if ( cardtemp == "\u25C6" || cardtemp == "\u2665" ){return 32;}
			return 0;
		}

		public string CardName( int card )
		{
			while (card > 13){card -= 13;}
			if (card == 1){return "A";}
			else if (card == 11){return "J";}
			else if (card == 12){return "Q";}
			else if (card == 13){return "K";}
			else {return "" + card;}
		}

		public int CardValue( int card )
		{
			while (card > 13){card -= 13;}
			if (card == 1){return 11;}
			if (card > 10){return 10;}
			return card;
		}

		public int CardValue2( int card )
		{
			while (card > 13){card -= 13;}
			if (card == 1){return 14;}
			return card;
		}

		public void ShuffleCards( )
		{
			int i, tempcard, tempcard2;
			for ( i = 1; i < 53; ++i ){Cardz[i] = i;}
			for ( i = 52; i >= 1; --i )
			{
				tempcard = Utility.Random( i )+1;
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

		public void playpoker( Mobile from )
		{
			from.SendGump( new PokerGump( this, this ) );
		}

		public override bool DisallowAllMoves
		{
			get { return true; }
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.AccessLevel >= AccessLevel.Seer ){from.SendGump( new PGamblerStatsGump( this ) );}
			else {base.OnDoubleClick( from );}
		}

		public PGambler( Serial serial ) : base( serial )
		{
		}

		public class PGamblerStatsGump : Gump
		{
			private PGambler m_From;
			public PGamblerStatsGump( PGambler gambler ) : base( 10, 10 )
			{
				m_From = gambler;
				AddPage( 0 );
				AddBackground( 30, 100, 90, 100, 5120 );
				AddLabel( 45, 100, 70, "Poker" );
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
		
		public class PokerGump : Gump
		{
			private PGambler m_From;

			public PokerGump( Mobile mobile, PGambler gambler ) : base( 10, 10 )
			{
				m_From = gambler;
				int i, temp = 0, tempd = 0;
				string cardtemp = "Player:";
				string cardtempd = "Dealer:";
				Closable = false;
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
				AddLabel( 30, 111, 1500, "Harold:" );
				AddLabel( 30, 231, 800, "You:" );
				AddImageTiled( 512, 399, 80, 93, 9274 );
				AddImage( 524, 423, 9811 );
				AddLabel( 532, 406, 600, ""+m_From.goldbp );
				AddItem ( 521, 456, 3823 );
				if (m_From.betround == 2 || m_From.betround == 1)
				{
					cardtemp="Player: 1";
					cardtempd = "Dealer: 1";
				}
				for ( i = 0; i <= 4; ++i )
				{
					if (m_From.betround == 1){m_From.dealercards[i] = m_From.pickcard(mobile);}
					tempd = m_From.dealercards[i];
					if (tempd > 0)
					{
						if (!NewCards)
						{
							cardtempd = m_From.CardSuit( tempd );
							AddBackground( 70 + ((i+1)*45), 117, 35, 50, 2171 );
							if (m_From.startgump == true){AddImage( 73 + ((i+1)*45), 110, 10897 );}
						}
						if (m_From.betround == 1 || m_From.betround == 2)
						{
							m_From.ftnc++;
							if (m_From.ftnc == 1)
							{
								if ( 0.005 > Utility.RandomDouble() ){m_From.ncmsg = "Harold wants no new cards!";}
								else if ( 0.01 > Utility.RandomDouble() ){m_From.ncmsg = "Harold wants one new card!";}
								else if ( 0.05 > Utility.RandomDouble() ){m_From.ncmsg = "Harold wants five new cards!";}
								else if ( 0.06 > Utility.RandomDouble() ){m_From.ncmsg = "Harold wants two new cards!";}
								else if ( 0.4 > Utility.RandomDouble() ){m_From.ncmsg = "Harold wants four new cards!";}
								else {m_From.ncmsg = "Harold wants three new cards!";}
							}
							AddLabel( 115, 171, 1500, m_From.ncmsg );
							if (m_From.hiddencards == true){AddImage( 73 + ((i+1)*45), 110, 10897 );}
						}
						else if (m_From.betround == 3)
						{
							if (m_From.hiddencards == true){AddImage( 73 + ((i+1)*45), 110, 10897 );}
						}
						else
						{
							if (m_From.showmsg == true)
							{
								AddLabel( 85 + ((i+1)*45), 143, m_From.cardcolor( cardtempd ), cardtempd );
								AddLabel( 77 + ((i+1)*45), 122, 1500, m_From.CardName( tempd ) );
							}
						}
					}
				}
				for ( i = 0; i <= 4; ++i )
				{
					if (m_From.betround == 1){m_From.playercards[i] = m_From.pickcard(mobile);}
					temp = m_From.playercards[i];
					if (temp > 0)
					{
						if (!NewCards)
						{
							cardtemp = m_From.CardSuit( temp );
							AddBackground( 70 + ((i+1)*45), 237, 35, 50, 2171 );
							if (m_From.startgump == true){AddImage( 73 + ((i+1)*45), 230, 10897 );}

							if (m_From.showmsg == true)
							{
								AddLabel( 85 + ((i+1)*45), 263, m_From.cardcolor( cardtemp ), cardtemp );
								AddLabel( 77 + ((i+1)*45), 242, 600, m_From.CardName( temp ) );
								if (i == 4){m_From.showmsg = false;}
							}
						}
						if (m_From.betround == 1 || m_From.betround == 2)
						{
							AddCheck( 78 + ((i+1)*45), 291, 210, 211, false, (i+1) );
							AddLabel( 70 + ((i+1)*45), 309, 800, "redeal" );
							AddButton( 35, 347, 2151, 2154, 1, GumpButtonType.Reply, 0 );
							AddLabel( 69, 351, 800, "Re-Deal" );
							AddButton( 420, 188, 4500, 4500, 7, GumpButtonType.Reply, 0 );
							AddButton( 420, 270, 4504, 4504, 8, GumpButtonType.Reply, 0 );
							AddImage( 417, 233, 51 );
							AddLabel( 429, 173, 800, "raise:" );
							AddLabel( 433, 246, 800, ""+m_From.playerraise );
							AddButton( 431, 115, 2472, 2474, 12, GumpButtonType.Reply, 0 );
							AddLabel( 431, 137, 800, "Drop" );
							if (+m_From.playerraise == 0){AddLabel( 191, 359, 800, "Check" );}
							else {AddLabel( 191, 359, 800, "You raise $"+m_From.playerraise );}
						}
						else if (m_From.betround == 3)
						{
							AddButton( 35, 347, 2151, 2154, 1, GumpButtonType.Reply, 0 );
							AddLabel( 69, 351, 800, "Call" );
							AddButton( 420, 188, 4500, 4500, 10, GumpButtonType.Reply, 0 );
							AddButton( 420, 270, 4504, 4504, 11, GumpButtonType.Reply, 0 );
							AddImage( 417, 233, 51 );
							AddLabel( 412, 173, 800, "raise again:" );
							AddLabel( 433, 246, 800, ""+m_From.playerraise2 );
							AddButton( 431, 115, 2472, 2474, 13, GumpButtonType.Reply, 0 );
							AddLabel( 431, 137, 800, "Drop" );
							if (+m_From.playerraise2 == 0){AddLabel( 191, 359, 800, "Check & call" );}
							else {AddLabel( 191, 359, 800, "You raise $"+m_From.playerraise2+" & call" );}
						}
						else
						{
							AddButton( 35, 347, 2151, 2154, 1, GumpButtonType.Reply, 0 );
							AddLabel( 69, 351, 800, "Deal" );
							AddButton( 420, 188, 4500, 4500, 5, GumpButtonType.Reply, 0 );
							AddButton( 420, 270, 4504, 4504, 6, GumpButtonType.Reply, 0 );
							AddImage( 417, 233, 51 );
							AddLabel( 418, 173, 800, "your bet:" );
							AddLabel( 433, 246, 800, ""+m_From.playerbet );
							if (m_From.showmsg == true)
							{
								AddLabel( 115, 171, 1500, m_From.dealermsg );
								AddLabel( 115, 291, 800, m_From.playermsg );
							}
							AddButton( 411, 114, 1027, 1028, 9, GumpButtonType.Reply, 0 );
							if (m_From.pwin == 1)
							{
								AddLabel( 30, 132, 38, "- $"+m_From.gamepot);
								AddLabel( 30, 252, 70, "+ $"+m_From.gamepot);
								m_From.pwin = 0;
							}
							if (m_From.pwin == 2)
							{
								AddLabel( 30, 132, 70, "+ $"+m_From.gamepot);
								AddLabel( 30, 252, 38, "- $"+m_From.gamepot);
								m_From.pwin = 0;
							}
							if (m_From.pwin == 3)
							{
								AddLabel( 30, 132, 55, "+/- $0" );
								AddLabel( 30, 252, 55, "+/- $0" );
								m_From.pwin = 0;
							}
						}
					}
				}
				AddLabel( 191, 343, 800, m_From.gamemsg );
				if (m_From.betround == 1){m_From.betround = 2;}
				if (m_From.betround == 4)
				{
					m_From.betround = 0;
					m_From.roundend = true;
				}
			}

			public override void OnResponse( NetState sender, RelayInfo info )
			{
				Mobile from = sender.Mobile;
				int i;
				switch ( info.ButtonID )
				{
					case 1:
					{ 
						m_From.gamemsg = "Good Luck!";
						m_From.showmsg = true;
						if (!from.InRange( m_From.Location, 4 ))
						{
							m_From.roundend = true;
							m_From.busy = false;
						}
						else
						{
							if (m_From.betround == 0)
							{
								if (m_From.paydealer( from, m_From.playerbet))
								{
									if ((m_From.m_current_card + 10) > 52)
									{
										Effects.PlaySound( from.Location, from.Map, 0x3D );
										m_From.ShuffleCards();
									}
									for ( i = 0; i <= 4; ++i )
									{
										m_From.playercards[i] = 0;
										m_From.dealercards[i] = 0;
									}
									m_From.betround = 1;
									m_From.roundend = false;
									m_From.startgump = false;
									m_From.hiddencards = true;
									m_From.gamemsg = "Mark the cards you want re-dealt.";
								}
								else 
								{
									m_From.gamemsg = "You need more money!";
									m_From.showmsg = false;
								}
							}
							else if (m_From.betround == 2)
							{
								if (m_From.playerraise == 0)
								{
									m_From.betround = 3;
									ArrayList Selections = new ArrayList( info.Switches );
									for ( i = 0; i <= 4; ++i  )
									{
										if (Selections.Contains( i+1 ) != false ){m_From.playercards[i] = m_From.pickcard(from);}
									}
									dnewcards(from);
								}
								else if (m_From.paydealer( from, m_From.playerraise))
								{
									m_From.betround = 3;
									ArrayList Selections = new ArrayList( info.Switches );
									for ( i = 0; i <= 4; ++i  )
									{
										if (Selections.Contains( i+1 ) != false ){m_From.playercards[i] = m_From.pickcard(from);}
									}
									dnewcards(from);
									if (m_From.countnc >= 4)
									{
										if (m_From.playerraise >= 600)
										{
											if ( 0.6 > Utility.RandomDouble() )
											{
												m_From.gamemsg = "Harold dropped, you win! ";
												m_From.startgump = true;
												m_From.showmsg = false;
												m_From.hiddencards = false;
												m_From.roundend = true;
												m_From.pwin = 1;
												m_From.payplayer(from, m_From.playerraise);
												m_From.gamepot = m_From.playerbet;
												m_From.playerbet = 500;
												m_From.playerraise = 0;
												m_From.playerraise2 = 0;
												m_From.countnc = 0;
												m_From.betround = 4;
												from.SendGump( new PokerGump( from, m_From ) );
												break;
											}
										}
										else if (m_From.playerraise >= 300)
										{
											if ( 0.3 > Utility.RandomDouble() )
											{
												m_From.gamemsg = "Harold dropped, you win! ";
												m_From.startgump = true;
												m_From.showmsg = false;
												m_From.hiddencards = false;
												m_From.roundend = true;
												m_From.pwin = 1;
												m_From.payplayer(from, m_From.playerraise);
												m_From.gamepot = m_From.playerbet;
												m_From.playerbet = 500;
												m_From.playerraise = 0;
												m_From.playerraise2 = 0;
												m_From.countnc = 0;
												m_From.betround = 4;
												from.SendGump( new PokerGump( from, m_From ) );
												break;
											}
										}
									}
									else if (m_From.countnc == 3)
									{
										if (m_From.playerraise >= 900)
										{
											if ( 0.3 > Utility.RandomDouble() )
											{
												m_From.gamemsg = "Harold dropped, you win! ";
												m_From.startgump = true;
												m_From.showmsg = false;
												m_From.hiddencards = false;
												m_From.roundend = true;
												m_From.pwin = 1;
												m_From.payplayer(from, m_From.playerraise);
												m_From.gamepot = m_From.playerbet;
												m_From.playerbet = 500;
												m_From.playerraise = 0;
												m_From.playerraise2 = 0;
												m_From.countnc = 0;
												m_From.betround = 4;
												from.SendGump( new PokerGump( from, m_From ) );
												break;
											}
										}
										else if (m_From.playerraise >= 600)
										{
											if ( 0.2 > Utility.RandomDouble() )
											{
												m_From.gamemsg = "Harold dropped, you win! ";
												m_From.startgump = true;
												m_From.showmsg = false;
												m_From.hiddencards = false;
												m_From.roundend = true;
												m_From.pwin = 1;
												m_From.payplayer(from, m_From.playerraise);
												m_From.gamepot = m_From.playerbet;
												m_From.playerbet = 500;
												m_From.playerraise = 0;
												m_From.playerraise2 = 0;
												m_From.countnc = 0;
												m_From.betround = 4;
												from.SendGump( new PokerGump( from, m_From ) );
												break;
											}
										}
									}
								}
								else {m_From.gamemsg = "You need more money!";}
							}
							else if (m_From.betround == 3)
							{
								if (m_From.playerraise2 == 0)
								{
									m_From.betround = 4;
									m_From.hiddencards = false;
									checkplayercards(from);
								}
								else if (m_From.paydealer( from, m_From.playerraise2))
								{
									checksrnd(from);
									if (m_From.countsr >= 4)
									{
										if (m_From.playerraise2 >= 600)
										{
											if ( 0.6 > Utility.RandomDouble() )
											{
												m_From.gamemsg = "Harold dropped, you win! ";
												m_From.startgump = true;
												m_From.showmsg = false;
												m_From.hiddencards = false;
												m_From.roundend = true;
												m_From.pwin = 1;
												m_From.payplayer(from, m_From.playerraise2);
												m_From.gamepot = (m_From.playerbet + m_From.playerraise);
												m_From.playerbet = 500;
												m_From.playerraise = 0;
												m_From.playerraise2 = 0;
												m_From.countsr = 0;
												m_From.betround = 4;
												from.SendGump( new PokerGump( from, m_From ) );
												break;
											}
										}
										else if (m_From.playerraise2 >= 300)
										{
											if ( 0.3 > Utility.RandomDouble() )
											{
												m_From.gamemsg = "Harold dropped, you win! ";
												m_From.startgump = true;
												m_From.showmsg = false;
												m_From.hiddencards = false;
												m_From.roundend = true;
												m_From.pwin = 1;
												m_From.payplayer(from, m_From.playerraise2);
												m_From.gamepot = (m_From.playerbet + m_From.playerraise);
												m_From.playerbet = 500;
												m_From.playerraise = 0;
												m_From.playerraise2 = 0;
												m_From.countsr = 0;
												m_From.betround = 4;
												from.SendGump( new PokerGump( from, m_From ) );
												break;
											}
										}
									}
									else if (m_From.countsr == 3)
									{
										if (m_From.playerraise2 >= 900)
										{
											if ( 0.3 > Utility.RandomDouble() )
											{
												m_From.gamemsg = "Harold dropped, you win! ";
												m_From.startgump = true;
												m_From.showmsg = false;
												m_From.hiddencards = false;
												m_From.roundend = true;
												m_From.pwin = 1;
												m_From.payplayer(from, m_From.playerraise2);
												m_From.gamepot = (m_From.playerbet + m_From.playerraise);
												m_From.playerbet = 500;
												m_From.playerraise = 0;
												m_From.playerraise2 = 0;
												m_From.countsr = 0;
												m_From.betround = 4;
												from.SendGump( new PokerGump( from, m_From ) );
												break;
											}
										}
										else if (m_From.playerraise2 >= 600)
										{
											if ( 0.2 > Utility.RandomDouble() )
											{
												m_From.gamemsg = "Harold dropped, you win! ";
												m_From.startgump = true;
												m_From.showmsg = false;
												m_From.hiddencards = false;
												m_From.roundend = true;
												m_From.pwin = 1;
												m_From.payplayer(from, m_From.playerraise2);
												m_From.gamepot = (m_From.playerbet + m_From.playerraise);
												m_From.playerbet = 500;
												m_From.playerraise = 0;
												m_From.playerraise2 = 0;
												m_From.countsr = 0;
												m_From.betround = 4;
												from.SendGump( new PokerGump( from, m_From ) );
												break;
											}
										}
									}
									m_From.betround = 4;
									m_From.hiddencards = false;
									checkplayercards(from);
								}
								else {m_From.gamemsg = "You need more money!";}
							}
						}
						from.SendGump( new PokerGump( from, m_From ) );
						break;
					}
					case 5:
					{
						m_From.startgump = true;
						m_From.gamemsg = "Good Luck!";
						if (m_From.roundend)
						{
							m_From.playerbet += 100;
							if (m_From.playerbet > 1000){m_From.playerbet = 100;}
						}
						from.SendGump( new PokerGump( from, m_From ) );
						break;
					}
					case 6:
					{
						m_From.startgump = true;
						m_From.gamemsg = "Good Luck!";
						if (m_From.roundend)
						{
							m_From.playerbet -= 100;
							if (m_From.playerbet < 100){m_From.playerbet = 1000;}
						}
						from.SendGump( new PokerGump( from, m_From ) );
						break;
					}
					case 7:
					{
						m_From.showmsg = true;
						m_From.playerraise += 100;
						if (m_From.playerraise > 1000){m_From.playerraise = 0;}
						from.SendGump( new PokerGump( from, m_From ) );
						break;
					}
					case 8:
					{
						m_From.showmsg = true;
						m_From.playerraise -= 100;
						if (m_From.playerraise < 0)
						{m_From.playerraise = 1000;}
						from.SendGump( new PokerGump( from, m_From ) );
						break;
					}
					case 9:
					{
						m_From.roundend = true;
						m_From.busy = false;
						m_From.startgump = true;
						m_From.hiddencards = true;
						m_From.showmsg = false;
						m_From.ftnc = 0;
						Effects.PlaySound( from.Location, from.Map, 0x1e9 );
						break;
					}
					case 10:
					{
						m_From.showmsg = true;
						m_From.playerraise2 += 100;
						if (m_From.playerraise2 > 1000){m_From.playerraise2 = 0;}
						from.SendGump( new PokerGump( from, m_From ) );
						break;
					}
					case 11:
					{
						m_From.showmsg = true;
						m_From.playerraise2 -= 100;
						if (m_From.playerraise2 < 0){m_From.playerraise2 = 1000;}
						from.SendGump( new PokerGump( from, m_From ) );
						break;
					}
					case 12:
					{
						m_From.gamemsg = "You loose!";
						m_From.startgump = true;
						m_From.showmsg = false;
						m_From.hiddencards = false;
						m_From.roundend = true;
						m_From.pwin = 2;
						m_From.gamepot = m_From.playerbet;
						m_From.playerbet = 500;
						m_From.playerraise = 0;
						m_From.playerraise2 = 0;
						m_From.betround = 4;
						from.SendGump( new PokerGump( from, m_From ) );
						break;
					}
					case 13:
					{
						m_From.gamemsg = "You loose!";
						m_From.startgump = true;
						m_From.showmsg = false;
						m_From.hiddencards = false;
						m_From.roundend = true;
						m_From.pwin = 2;
						m_From.gamepot = (m_From.playerbet + m_From.playerraise);
						m_From.playerbet = 500;
						m_From.playerraise = 0;
						m_From.playerraise2 = 0;
						m_From.betround = 4;
						from.SendGump( new PokerGump( from, m_From ) );
						break;
					}
				}
			}

			public void dnewcards(Mobile from)
			{
				int i, matchd1 = 0, matchd2 = 0, matchd3 = 0, matchd4 = 0, matchd5 = 0, tempd = 0;
				for(int j = 4; j >= 0; j--)
				{
					for ( i = 0; i < 4; i++ )
					{
						if (m_From.CardValue2(m_From.dealercards[i]) >= m_From.CardValue2(m_From.dealercards[i+1]))
						{
							tempd = m_From.dealercards[i];
							m_From.dealercards[i] = m_From.dealercards[i+1];
							m_From.dealercards[i+1] = tempd;
						}
					}
				}
				for ( i = 0; i <= 4; i++  )
				{
					tempd = m_From.CardValue2(m_From.dealercards[i]);
					if ((m_From.CardValue2(m_From.dealercards[0]) == tempd) && i != 0){matchd1++;}
					if ((m_From.CardValue2(m_From.dealercards[1]) == tempd) && i != 1){matchd2++;}
					if ((m_From.CardValue2(m_From.dealercards[2]) == tempd) && i != 2){matchd3++;}
					if ((m_From.CardValue2(m_From.dealercards[3]) == tempd) && i != 3){matchd4++;}
					if ((m_From.CardValue2(m_From.dealercards[4]) == tempd) && i != 4){matchd5++;}
				}
				m_From.countnc = 0;
				if (matchd1 >= 3){}
				else
				{
					if (matchd1 == 2){}
					else
					{
						if (matchd1 == 1){}
						else
						{
							m_From.dealercards[0] = m_From.pickcard(from);
							m_From.countnc += 1;
						}
					}
				}
				if (matchd2 >= 3){}
				else
				{
					if (matchd2 == 2){}
					else
					{
						if (matchd2 == 1){}
						else
						{
							m_From.dealercards[1] = m_From.pickcard(from);
							m_From.countnc += 1;
						}
					}
				}
				if (matchd3 >= 3){}
				else
				{
					if (matchd3 == 2){}
					else
					{
						if (matchd3 == 1){}
						else
						{
							m_From.dealercards[2] = m_From.pickcard(from);
							m_From.countnc += 1;
						}
					}
				}
				if (matchd4 >= 3){}
				else
				{
					if (matchd4 == 2){}
					else
					{
						if (matchd4 == 1){}
						else
						{
							m_From.dealercards[3] = m_From.pickcard(from);
							m_From.countnc += 1;
						}
					}
				}
				if (matchd5 >= 3){}
				else
				{
					if (matchd5 == 2){}
					else
					{
						if (matchd5 == 1){}
						else
						{
							m_From.dealercards[4] = m_From.pickcard(from);
							m_From.countnc += 1;
						}
					}
				}
			}

			public void checksrnd(Mobile from)
			{
				int i, matchd1 = 0, matchd2 = 0, matchd3 = 0, matchd4 = 0, matchd5 = 0, tempd = 0;
				for(int j = 4; j >= 0; j--)
				{
					for ( i = 0; i < 4; i++ )
					{
						if (m_From.CardValue2(m_From.dealercards[i]) >= m_From.CardValue2(m_From.dealercards[i+1]))
						{
							tempd = m_From.dealercards[i];
							m_From.dealercards[i] = m_From.dealercards[i+1];
							m_From.dealercards[i+1] = tempd;
						}
					}
				}
				for ( i = 0; i <= 4; i++  )
				{
					tempd = m_From.CardValue2(m_From.dealercards[i]);
					if ((m_From.CardValue2(m_From.dealercards[0]) == tempd) && i != 0){matchd1++;}
					if ((m_From.CardValue2(m_From.dealercards[1]) == tempd) && i != 1){matchd2++;}
					if ((m_From.CardValue2(m_From.dealercards[2]) == tempd) && i != 2){matchd3++;}
					if ((m_From.CardValue2(m_From.dealercards[3]) == tempd) && i != 3){matchd4++;}
					if ((m_From.CardValue2(m_From.dealercards[4]) == tempd) && i != 4){matchd5++;}
				}
				m_From.countsr = 0;
				if (matchd1 >= 3){}
				else
				{
					if (matchd1 == 2){}
					else
					{
						if (matchd1 == 1){}
						else {m_From.countsr += 1;}
					}
				}
				if (matchd2 >= 3){}
				else
				{
					if (matchd2 == 2){}
					else
					{
						if (matchd2 == 1){}
						else {m_From.countsr += 1;}
					}
				}
				if (matchd3 >= 3){}
				else
				{
					if (matchd3 == 2){}
					else
					{
						if (matchd3 == 1){}
						else {m_From.countsr += 1;}
					}
				}
				if (matchd4 >= 3){}
				else
				{
					if (matchd4 == 2){}
					else
					{
						if (matchd4 == 1){}
						else{m_From.countsr += 1;}
					}
				}
				if (matchd5 >= 3){}
				else
				{
					if (matchd5 == 2){}
					else
					{
						if (matchd5 == 1){}
						else {m_From.countsr += 1;}
					}
				}
			}

			public void checkplayercards(Mobile from)
			{
				int i, match1 = 0, match2 = 0, match3 = 0, match4 = 0, match5 = 0, temp = 0, hcp = 0, shcp = 0;
				int phpp1 = 0, phpp2 = 0, phpp3 = 0, phpp4 = 0, phpp5 = 0, hpp = 0, shpp = 0, pwinch = 0;
				int phcp1 = 0, phcp2 = 0, phcp3 = 0, phcp4 = 0, phcp5 = 0;
				int stc1 = 0, stc2 = 0, stc3 = 0, stc4 = 0, stc5 = 0;
				bool isStrt = false, isFlush = false;
				string Temp, hcps = "", shcps = "", hpps = "", shpps = "", ccps = "", ccpst = "";
				int matchd1 = 0, matchd2 = 0, matchd3 = 0, matchd4 = 0, matchd5 = 0, tempd = 0, hcd = 0, shcd = 0;
				int phpd1 = 0, phpd2 = 0, phpd3 = 0, phpd4 = 0, phpd5 = 0, hpd = 0, shpd = 0, dwinch = 0;
				int phcd1 = 0, phcd2 = 0, phcd3 = 0, phcd4 = 0, phcd5 = 0;
				int stcd1 = 0, stcd2 = 0, stcd3 = 0, stcd4 = 0, stcd5 = 0;
				bool isStrtd = false, isFlushd = false;
				string Tempd, hcds = "", shcds = "", hpds = "", shpds = "", ccds = "", ccdst = "";
				for(int j = 4; j >= 0; j--)
				{
					for ( i = 0; i < 4; i++ )
					{
						if (m_From.CardValue2(m_From.playercards[i]) >= m_From.CardValue2(m_From.playercards[i+1]))
						{
							temp = m_From.playercards[i];
							m_From.playercards[i] = m_From.playercards[i+1];
							m_From.playercards[i+1] = temp;
						}
						if (m_From.CardValue2(m_From.dealercards[i]) >= m_From.CardValue2(m_From.dealercards[i+1]))
						{
							tempd = m_From.dealercards[i];
							m_From.dealercards[i] = m_From.dealercards[i+1];
							m_From.dealercards[i+1] = tempd;
						}
					}
				}
                hcp = m_From.CardValue2(m_From.playercards[4]);
				shcp = m_From.CardValue2(m_From.playercards[3]);
				if (hcp <= 10){hcps = ""+hcp;}
				else
				{
					if(hcp == 11){hcps = "Jack";}
					if(hcp == 12){hcps = "Queen";}
					if(hcp == 13){hcps = "King";}
					if(hcp == 14){hcps = "Ace";}
				}
				if (shcp <= 10){shcps = ""+shcp;}
				else
				{
					if(shcp == 11){shcps = "Jack";}
					if(shcp == 12){shcps = "Queen";}
					if(shcp == 13){shcps = "King";}
					if(shcp == 14){shcps = "Ace";}
				}
				m_From.playermsg = "High Card: "+hcps+", Kicker: "+shcps;
				pwinch = 1;		
				stc1 = m_From.CardValue2(m_From.playercards[0]);
				stc2 = m_From.CardValue2(m_From.playercards[1]);
				stc3 = m_From.CardValue2(m_From.playercards[2]);
				stc4 = m_From.CardValue2(m_From.playercards[3]);
				stc5 = m_From.CardValue2(m_From.playercards[4]);
				if(((stc1 + 1 == stc2) && (stc1 + 2 == stc3) && (stc1 + 3 == stc4) && (stc1 + 4 == stc5)) || ((stc1 == 2) && (stc1 + 1 == stc2) && (stc1 + 2 == stc3) && (stc1 + 3 == stc4) && (stc1 + 12 == stc5))){isStrt = true;}
				if (isStrt)
				{
					if (hcp <= 10){m_From.playermsg = "Straight, High Card: "+hcp;}
					else
					{
						if(hcp == 11){m_From.playermsg = "Straight, High Card: Jack";}
						if(hcp == 12){m_From.playermsg = "Straight, High Card: Queen";}
						if(hcp == 13){m_From.playermsg = "Straight, High Card: King";}
						if(hcp == 14){m_From.playermsg = "Straight, High Card: Ace";}
					}
					pwinch = 5;
				}
				Temp = m_From.CardSuit(m_From.playercards[0]);
				if(Temp == m_From.CardSuit(m_From.playercards[1]) && Temp == m_From.CardSuit(m_From.playercards[2]) && Temp == m_From.CardSuit(m_From.playercards[3]) && Temp == m_From.CardSuit(m_From.playercards[4]))
				{
					isFlush = true;
					temp = m_From.playercards[1];
					ccpst = m_From.CardSuit( temp );
					if (ccpst == "\u2663"){ccps = "Clubs";}
					if (ccpst == "\u25C6"){ccps = "Diamonds";}
					if (ccpst == "\u2665"){ccps = "Hearts";}
					if (ccpst == "\u2660"){ccps = "Spades";}
					hcp = m_From.CardValue2(m_From.playercards[4]);
					if (hcp <= 10){hcps = ""+hcp;}
					else
					{
						if(hcp == 11){hcps = "Jack";}
						if(hcp == 12){hcps = "Queen";}
						if(hcp == 13){hcps = "King";}
						if(hcp == 14){hcps = "Ace";}
					}
					m_From.playermsg = "Flush: "+ccps+", High Card: "+hcps;
					pwinch = 6;
				}
				if(!isStrt && !isFlush)
				{
					for ( i = 0; i <= 4; i++  )
					{
						temp = m_From.CardValue2(m_From.playercards[i]);
						if ((m_From.CardValue2(m_From.playercards[0]) == temp) && i != 0){match1++;}
						if ((m_From.CardValue2(m_From.playercards[1]) == temp) && i != 1){match2++;}
						if ((m_From.CardValue2(m_From.playercards[2]) == temp) && i != 2){match3++;}
						if ((m_From.CardValue2(m_From.playercards[3]) == temp) && i != 3){match4++;}
						if ((m_From.CardValue2(m_From.playercards[4]) == temp) && i != 4){match5++;}
					}
					if((match1 == 3) || (match2 == 3) || (match3 == 3) || (match4 == 3) || (match5 == 3))
					{
						if (match1 == 3){hpp = m_From.CardValue2(m_From.playercards[0]);}
						if (match2 == 3){hpp = m_From.CardValue2(m_From.playercards[1]);}
						if (match3 == 3){hpp = m_From.CardValue2(m_From.playercards[2]);}
						if (match4 == 3){hpp = m_From.CardValue2(m_From.playercards[3]);}
						if (match5 == 3){hpp = m_From.CardValue2(m_From.playercards[4]);}
						if (hpp <= 10){hpps = ""+hpp+"s";}
						else
						{
							if(hpp == 11){hpps = "Jacks";}
							if(hpp == 12){hpps = "Queens";}
							if(hpp == 13){hpps = "Kings";}
							if(hpp == 14){hpps = "Aces";}
						}
						m_From.playermsg = "Four of a kind: "+hpps;
						pwinch = 8;
					}
					if((match1 == 2) || (match2 == 2) || (match3 == 2) || (match4 == 2) || (match5 == 2))
					{
						if (match1 == 2){hpp = m_From.CardValue2(m_From.playercards[0]);}
						if (match2 == 2){hpp = m_From.CardValue2(m_From.playercards[1]);}
						if (match3 == 2){hpp = m_From.CardValue2(m_From.playercards[2]);}
						if (match4 == 2){hpp = m_From.CardValue2(m_From.playercards[3]);}
						if (match5 == 2){hpp = m_From.CardValue2(m_From.playercards[4]);}
						if (hpp <= 10){hpps = ""+hpp+"s";}
						else
						{
							if(hpp == 11){hpps = "Jacks";}
							if(hpp == 12){hpps = "Queens";}
							if(hpp == 13){hpps = "Kings";}
							if(hpp == 14){hpps = "Aces";}
						}
						m_From.playermsg = "Three of a kind: "+hpps;
						pwinch = 4;
					}
					if((match1 + match2 + match3 + match4 + match5) == 8)
					{
						if (match1 < 2){shpp = m_From.CardValue2(m_From.playercards[0]);}
						else {hpp = m_From.CardValue2(m_From.playercards[0]);}
						if (match2 < 2){shpp = m_From.CardValue2(m_From.playercards[1]);}
						else {hpp = m_From.CardValue2(m_From.playercards[1]);}
						if (match3 < 2){shpp = m_From.CardValue2(m_From.playercards[2]);}
						else {hpp = m_From.CardValue2(m_From.playercards[2]);}
						if (match4 < 2){shpp = m_From.CardValue2(m_From.playercards[3]);}
						else {hpp = m_From.CardValue2(m_From.playercards[3]);}
						if (match5 < 2){shpp = m_From.CardValue2(m_From.playercards[4]);}
						else {hpp = m_From.CardValue2(m_From.playercards[4]);}
						if (hpp <= 10){hpps = ""+hpp+"s";}
						else
						{
							if(hpp == 11){hpps = "Jacks";}
							if(hpp == 12){hpps = "Queens";}
							if(hpp == 13){hpps = "Kings";}
							if(hpp == 14){hpps = "Aces";}
						}
						if (shpp <= 10){shpps = ""+shpp+"s";}
						else
						{
							if(shpp == 11){shpps = "Jacks";}
							if(shpp == 12){shpps = "Queens";}
							if(shpp == 13){shpps = "Kings";}
							if(shpp == 14){shpps = "Aces";}
						}
						m_From.playermsg = "Full House: "+hpps+" & "+shpps;
						pwinch = 7;
					}
					if((match1 + match2 + match3 + match4 + match5) == 4)
					{
						if (match1 < 1)
						{
							hcp = m_From.CardValue2(m_From.playercards[0]);
							phpp1 = 0;
						}
						else {phpp1 = m_From.CardValue2(m_From.playercards[0]);}
						if (match2 < 1)
						{
							hcp = m_From.CardValue2(m_From.playercards[1]);
							phpp2 = 0;
						}
						else {phpp2 = m_From.CardValue2(m_From.playercards[1]);}
						if (match3 < 1)
						{
							hcp = m_From.CardValue2(m_From.playercards[2]);
							phpp3 = 0;
						}
						else {phpp3 = m_From.CardValue2(m_From.playercards[2]);}
						if (match4 < 1)
						{
							hcp = m_From.CardValue2(m_From.playercards[3]);
							phpp4 = 0;
						}
						else {phpp4 = m_From.CardValue2(m_From.playercards[3]);}
						if (match5 < 1)
						{
							hcp = m_From.CardValue2(m_From.playercards[4]);
							phpp5 = 0;
						}
						else {phpp5 = m_From.CardValue2(m_From.playercards[4]);}
						if (phpp1 == 0){}
						else
						{
							if ((phpp1 >= phpp2) && (phpp1 >= phpp3) && (phpp1 >= phpp4) && (phpp1 >= phpp5)){hpp = m_From.CardValue2(m_From.playercards[0]);}
							else {shpp = m_From.CardValue2(m_From.playercards[0]);}
						}
						if (phpp2 == 0){}
						else
						{
							if ((phpp2 >= phpp1) && (phpp2 >= phpp3) && (phpp2 >= phpp4) && (phpp2 >= phpp5)){hpp = m_From.CardValue2(m_From.playercards[1]);}
							else {shpp = m_From.CardValue2(m_From.playercards[1]);}
						}
						if (phpp3 == 0){}
						else
						{
							if ((phpp3 >= phpp2) && (phpp3 >= phpp1) && (phpp3 >= phpp4) && (phpp3 >= phpp5)){hpp = m_From.CardValue2(m_From.playercards[2]);}
							else {shpp = m_From.CardValue2(m_From.playercards[2]);}
						}
						if (phpp4 == 0){}
						else
						{
							if ((phpp4 >= phpp2) && (phpp4 >= phpp3) && (phpp4 >= phpp1) && (phpp4 >= phpp5)){hpp = m_From.CardValue2(m_From.playercards[3]);}
							else {shpp = m_From.CardValue2(m_From.playercards[3]);}
						}
						if (phpp5 == 0){}
						else
						{
							if ((phpp5 >= phpp2) && (phpp5 >= phpp3) && (phpp5 >= phpp4) && (phpp5 >= phpp1)){hpp = m_From.CardValue2(m_From.playercards[4]);}
							else {shpp = m_From.CardValue2(m_From.playercards[4]);}
						}
						if (hcp <= 10){hcps = ""+hcp;}
						else
						{
							if(hcp == 11){hcps = "Jack";}
							if(hcp == 12){hcps = "Queen";}
							if(hcp == 13){hcps = "King";}
							if(hcp == 14){hcps = "Ace";}
						}
						if (hpp <= 10){hpps = ""+hpp+"s";}
						else
						{
							if(hpp == 11){hpps = "Jacks";}
							if(hpp == 12){hpps = "Queens";}
							if(hpp == 13){hpps = "Kings";}
							if(hpp == 14){hpps = "Aces";}
						}
						if (shpp <= 10){shpps = ""+shpp+"s";}
						else
						{
							if(shpp == 11){shpps = "Jacks";}
							if(shpp == 12){shpps = "Queens";}
							if(shpp == 13){shpps = "Kings";}
							if(shpp == 14){shpps = "Aces";}
						}
						m_From.playermsg = "Two Pairs: "+hpps+" & "+shpps+", Kicker: "+hcps;
						pwinch = 3;
					}
					temp = 0;
					if((match1 + match2 + match3 + match4 + match5) == 2)
					{
						if (match1 < 1){phcp1 = m_From.CardValue2(m_From.playercards[0]);}
						else {hpp = m_From.CardValue2(m_From.playercards[0]);}
						if (match2 < 1){phcp2 = m_From.CardValue2(m_From.playercards[1]);}
						else {hpp = m_From.CardValue2(m_From.playercards[1]);}
						if (match3 < 1){phcp3 = m_From.CardValue2(m_From.playercards[2]);}
						else {hpp = m_From.CardValue2(m_From.playercards[2]);}
						if (match4 < 1){phcp4 = m_From.CardValue2(m_From.playercards[3]);}
						else{hpp = m_From.CardValue2(m_From.playercards[3]);}
						if (match5 < 1){phcp5 = m_From.CardValue2(m_From.playercards[4]);}
						else{hpp = m_From.CardValue2(m_From.playercards[4]);}
						if ((phcp1 >= phcp2) && (phcp1 >= phcp3) && (phcp1 >= phcp4) && (phcp1 >= phcp5)){hcp = m_From.CardValue2(m_From.playercards[0]);}
						if ((phcp2 >= phcp1) && (phcp2 >= phcp3) && (phcp2 >= phcp4) && (phcp2 >= phcp5)){hcp = m_From.CardValue2(m_From.playercards[1]);}
						if ((phcp3 >= phcp2) && (phcp3 >= phcp1) && (phcp3 >= phcp4) && (phcp3 >= phcp5)){hcp = m_From.CardValue2(m_From.playercards[2]);}
						if ((phcp4 >= phcp2) && (phcp4 >= phcp3) && (phcp4 >= phcp1) && (phcp4 >= phcp5)){hcp = m_From.CardValue2(m_From.playercards[3]);}
						if ((phcp5 >= phcp2) && (phcp5 >= phcp3) && (phcp5 >= phcp4) && (phcp5 >= phcp1)){hcp = m_From.CardValue2(m_From.playercards[4]);}
						if (hcp <= 10){hcps = ""+hcp;}
						else
						{
							if(hcp == 11){hcps = "Jack";}
							if(hcp == 12){hcps = "Queen";}
							if(hcp == 13){hcps = "King";}
							if(hcp == 14){hcps = "Ace";}
						}
						if (hpp <= 10){hpps = ""+hpp+"s";}
						else
						{
							if(hpp == 11){hpps = "Jacks";}
							if(hpp == 12){hpps = "Queens";}
							if(hpp == 13){hpps = "Kings";}
							if(hpp == 14){hpps = "Aces";}
						}
						m_From.playermsg = "One Pair: "+hpps+", Kicker: "+hcps;
						pwinch = 2;
					}
				} 
				if(isFlush && isStrt)
				{
					if(m_From.playercards[0] == 10)
					{
						m_From.playermsg = "Royal Flush: "+ccps;
						pwinch = 10;
					}
					else
					{
						hcp = m_From.CardValue2(m_From.playercards[4]);
						if (hcp <= 10){m_From.playermsg = "Straight Flush: "+ccps+", High Card: "+hcp;}
						else
						{
							if(hcp == 11){m_From.playermsg = "Straight Flush: "+ccps+", High Card: Jack";}
							if(hcp == 12){m_From.playermsg = "Straight Flush: "+ccps+", High Card: Queen";}
							if(hcp == 13){m_From.playermsg = "Straight Flush: "+ccps+", High Card: King";}
							if(hcp == 14){m_From.playermsg = "Straight Flush: "+ccps+", High Card: Ace";}
						}
						pwinch = 9;
					}
				}
                hcd = m_From.CardValue2(m_From.dealercards[4]);
				shcd = m_From.CardValue2(m_From.dealercards[3]);
				if (hcd <= 10){hcds = ""+hcd;}
				else
				{
					if(hcd == 11){hcds = "Jack";}
					if(hcd == 12){hcds = "Queen";}
					if(hcd == 13){hcds = "King";}
					if(hcd == 14){hcds = "Ace";}
				}
				if (shcd <= 10){shcds = ""+shcd;}
				else
				{
					if(shcd == 11){shcds = "Jack";}
					if(shcd == 12){shcds = "Queen";}
					if(shcd == 13){shcds = "King";}
					if(shcd == 14){shcds = "Ace";}
				}
				m_From.dealermsg = "High Card: "+hcds+", Kicker: "+shcds;
				dwinch = 1;				
				stcd1 = m_From.CardValue2(m_From.dealercards[0]);
				stcd2 = m_From.CardValue2(m_From.dealercards[1]);
				stcd3 = m_From.CardValue2(m_From.dealercards[2]);
				stcd4 = m_From.CardValue2(m_From.dealercards[3]);
				stcd5 = m_From.CardValue2(m_From.dealercards[4]);
				if(((stcd1 + 1 == stcd2) && (stcd1 + 2 == stcd3) && (stcd1 + 3 == stcd4) && (stcd1 + 4 == stcd5)) || ((stcd1 == 2) && (stcd1 + 1 == stcd2) && (stcd1 + 2 == stcd3) && (stcd1 + 3 == stcd4) && (stcd1 + 12 == stcd5))){isStrtd = true;}
				if (isStrtd)
				{
					if (hcd <= 10){m_From.dealermsg = "Straight, High Card: "+hcd;}
					else
					{
						if(hcd == 11){m_From.dealermsg = "Straight, High Card: Jack";}
						if(hcd == 12){m_From.dealermsg = "Straight, High Card: Queen";}
						if(hcd == 13){m_From.dealermsg = "Straight, High Card: King";}
						if(hcd == 14){m_From.dealermsg = "Straight, High Card: Ace";}
					}
					dwinch = 5;
				}
				Tempd = m_From.CardSuit(m_From.dealercards[0]);
				if(Tempd == m_From.CardSuit(m_From.dealercards[1]) && Tempd == m_From.CardSuit(m_From.dealercards[2]) && Tempd == m_From.CardSuit(m_From.dealercards[3]) && Tempd == m_From.CardSuit(m_From.dealercards[4]))
				{
					isFlushd = true;
					tempd = m_From.dealercards[1];
					ccdst = m_From.CardSuit( tempd );
					if (ccdst == "\u2663"){ccds = "Clubs";}
					if (ccdst == "\u25C6"){ccds = "Diamonds";}
					if (ccdst == "\u2665"){ccds = "Hearts";}
					if (ccdst == "\u2660"){ccds = "Spades";}
					hcd = m_From.CardValue2(m_From.dealercards[4]);
					if (hcd <= 10){hcds = ""+hcd;}
					else
					{
						if(hcd == 11){hcds = "Jack";}
						if(hcd == 12){hcds = "Queen";}
						if(hcd == 13){hcds = "King";}
						if(hcd == 14){hcds = "Ace";}
					}
					m_From.dealermsg = "Flush: "+ccds+", High Card: "+hcds;
					dwinch = 6;
				}
				if(!isStrtd && !isFlushd)
				{
					for ( i = 0; i <= 4; i++  )
					{
						tempd = m_From.CardValue2(m_From.dealercards[i]);
						if ((m_From.CardValue2(m_From.dealercards[0]) == tempd) && i != 0){matchd1++;}
						if ((m_From.CardValue2(m_From.dealercards[1]) == tempd) && i != 1){matchd2++;}
						if ((m_From.CardValue2(m_From.dealercards[2]) == tempd) && i != 2){matchd3++;}
						if ((m_From.CardValue2(m_From.dealercards[3]) == tempd) && i != 3){matchd4++;}
						if ((m_From.CardValue2(m_From.dealercards[4]) == tempd) && i != 4){matchd5++;}
					}
					if((matchd1 == 3) || (matchd2 == 3) || (matchd3 == 3) || (matchd4 == 3) || (matchd5 == 3))
					{
						if (matchd1 == 3){hpd = m_From.CardValue2(m_From.dealercards[0]);}
						if (matchd2 == 3){hpd = m_From.CardValue2(m_From.dealercards[1]);}
						if (matchd3 == 3){hpd = m_From.CardValue2(m_From.dealercards[2]);}
						if (matchd4 == 3){hpd = m_From.CardValue2(m_From.dealercards[3]);}
						if (matchd5 == 3){hpd = m_From.CardValue2(m_From.dealercards[4]);}
						if (hpd <= 10){hpds = ""+hpd+"s";}
						else
						{
							if(hpd == 11){hpds = "Jacks";}
							if(hpd == 12){hpds = "Queens";}
							if(hpd == 13){hpds = "Kings";}
							if(hpd == 14){hpds = "Aces";}
						}
						m_From.dealermsg = "Four of a kind: "+hpds;
						dwinch = 8;
					}
					if((matchd1 == 2) || (matchd2 == 2) || (matchd3 == 2) || (matchd4 == 2) || (matchd5 == 2))
					{
						if (matchd1 == 2){hpd = m_From.CardValue2(m_From.dealercards[0]);}
						if (matchd2 == 2){hpd = m_From.CardValue2(m_From.dealercards[1]);}
						if (matchd3 == 2){hpd = m_From.CardValue2(m_From.dealercards[2]);}
						if (matchd4 == 2){hpd = m_From.CardValue2(m_From.dealercards[3]);}
						if (matchd5 == 2){hpd = m_From.CardValue2(m_From.dealercards[4]);}
						if (hpd <= 10){hpds = ""+hpd+"s";}
						else
						{
							if(hpd == 11){hpds = "Jacks";}
							if(hpd == 12){hpds = "Queens";}
							if(hpd == 13){hpds = "Kings";}
							if(hpd == 14){hpds = "Aces";}
						}
						m_From.dealermsg = "Three of a kind: "+hpds;
						dwinch = 4;
					}
					if((matchd1 + matchd2 + matchd3 + matchd4 + matchd5) == 8)
					{
						if (matchd1 < 2){shpd = m_From.CardValue2(m_From.dealercards[0]);}
						else {hpd = m_From.CardValue2(m_From.dealercards[0]);}
						if (matchd2 < 2){shpd = m_From.CardValue2(m_From.dealercards[1]);}
						else{hpd = m_From.CardValue2(m_From.dealercards[1]);}
						if (matchd3 < 2){shpd = m_From.CardValue2(m_From.dealercards[2]);}
						else {hpd = m_From.CardValue2(m_From.dealercards[2]);}
						if (matchd4 < 2){shpd = m_From.CardValue2(m_From.dealercards[3]);}
						else {hpd = m_From.CardValue2(m_From.dealercards[3]);}
						if (matchd5 < 2){shpd = m_From.CardValue2(m_From.dealercards[4]);}
						else {hpd = m_From.CardValue2(m_From.dealercards[4]);}
						if (hpd <= 10){hpds = ""+hpd+"s";}
						else
						{
							if(hpd == 11){hpds = "Jacks";}
							if(hpd == 12){hpds = "Queens";}
							if(hpd == 13){hpds = "Kings";}
							if(hpd == 14){hpds = "Aces";}
						}
						if (shpd <= 10){shpds = ""+shpd+"s";}
						else
						{
							if(shpd == 11){shpds = "Jacks";}
							if(shpd == 12){shpds = "Queens";}
							if(shpd == 13){shpds = "Kings";}
							if(shpd == 14){shpds = "Aces";}
						}
						m_From.dealermsg = "Full House: "+hpds+" & "+shpds;
						dwinch = 7;
					}
					if((matchd1 + matchd2 + matchd3 + matchd4 + matchd5) == 4)
					{
						if (matchd1 < 1)
						{
							hcd = m_From.CardValue2(m_From.dealercards[0]);
							phpd1 = 0;
						}
						else {phpd1 = m_From.CardValue2(m_From.dealercards[0]);}
						if (matchd2 < 1)
						{
							hcd = m_From.CardValue2(m_From.dealercards[1]);
							phpd2 = 0;
						}
						else {phpd2 = m_From.CardValue2(m_From.dealercards[1]);}
						if (matchd3 < 1)
						{
							hcd = m_From.CardValue2(m_From.dealercards[2]);
							phpd3 = 0;
						}
						else {phpd3 = m_From.CardValue2(m_From.dealercards[2]);}
						if (matchd4 < 1)
						{
							hcd = m_From.CardValue2(m_From.dealercards[3]);
							phpd4 = 0;
						}
						else {phpd4 = m_From.CardValue2(m_From.dealercards[3]);}
						if (matchd5 < 1)
						{
							hcd = m_From.CardValue2(m_From.dealercards[4]);
							phpd5 = 0;
						}
						else
						{phpd5 = m_From.CardValue2(m_From.dealercards[4]);}
						if (phpd1 == 0){}
						else
						{
							if ((phpd1 >= phpd2) && (phpd1 >= phpd3) && (phpd1 >= phpd4) && (phpd1 >= phpd5)){hpd = m_From.CardValue2(m_From.dealercards[0]);}
							else {shpd = m_From.CardValue2(m_From.dealercards[0]);}
						}
						if (phpd2 == 0){}
						else
						{
							if ((phpd2 >= phpd1) && (phpd2 >= phpd3) && (phpd2 >= phpd4) && (phpd2 >= phpd5)){hpd = m_From.CardValue2(m_From.dealercards[1]);}
							else {shpd = m_From.CardValue2(m_From.dealercards[1]);}
						}
						if (phpd3 == 0){}
						else
						{
							if ((phpd3 >= phpd2) && (phpd3 >= phpd1) && (phpd3 >= phpd4) && (phpd3 >= phpd5)){hpd = m_From.CardValue2(m_From.dealercards[2]);}
							else {shpd = m_From.CardValue2(m_From.dealercards[2]);}
						}
						if (phpd4 == 0){}
						else
						{
							if ((phpd4 >= phpd2) && (phpd4 >= phpd3) && (phpd4 >= phpd1) && (phpd4 >= phpd5)){hpd = m_From.CardValue2(m_From.dealercards[3]);}
							else {shpd = m_From.CardValue2(m_From.dealercards[3]);}
						}
						if (phpd5 == 0){}
						else
						{
							if ((phpd5 >= phpd2) && (phpd5 >= phpd3) && (phpd5 >= phpd4) && (phpd5 >= phpd1)){hpd = m_From.CardValue2(m_From.dealercards[4]);}
							else {shpd = m_From.CardValue2(m_From.dealercards[4]);}
						}
						if (hcd <= 10){hcds = ""+hcd;}
						else
						{
							if(hcd == 11){hcds = "Jack";}
							if(hcd == 12){hcds = "Queen";}
							if(hcd == 13){hcds = "King";}
							if(hcd == 14){hcds = "Ace";}
						}
						if (hpd <= 10){hpds = ""+hpd+"s";}
						else
						{
							if(hpd == 11){hpds = "Jacks";}
							if(hpd == 12){hpds = "Queens";}
							if(hpd == 13){hpds = "Kings";}
							if(hpd == 14){hpds = "Aces";}
						}
						if (shpd <= 10){shpds = ""+shpd+"s";}
						else
						{
							if(shpd == 11){shpds = "Jacks";}
							if(shpd == 12){shpds = "Queens";}
							if(shpd == 13){shpds = "Kings";}
							if(shpd == 14){shpds = "Aces";}
						}
						m_From.dealermsg = "Two Pairs: "+hpds+" & "+shpds+", Kicker: "+hcds;
						dwinch = 3;
					}
					tempd = 0;
					if((matchd1 + matchd2 + matchd3 + matchd4 + matchd5) == 2)
					{
						if (matchd1 < 1){phcd1 = m_From.CardValue2(m_From.dealercards[0]);}
						else {hpd = m_From.CardValue2(m_From.dealercards[0]);}
						if (matchd2 < 1){phcd2 = m_From.CardValue2(m_From.dealercards[1]);}
						else {hpd = m_From.CardValue2(m_From.dealercards[1]);}
						if (matchd3 < 1){phcd3 = m_From.CardValue2(m_From.dealercards[2]);}
						else {hpd = m_From.CardValue2(m_From.dealercards[2]);}
						if (matchd4 < 1){phcd4 = m_From.CardValue2(m_From.dealercards[3]);}
						else {hpd = m_From.CardValue2(m_From.dealercards[3]);}
						if (matchd5 < 1){phcd5 = m_From.CardValue2(m_From.dealercards[4]);}
						else {hpd = m_From.CardValue2(m_From.dealercards[4]);}
						if ((phcd1 >= phcd2) && (phcd1 >= phcd3) && (phcd1 >= phcd4) && (phcd1 >= phcd5)){hcd = m_From.CardValue2(m_From.dealercards[0]);}
						if ((phcd2 >= phcd1) && (phcd2 >= phcd3) && (phcd2 >= phcd4) && (phcd2 >= phcd5)){hcd = m_From.CardValue2(m_From.dealercards[1]);}
						if ((phcd3 >= phcd2) && (phcd3 >= phcd1) && (phcd3 >= phcd4) && (phcd3 >= phcd5)){hcd = m_From.CardValue2(m_From.dealercards[2]);}
						if ((phcd4 >= phcd2) && (phcd4 >= phcd3) && (phcd4 >= phcd1) && (phcd4 >= phcd5)){hcd = m_From.CardValue2(m_From.dealercards[3]);}
						if ((phcd5 >= phcd2) && (phcd5 >= phcd3) && (phcd5 >= phcd4) && (phcd5 >= phcd1)){hcd = m_From.CardValue2(m_From.dealercards[4]);}
						if (hcd <= 10){hcds = ""+hcd;}
						else
						{
							if(hcd == 11){hcds = "Jack";}
							if(hcd == 12){hcds = "Queen";}
							if(hcd == 13){hcds = "King";}
							if(hcd == 14){hcds = "Ace";}
						}
						if (hpd <= 10){hpds = ""+hpd+"s";}
						else
						{
							if(hpd == 11){hpds = "Jacks";}
							if(hpd == 12){hpds = "Queens";}
							if(hpd == 13){hpds = "Kings";}
							if(hpd == 14){hpds = "Aces";}
						}
						m_From.dealermsg = "One Pair: "+hpds+", Kicker: "+hcds;
						dwinch = 2;
					}
				}
				if ( isFlushd && isStrtd )
				{
					if(m_From.dealercards[0] == 10)
					{
						m_From.dealermsg = "Royal Flush: "+ccds;
						dwinch = 10;
					}
					else
					{
						hcd = m_From.CardValue2(m_From.dealercards[4]);

						if (hcd <= 10){m_From.dealermsg = "Straight Flush: "+ccds+", High Card: "+hcd;}
						else
						{
							if(hcd == 11){m_From.dealermsg = "Straight Flush: "+ccds+", High Card: Jack";}
							if(hcd == 12){m_From.dealermsg = "Straight Flush: "+ccds+", High Card: Queen";}
							if(hcd == 13){m_From.dealermsg = "Straight Flush: "+ccds+", High Card: King";}
							if(hcd == 14){m_From.dealermsg = "Straight Flush: "+ccds+", High Card: Ace";}
						}
						dwinch = 9;
					}
				}
				if (pwinch > dwinch)
				{
					m_From.gamemsg = "You win!";
					m_From.pwin = 1;
				}
				else if (pwinch == dwinch)
				{
					if (pwinch == 1)
					{
						if (hcp > hcd)
						{
							m_From.gamemsg = "You win!";
							m_From.pwin = 1;
						}
						else if (hcp < hcd)
						{
							m_From.gamemsg = "You loose!";
							m_From.pwin = 2;
						}
						else
						{
							if (shcp > shcd)
							{
								m_From.gamemsg = "You win!";
								m_From.pwin = 1;
							}
							else if (shcp < shcd)
							{
								m_From.gamemsg = "You loose!";
								m_From.pwin = 2;
							}
							else
							{
								m_From.gamemsg = "Tie!";
								m_From.pwin = 3;
							}
						}
					}
					else if (pwinch == 2)
					{
						if (hpd > hpp)
						{
							m_From.gamemsg = "You loose!";
							m_From.pwin = 2;
						}
						else if (hpd < hpp)
						{
							m_From.gamemsg = "You win!";
							m_From.pwin = 1;
						}
						else 
						{
							if (hcd < hcp)
							{
								m_From.gamemsg = "You win!";
								m_From.pwin = 1;
							}
							else if (hcd > hcp)
							{
								m_From.gamemsg = "You loose!";
								m_From.pwin = 2;
							}
							else
							{
								m_From.gamemsg = "Tie!";
								m_From.pwin = 3;
							}
						}
					}
					else if (pwinch == 3)
					{
						if (hpp > hpd)
						{
							m_From.gamemsg = "You win!";
							m_From.pwin = 1;
						}
						else if (hpp < hpd)
						{
							m_From.gamemsg = "You loose!";
							m_From.pwin = 2;
						}
						else
						{
							if (shpp > shpd)
							{
								m_From.gamemsg = "You win!";
								m_From.pwin = 1;
							}
							else if (shpp < shpd)
							{
								m_From.gamemsg = "You loose!";
								m_From.pwin = 2;
							}
							else
							{
								if (hcp > hcd)
								{
									m_From.gamemsg = "You win!";
									m_From.pwin = 1;
								}
								else if (hcp < hcd)
								{
									m_From.gamemsg = "You loose!";
									m_From.pwin = 2;
								}
								else
								{
									m_From.gamemsg = "Tie!";
									m_From.pwin = 3;
								}
							}
						}
					}
					else if (pwinch == 4)
					{
						if (hpp > hpd)
						{
							m_From.gamemsg = "You win!";
							m_From.pwin = 1;
						}
						else
						{
							m_From.gamemsg = "You loose!";
							m_From.pwin = 2;
						}
					}
					else if (pwinch == 5)
					{
						if (hcp > hcd)
						{
							m_From.gamemsg = "You win!";
							m_From.pwin = 1;
						}
						else if (hcp < hcd)
						{
							m_From.gamemsg = "You loose!";
							m_From.pwin = 2;
						}
						else
						{
							m_From.gamemsg = "Tie!";
							m_From.pwin = 3;
						}
					}
					else if (pwinch == 6)
					{
						if (hcp > hcd)
						{
							m_From.gamemsg = "You win!";
							m_From.pwin = 1;
						}
						else if (hcp < hcd)
						{
							m_From.gamemsg = "You loose!";
							m_From.pwin = 2;
						}
						else
						{
							m_From.gamemsg = "Tie!";
							m_From.pwin = 3;
						}
					}
					else if (pwinch == 7)
					{
						if (hpp > hpd)
						{
							m_From.gamemsg = "You win!";
							m_From.pwin = 1;
						}
						else
						{
							m_From.gamemsg = "You loose!";
							m_From.pwin = 2;
						}
					}
					else if (pwinch == 8)
					{
						if (hpp > hpd)
						{
							m_From.gamemsg = "You win!";
							m_From.pwin = 1;
						}
						else
						{
							m_From.gamemsg = "You loose!";
							m_From.pwin = 2;
						}
					}
					else if (pwinch == 9)
					{
						if (hcp > hcd)
						{
							m_From.gamemsg = "You win!";
							m_From.pwin = 1;
						}
						else if (hcp < hcd)
						{
							m_From.gamemsg = "You loose!";
							m_From.pwin = 2;
						}
						else
						{
							m_From.gamemsg = "Tie!";
							m_From.pwin = 3;
						}
					}
					else if (pwinch == 10)
					{
						m_From.gamemsg = "Tie!";
						m_From.pwin = 3;
					}
					else
					{
						m_From.gamemsg = "Tie!";
						m_From.pwin = 3;
					}
				}
				else
				{
					m_From.gamemsg = "You loose!";
					m_From.pwin = 2;
				}			
				m_From.gamepot = (m_From.playerbet + m_From.playerraise + m_From.playerraise2);
				m_From.playerbet = 500;
				m_From.playerraise = 0;
				m_From.playerraise2 = 0;
				m_From.ftnc = 0;
				m_From.showmsg = true;
				if (m_From.pwin == 1)
				{
					m_From.payplayer(from, (m_From.gamepot * 2));
					Effects.PlaySound( from.Location, from.Map, 0x36 );
					m_From.gamestats[1] += 1;
				}
				else if (m_From.pwin == 3)
				{
					m_From.payplayer(from, m_From.gamepot);
					Effects.PlaySound( from.Location, from.Map, 0x36 );
					m_From.gamestats[2] += 1;
				}
				else {m_From.gamestats[0] += 1;}
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
