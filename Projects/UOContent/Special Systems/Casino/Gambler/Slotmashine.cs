using System;
using System.Collections;
using Server.Items;
using Server.ContextMenus;
using Server.Misc;
using Server.Network;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Items
{
	public class Slotmashine : Item
	{
		private int jackpot = 50000;
		private int goldbp;
		private int slot1 = 3;
		private int slot2 = 3;
		private int slot3 = 3;
		private int slot4 = 3;
		private int slot5 = 3;
		private int bet = 500;


		public override string DefaultName
		{
			get { return "a slot mashine"; }
		}

		[Constructible]
		public Slotmashine() : base( 0xED4 )
		{
			Movable = false;
			Hue = 2207;
		}

		public Slotmashine( Serial serial ) : base( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.InRange( this.GetWorldLocation(), 3 ) )
			{
				from.CloseGump<GambleGump>();
				from.SendGump( new GambleGump( this, from ) );
			}
			else
			{
			}	
		}

		public void gamble( Mobile from, int bet )
		{	
			int match1 = 0, match2 = 0, match3 = 0, match4 = 0, match5 = 0;

  			Container pack = from.Backpack; 
      		if ( pack != null && pack.ConsumeTotal( typeof( Gold ), bet ) )
    		{
				slot1 = Utility.Random( 9 );
				slot2 = Utility.Random( 9 );
				slot3 = Utility.Random( 9 );
				slot4 = Utility.Random( 9 );
				slot5 = Utility.Random( 9 );

				if (slot1 == slot2){match1 += 1;}
				if (slot1 == slot3){match1 += 1;}
				if (slot1 == slot4){match1 += 1;}
				if (slot1 == slot5){match1 += 1;}

				if (slot2 == slot1){match2 += 1;}
				if (slot2 == slot3){match2 += 1;}
				if (slot2 == slot4){match2 += 1;}
				if (slot2 == slot5){match2 += 1;}

				if (slot3 == slot2){match3 += 1;}
				if (slot3 == slot1){match3 += 1;}
				if (slot3 == slot4){match3 += 1;}
				if (slot3 == slot5){match3 += 1;}

				if (slot4 == slot2){match4 += 1;}
				if (slot4 == slot3){match4 += 1;}
				if (slot4 == slot1){match4 += 1;}
				if (slot4 == slot5){match4 += 1;}

				if (slot5 == slot2){match5 += 1;}
				if (slot5 == slot3){match5 += 1;}
				if (slot5 == slot4){match5 += 1;}
				if (slot5 == slot1){match5 += 1;}

				if ((match1 == 4) || (match2 == 4) || (match3 == 4) || (match4 == 4) || (match5 == 4))
				{
					from.AddToBackpack(new Gold(100 * bet));
					from.AddToBackpack(new Gold(jackpot));
					jackpot = 50000;
					switch(Utility.Random(6))
					{
						case 0: from.AddToBackpack( new GamblingReward1() ); break;
						case 1: from.AddToBackpack( new GamblingReward2() ); break;
						case 2: from.AddToBackpack( new GamblingReward3() ); break;
						case 3: from.AddToBackpack( new GamblingReward4() ); break;
						case 4: from.AddToBackpack( new GamblingReward5() ); break;
						case 5: from.AddToBackpack( new GamblingReward6() ); break;
					}
				}
				else if ((match1 == 3) || (match2 == 3) || (match3 == 3) || (match4 == 3) || (match5 == 3))
				{
					from.AddToBackpack(new Gold(25 * bet));
				}
				else if (match1 + match2 + match3 + match4 + match5 == 8)
				{
					from.AddToBackpack(new Gold(5 * bet));
				}
				else if ((match1 == 2) || (match2 == 2) || (match3 == 2) || (match4 == 2) || (match5 == 2))
				{
					from.AddToBackpack(new Gold(3 * bet));
				}
				else if (match1 + match2 + match3 + match4 + match5 == 4)
				{
					from.AddToBackpack(new Gold(2 * bet));
				}
				else
				{
					jackpot += (bet / 2);
				}
    		}	
			else {from.SendMessage(38, "You do not have enough gold!");}	
		}

		public class GambleGump : Gump
		{
			private Mobile m_mobile;
			private Slotmashine m_gambler;
		
			public GambleGump( Slotmashine game, Mobile from ) : base( 50, 50)
			{
				m_mobile = from;
				m_gambler = game;
				m_gambler.goldbp = m_mobile.Backpack.GetAmount(typeof(Gold));

				this.Closable = false;
				this.Disposable = true;
				this.Draggable = true;
				this.Resizable = false;
				this.AddPage(0);
				this.AddBackground(50, 28, 385, 320, 2620);

				this.AddImageTiled( 72, 53, 60, 120, 3004 );
				this.AddImageTiled( 142, 53, 60, 120, 3004 );
				this.AddImageTiled( 212, 53, 60, 120, 3004 );
				this.AddImageTiled( 282, 53, 60, 120, 3004 );
				this.AddImageTiled( 352, 53, 60, 120, 3004 );

				if (m_gambler.slot1 == 0){this.AddItem(79, 68, 9080);}
				if (m_gambler.slot1 == 1){this.AddItem(83, 77, 15786);}
				if (m_gambler.slot1 == 2){this.AddItem(80, 77, 13900);}
				if (m_gambler.slot1 == 3){this.AddItem(79, 59, 12252);}
				if (m_gambler.slot1 == 4){this.AddItem(77, 73, 11740);}
				if (m_gambler.slot1 == 5){this.AddItem(76, 64, 10903);}
				if (m_gambler.slot1 == 6){this.AddItem(76, 85, 4354);}
				if (m_gambler.slot1 == 7){this.AddItem(76, 88, 15122);}
				if (m_gambler.slot1 == 8){this.AddItem(79, 73, 6257);}

				if (m_gambler.slot2 == 0){this.AddItem(149, 68, 9080);}
				if (m_gambler.slot2 == 1){this.AddItem(153, 77, 15786);}
				if (m_gambler.slot2 == 2){this.AddItem(150, 77, 13900);}
				if (m_gambler.slot2 == 3){this.AddItem(149, 59, 12252);}
				if (m_gambler.slot2 == 4){this.AddItem(147, 73, 11740);}
				if (m_gambler.slot2 == 5){this.AddItem(146, 64, 10903);}
				if (m_gambler.slot2 == 6){this.AddItem(146, 85, 4354);}
				if (m_gambler.slot2 == 7){this.AddItem(146, 88, 15122);}
				if (m_gambler.slot2 == 8){this.AddItem(149, 73, 6257);}

				if (m_gambler.slot3 == 0){this.AddItem(219, 68, 9080);}
				if (m_gambler.slot3 == 1){this.AddItem(223, 77, 15786);}
				if (m_gambler.slot3 == 2){this.AddItem(220, 77, 13900);}
				if (m_gambler.slot3 == 3){this.AddItem(219, 59, 12252);}
				if (m_gambler.slot3 == 4){this.AddItem(217, 73, 11740);}
				if (m_gambler.slot3 == 5){this.AddItem(216, 64, 10903);}
				if (m_gambler.slot3 == 6){this.AddItem(216, 85, 4354);}
				if (m_gambler.slot3 == 7){this.AddItem(216, 88, 15122);}
				if (m_gambler.slot3 == 8){this.AddItem(219, 73, 6257);}

				if (m_gambler.slot4 == 0){this.AddItem(289, 68, 9080);}
				if (m_gambler.slot4 == 1){this.AddItem(293, 77, 15786);}
				if (m_gambler.slot4 == 2){this.AddItem(290, 77, 13900);}
				if (m_gambler.slot4 == 3){this.AddItem(289, 59, 12252);}
				if (m_gambler.slot4 == 4){this.AddItem(287, 73, 11740);}
				if (m_gambler.slot4 == 5){this.AddItem(286, 64, 10903);}
				if (m_gambler.slot4 == 6){this.AddItem(286, 85, 4354);}
				if (m_gambler.slot4 == 7){this.AddItem(286, 88, 15122);}
				if (m_gambler.slot4 == 8){this.AddItem(289, 73, 6257);}

				if (m_gambler.slot5 == 0){this.AddItem(359, 68, 9080);}
				if (m_gambler.slot5 == 1){this.AddItem(363, 77, 15786);}
				if (m_gambler.slot5 == 2){this.AddItem(360, 77, 13900);}
				if (m_gambler.slot5 == 3){this.AddItem(359, 59, 12252);}
				if (m_gambler.slot5 == 4){this.AddItem(357, 73, 11740);}
				if (m_gambler.slot5 == 5){this.AddItem(356, 64, 10903);}
				if (m_gambler.slot5 == 6){this.AddItem(356, 85, 4354);}
				if (m_gambler.slot5 == 7){this.AddItem(356, 88, 15122);}
				if (m_gambler.slot5 == 8){this.AddItem(359, 73, 6257);}

				this.AddImage(153, 210, 2443);
				this.AddButton( 219, 205, 5600, 5604, 2, GumpButtonType.Reply, 0 );
				this.AddLabel(88, 211, 800, "your bet:");
				this.AddLabel( 170, 211, 0, ""+m_gambler.bet );
				this.AddButton( 219, 223, 5602, 5606, 3, GumpButtonType.Reply, 0 );

				this.AddImage(283, 220, 2445);
				this.AddLabel(313, 201, 1500, "Jackpot:");
				this.AddLabel( 300, 221, 0, ""+m_gambler.jackpot );

				this.AddButton( 77, 267, 2151, 2154, 1, GumpButtonType.Reply, 0 );
				this.AddLabel( 111, 271, 800, "Gamble" );

				this.AddButton( 79, 301, 2472, 2474, 4, GumpButtonType.Reply, 0 );
				this.AddLabel( 111, 305, 800, "Quit" );

				this.AddLabel( 215, 286, 600, "Gold in your backpack:" );
				this.AddLabel( 215, 301, 600, ""+m_gambler.goldbp );
				this.AddImage( 355, 273, 9811 );
				this.AddItem ( 352, 306, 3823 );

				this.AddImageTiled(437, 28, 133, 295, 9274);
				this.AddLabel( 447, 38, 800, "Winning table:" );

				this.AddLabel( 447, 68, 1500, "2 * 2 of a kind:" );
				this.AddLabel( 447, 83, 800, "your bet * 2" );

				this.AddLabel( 447, 113, 1500, "3 of a kind" );
				this.AddLabel( 447, 128, 800, "your bet * 3" );

				this.AddLabel( 447, 158, 1500, "2 & 3 of a kind" );
				this.AddLabel( 447, 173, 800, "your bet * 5" );

				this.AddLabel( 447, 203, 1500, "4 of a kind" );
				this.AddLabel( 447, 218, 800, "your bet * 25" );

				this.AddLabel( 447, 248, 1500, "5 of a kind" );
				this.AddLabel( 447, 263, 800, "your bet * 100" );
				this.AddLabel( 447, 278, 800, "& jackpot" );
				this.AddLabel( 447, 293, 800, "& 1 rare decoitem" );
			}
			
			public override void OnResponse( NetState state, RelayInfo info )  
			{
				Mobile from = state.Mobile;
				
				switch ( info.ButtonID ) 
				{
					case 1:
					{
						m_gambler.gamble(from, m_gambler.bet);
						m_mobile.SendGump( new GambleGump( m_gambler, m_mobile ) );
						break;
					}
					case 2:
					{
						m_gambler.bet += 100;
						if (m_gambler.bet > 1000){m_gambler.bet = 500;}
						m_mobile.SendGump( new GambleGump( m_gambler, m_mobile ) );
						break;
					}
					case 3:
					{
						m_gambler.bet -= 100;
						if (m_gambler.bet < 500){m_gambler.bet = 1000;}
						m_mobile.SendGump( new GambleGump( m_gambler, m_mobile ) );
						break;
					}
					case 4: 
					{
						m_gambler.bet = 500;
						m_gambler.slot1 = 3;
						m_gambler.slot2 = 3;
						m_gambler.slot3 = 3;
						m_gambler.slot4 = 3;
						m_gambler.slot5 = 3;
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

			writer.Write( (int) jackpot );
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					jackpot = reader.ReadInt();
					break;
				}
			}
		}
	}
}
