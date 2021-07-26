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
	public class WGambler : BaseCreature
	{
		public override bool ClickTitle{ get{ return false; } }
		public override bool CanTeach{ get{ return false; } }
		
		public int m_guthaben;

		[CommandProperty( AccessLevel.GameMaster )]
		public int guthaben
		{
			get{ return m_guthaben; }
			set{ m_guthaben = value; }
		}		
 
 		[Constructible]
		public WGambler() : base( AIType.AI_Melee, FightMode.Aggressor, 20, 1, 0.2, 0.4 )
		{
      		this.Female = true;
			Body = 0x191;
			Name = "Anais";
			Title = "the gambler";

			AddItem( new FancyShirt( 1908 ));
			AddItem( new LongPants( ));
			AddItem( new Bandana( 1908 ));

			SetStr( 25, 50 );
			SetDex( 41, 45 );
			SetInt( 21, 35 );
			SetHits( 188, 208 );
			
			CantWalk = true;
      		Blessed = true;

			Fame = 0;
			Karma = -1000;
			VirtualArmor = 66;

			AddItem( new Backpack() );  
			guthaben = 10000;
		}

		private static TimeSpan m_NextPlapperDelay = TimeSpan.FromSeconds( 30.0 );

		private DateTime m_NextPlapper;

		public override void OnDoubleClick(Mobile m_Mobile)
		{
			if( !( m_Mobile is PlayerMobile ) )
			return;
					
			if( m_Mobile == null )
			return;
		
			PlayerMobile mobile = (PlayerMobile) m_Mobile;
			{
				if ( ! mobile.HasGump<GambleGump>() )
				{						   
					mobile.SendGump( new GambleGump( mobile, this ));		
				}
			}
		}
    
		public void gamble( Mobile from, int tip, int einsatz )
		{
  			if( einsatz > 10000)	
  			{
				Say("I will not take any bets above 10000 gold!");
				return;
			}
  			else if( m_guthaben < (einsatz * 4))	
  			{
				Say("Sorry, I do not have that much cash at the moment!");
				return;
			}	
 			
 			if(tip < 1 || tip > 6)
 			{
 				Say("A dice has six sides, my friend. Not more, not less.");
				return;
			}	
 			
			if ( einsatz > 0 )
  			{
  				Container pack = from.Backpack; 
      			if ( pack != null && pack.ConsumeTotal( typeof( Gold ), einsatz ) )
    			{
    				string gegeben = "Du gibst ihm " + einsatz + " Gold";
					from.SendMessage(gegeben);
					int gewinn = 0;		
    				int wuerfel1 = Utility.Random( 6 ) + 1;
    				int wuerfel2 = Utility.Random( 6 ) + 1;
					string gesetzt = "You bet " + einsatz + " gold on the " + tip + ".";
    				this.Say(gesetzt);
    				string wurf = "*throws a " + wuerfel1 + " and a " + wuerfel2 + "*";
    				this.Say(wurf);
					string ergebnis;

					if(tip == wuerfel1 && tip == wuerfel2)
					{
						gewinn = einsatz * 4;
						ergebnis = "Damn! Two hits!You win " + gewinn + " gold!!";
					}
					else if(tip == wuerfel1 || tip == wuerfel2)
					{
						gewinn = einsatz * 2;
						ergebnis = "One hit! You win " + gewinn + " gold!";
					}
    			   	else
    			   	{
						ergebnis = "What a pity...";
						Emote("*she smirks*");
						gewinn = 0;
					}
					Say(ergebnis);
                 
					if(gewinn > 0)
					{
						from.AddToBackpack( new Gold(gewinn) );
						m_guthaben =  m_guthaben - gewinn;
					}
					else
					{
						m_guthaben = m_guthaben + einsatz;
					}
    				m_NextPlapper = DateTime.Now + TimeSpan.FromSeconds( 200 );
    			}	
				else
				{
					this.Say("You do not have enough gold!");
				}	
    		}			
    		else
    		{
				this.Say("That seems to be not enough.");
    		}
		}

		public override void OnMovement(Mobile m, Point3D oldLocation) 
		{                    
			if ( m is PlayerMobile && this.InLOS( m ) && this.CanSee( m ))
			{
				this.Direction = GetDirectionTo( m.Location );
			}

			if ( m.InRange( this, 10 ) && DateTime.Now >= m_NextPlapper && this.InLOS( m ) && this.CanSee( m ) && m is PlayerMobile && this.Combatant == null) 
			{                
				this.CurrentSpeed = this.ActiveSpeed;
				m_NextPlapper = DateTime.Now + m_NextPlapperDelay;
				switch ( Utility.Random( 3 ))
  				{
  					case 0: m_NextPlapper = DateTime.Now + m_NextPlapperDelay;PublicOverheadMessage(Server.Network.MessageType.Label, 0, false, "Do you feel lucky??" ); break;
  					case 1: m_NextPlapper = DateTime.Now + m_NextPlapperDelay;PublicOverheadMessage(Server.Network.MessageType.Label, 0, false, "Hey, would you like to win some gold?" ); break;
  					case 2: m_NextPlapper = DateTime.Now + m_NextPlapperDelay;PublicOverheadMessage(Server.Network.MessageType.Label, 0, false, "How about winning four times your bet?" ); break;
  				}
			}						
		}
		
		public override bool HandlesOnSpeech( Mobile from )
		{
			if ( from.InRange( this.Location, 8 ) && this.InLOS(from) )
			{
				return true;
			}
			return base.HandlesOnSpeech( from );
		}
		
		public override void OnSpeech( SpeechEventArgs e ) 
		{ 
			Mobile m = e.Mobile;   
			string gesagt = e.Speech.ToLower();
			if ( gesagt.IndexOf( "leave" ) >= 0 || gesagt.IndexOf( "quiet" ) >= 0 || gesagt.IndexOf( "shush" ) >= 0)
			{
				PublicOverheadMessage(Server.Network.MessageType.Label, 0, false, "Nothing ventured, nothing gained." );
				this.CurrentSpeed = this.PassiveSpeed;
				m_NextPlapper = DateTime.Now + TimeSpan.FromSeconds( 120 );
			}
			if ( gesagt.IndexOf( this.Name.ToLower() ) >= 0 )
			{
				PublicOverheadMessage(Server.Network.MessageType.Label, 0, false, "Huh?" );
				m_NextPlapper = DateTime.Now + TimeSpan.FromSeconds( 120 );
			}			   
   		}			
   		
		public override bool AlwaysMurderer{ get{ return false; } }

		public WGambler( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( m_guthaben );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );
			m_guthaben = reader.ReadInt();
			int version = reader.ReadInt();
		}
	}
}

namespace Server.Gumps
{
	public class GambleGump : Server.Gumps.Gump
	{
		private Mobile m_mobile;
		private WGambler m_gambler;
    
		public GambleGump( Mobile mobile, WGambler gambler) : base(0, 0)
		{
			m_mobile = mobile;
			m_gambler = gambler;
		
			this.Closable=true;
			this.Disposable=true;
			this.Draggable=true;
			this.Resizable=false;
			this.AddPage(0);
			this.AddBackground(50, 28, 500, 390, 9380);
			this.AddButton(438, 351, 247, 248, 1, GumpButtonType.Reply, 0); //Okay
			this.AddImage(107, 118, 62);
			this.AddImage(106, 79, 62);
			this.AddImageTiled(200, 184, 35, 32, 3004);
			this.AddItem(139, 178, 1175);
			this.AddImage(155, 195, 2362);
			this.AddImage(204, 188, 2362);
			this.AddImage(218, 200, 2362);
			this.AddImage(438, 305, 2443);
			this.AddImage(336, 270, 2440);
			this.AddLabel(338, 250, 0, @"Bet:");
			this.AddLabel(338, 305, 0, @"Bet on:");
			this.AddTextEntry(397, 272, 43, 20, 0, 1, @"100");
			this.AddTextEntry(463, 306, 21, 20, 0, 2, @"6");
			this.AddHtml( 303, 68, 210, 172, @"Bet your gold on a number between 1 and 6.<br>If I throw it once, you get double your bet, if twice, you get four times your bet.<br>If I do not throw it: bad luck for you!", (bool)true, (bool)true);
		}
    
		private int einsatz;
		private int tip;
		
		public override void OnResponse( NetState state, RelayInfo info )  
		{
			Mobile from = state.Mobile; 
			
			switch ( info.ButtonID ) 
			{
				case 0: 
				{
					break;
				}
				case 1:
				{
					TextRelay text_einsatz = info.GetTextEntry(1);
					TextRelay text_tip = info.GetTextEntry(2);
					if ( text_einsatz != null && text_tip != null)
					{
						try
						{
							einsatz = Convert.ToInt32( text_einsatz.Text );
							tip = Convert.ToInt32( text_tip.Text );
							m_gambler.gamble(from, tip, einsatz);
						}
						catch
						{
							Console.WriteLine("gamble convert-crash abgefangen");
						}
					}
				   break; 
				} 
				default:
				{
					break;
				}
			}
		}
	}
}
