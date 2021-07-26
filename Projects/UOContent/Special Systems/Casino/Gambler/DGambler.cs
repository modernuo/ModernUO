using System;
using System.Collections;
using Server.Items;
using Server.ContextMenus;
using Server.Misc;
using Server.Network;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Mobiles
{
	public class DGambler : BaseCreature
	{
		public override bool DisallowAllMoves{ get{ return true; } }
		public override bool ClickTitle{ get{ return false; } }
		public override bool CanTeach{ get{ return false; } }
		private bool startgump = true;
		private bool busy;
		private int [] gamestats = new int[3];
		private int wuerfel1;
		private int wuerfel2;
		private int goldbp = 0;
		private int einsatz;
		private int tip;
		private string gamemsg = "Good luck!";
		private Mobile m_player;
		
		[Constructible]
        public DGambler() : base( AIType.AI_Melee, FightMode.None, 10, 1, 0.2, 0.4 ) 
        {
            SetStr( 10, 30 );
            SetDex( 10, 30 );
            SetInt( 10, 30 );
			Name = "Anais";
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
    
		public void gamble( Mobile from, int tip, int einsatz )
		{	
  			Container pack = from.Backpack; 
      		if ( pack != null && pack.ConsumeTotal( typeof( Gold ), einsatz ) )
    		{
				int gewinn = 0;		
    			wuerfel1 = Utility.Random( 6 ) + 1;
    			wuerfel2 = Utility.Random( 6 ) + 1;

				if(tip == wuerfel1 && tip == wuerfel2)
				{
					gewinn = einsatz * 4;
					gamemsg = "Two hits! You win " + gewinn + " gold.";
					gamestats[2] += 1;
				}
				else if(tip == wuerfel1 || tip == wuerfel2)
				{
					gewinn = einsatz * 2;
					gamemsg = "One hit! You win " + gewinn + " gold.";
					gamestats[1] += 1;
				}
    			else
    			{
					gewinn = 0;
					gamemsg = "No hit! Anais wins.";
					gamestats[0] += 1;
				}
                 
				if(gewinn > 0)
				{
					from.AddToBackpack( new Gold(gewinn) );
					goldbp = from.Backpack.GetAmount(typeof(Gold));
				}
				else {goldbp = from.Backpack.GetAmount(typeof(Gold));}
    		}	
			else {this.Say("You do not have enough gold!");}	
		}
		
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
				else if  (e.Speech.ToLower() == "dice" || e.Speech.ToLower() == "Dice")
				{
					if (!busy)
					{
						busy = true;
						m_player = from;
						message = "So, you want to try your luck.";
						this.Say( message );
						checkgold(from);
						m_player.SendGump( new GambleGump( m_player, this ));
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

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.AccessLevel >= AccessLevel.Seer ){from.SendGump( new DGamblerStatsGump( this ) );}
			else{base.OnDoubleClick( from );}
		}
   		
		public override bool AlwaysMurderer{ get{ return false; } }

		public DGambler( Serial serial ) : base( serial )
		{
		}

		public class DGamblerStatsGump : Gump
		{
			private DGambler m_From;

			public DGamblerStatsGump( DGambler gambler ) : base( 10, 10 )
			{
				m_From = gambler;
				AddPage( 0 );
				AddBackground( 30, 100, 90, 100, 5120 );
				AddLabel( 45, 100, 70, "Dicegame" );
				AddLabel( 45, 120, 600, "Wins: "+m_From.gamestats[0] );
				AddLabel( 45, 135, 600, "S-L: "+m_From.gamestats[1] );
				AddLabel( 45, 150, 600, "D-L: "+m_From.gamestats[2] );
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

		public class GambleGump : Gump
		{
			private Mobile m_mobile;
			private DGambler m_gambler;
		
			public GambleGump( Mobile mobile, DGambler gambler) : base(0, 0)
			{
				m_mobile = mobile;
				m_gambler = gambler;
				if (m_gambler.einsatz <= 99)
				{
					m_gambler.einsatz = 500;
				}
				if (m_gambler.tip == 0)
				{
					m_gambler.tip = 3;
				}
				this.Closable = false;
				this.Disposable = true;
				this.Draggable = true;
				this.Resizable = false;
				this.AddPage(0);
				this.AddBackground(50, 28, 500, 320, 2620);
				this.AddImage(362, 88, 1417);
				if (m_gambler.wuerfel1 == 0){this.AddImage(381, 107, 11285);}
				if (m_gambler.wuerfel1 == 1){this.AddImage(381, 107, 11280);}
				if (m_gambler.wuerfel1 == 2){this.AddImage(381, 107, 11281);}
				if (m_gambler.wuerfel1 == 3){this.AddImage(381, 107, 11282);}
				if (m_gambler.wuerfel1 == 4){this.AddImage(381, 107, 11283);}
				if (m_gambler.wuerfel1 == 5){this.AddImage(381, 107, 11284);}
				if (m_gambler.wuerfel1 == 6){this.AddImage(381, 107, 11285);}
				if (m_gambler.wuerfel2 == 0){this.AddImage(402, 128, 11285);}
				if (m_gambler.wuerfel2 == 1){this.AddImage(402, 128, 11280);}
				if (m_gambler.wuerfel2 == 2){this.AddImage(402, 128, 11281);}
				if (m_gambler.wuerfel2 == 3){this.AddImage(402, 128, 11282);}
				if (m_gambler.wuerfel2 == 4){this.AddImage(402, 128, 11283);}
				if (m_gambler.wuerfel2 == 5){this.AddImage(402, 128, 11284);}
				if (m_gambler.wuerfel2 == 6){this.AddImage(402, 128, 11285);}
				this.AddImageTiled( 75, 53, 195, 130, 9274 );
				this.AddLabel( 83, 58, 800, "Bet your gold on a number" );
				this.AddLabel( 83, 73, 800, "between 1 and 6." );
				this.AddLabel( 83, 88, 800, "If I throw it once, you get" );
				this.AddLabel( 83, 103, 800, "double your bet, if twice, you" );
				this.AddLabel( 83, 118, 800, "get four times your bet." );
				this.AddLabel( 83, 143, 800, "If I do not throw it:" );
				this.AddLabel( 83, 158, 800, "Bad luck for you!" );
				if (m_gambler.gamemsg == "No hit! Anais wins.")
				{
					this.AddLabel( 315, 205, 1500, ""+m_gambler.gamemsg );
				}
				else
				{
					this.AddLabel( 315, 205, 800, ""+m_gambler.gamemsg );
				}
				this.AddImage(163, 203, 2443);
				this.AddButton( 229, 198, 5600, 5604, 2, GumpButtonType.Reply, 0 );
				this.AddLabel(98, 204, 800, "your bet:");
				this.AddLabel( 180, 204, 0, ""+m_gambler.einsatz );
				this.AddButton( 229, 216, 5602, 5606, 3, GumpButtonType.Reply, 0 );
				this.AddImage(163, 253, 2443);
				this.AddButton( 229, 248, 5600, 5604, 4, GumpButtonType.Reply, 0 );
				this.AddLabel(98, 254, 800, "bet on:");
				this.AddLabel( 190, 254, 0, ""+m_gambler.tip );
				this.AddButton( 229, 266, 5602, 5606, 5, GumpButtonType.Reply, 0 );
				this.AddButton( 465, 45, 1027, 1028, 6, GumpButtonType.Reply, 0 );
				this.AddLabel( 315, 286, 600, "Gold in your backpack:" );
				this.AddLabel( 315, 301, 600, ""+m_gambler.goldbp );
				this.AddImage( 455, 273, 9811 );
				this.AddItem ( 452, 306, 3823 );
				this.AddButton( 97, 297, 2151, 2154, 1, GumpButtonType.Reply, 0 );
				this.AddLabel( 131, 301, 800, "roll dices" );
			}
			
			public override void OnResponse( NetState state, RelayInfo info )  
			{
				Mobile from = state.Mobile;

				if (m_gambler.startgump == true)
				{
					m_gambler.einsatz = 500;
					m_gambler.tip = 3;
					m_gambler.startgump = false;
				}
				
				switch ( info.ButtonID ) 
				{
					case 1:
					{
						if ( m_gambler.einsatz != 0 && m_gambler.tip != 0){m_gambler.gamble(from, m_gambler.tip, m_gambler.einsatz);}
						m_mobile.SendGump( new GambleGump( m_mobile, m_gambler ) );
						break;
					}
					case 2:
					{
						m_gambler.einsatz += 100;
						if (m_gambler.einsatz > 1000){m_gambler.einsatz = 100;}
						m_mobile.SendGump( new GambleGump( m_mobile, m_gambler ) );
						break;
					}
					case 3:
					{
						m_gambler.einsatz -= 100;
						if (m_gambler.einsatz < 100){m_gambler.einsatz = 1000;}
						m_mobile.SendGump( new GambleGump( m_mobile, m_gambler ) );
						break;
					}
					case 4:
					{
						m_gambler.tip += 1;
						if (m_gambler.tip > 6){m_gambler.tip = 1;}
						m_mobile.SendGump( new GambleGump( m_mobile, m_gambler ) );
						break;
					}
					case 5:
					{
						m_gambler.tip -= 1;
						if (m_gambler.tip < 1){m_gambler.tip = 6;}
						m_mobile.SendGump( new GambleGump( m_mobile, m_gambler ) );
						break;
					}
					case 6: 
					{
						m_gambler.startgump = true;
						m_gambler.busy = false;
						m_gambler.einsatz = 500;
						m_gambler.tip = 3;
						m_gambler.wuerfel1 = 0;
						m_gambler.wuerfel2 = 0;
						m_gambler.gamemsg = "Good luck!";
						Effects.PlaySound( from.Location, from.Map, 0x1e9 );
						break;
					}
				}
			}
		}

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
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
			busy = reader.ReadBool();
			for ( int i = 0; i <= 2; ++i )
			{
				gamestats[i]=reader.ReadInt();
			}
		}
	}
}
