using System;
using Server.Items;

namespace Server.Items
{
	public class GamblingStone : Item
	{
		private int m_GamblePot = 2500;

		[CommandProperty( AccessLevel.GameMaster )]
		public int GamblePot
		{
			get
			{
				return m_GamblePot;
			}
			set
			{
				m_GamblePot = value;
				InvalidateProperties();
			}
		}

		public override string DefaultName
		{
			get { return "a gambling stone"; }
		}

		[Constructable]
		public GamblingStone() : base( 0xED4 )
		{
			Movable = false;
			Hue = 0x56;
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( "Jackpot: {0}gp", m_GamblePot );
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );
			base.LabelTo( from, "Jackpot: {0}gp", m_GamblePot );
		}

		public override void OnDoubleClick( Mobile from )
		{
			Container pack = from.Backpack;

			if ( pack != null && pack.ConsumeTotal( typeof( Gold ), 250 ) )
			{
				m_GamblePot += 150;
				InvalidateProperties();

				int roll = Utility.Random( 1200 );

				if ( roll == 0 ) // Jackpot
				{
					from.SendMessage( 0x35, "You win the {0}gp jackpot!", m_GamblePot );
					from.AddToBackpack( new BankCheck( m_GamblePot ) );

					m_GamblePot = 2500;
				}
				else if ( roll <= 20 ) // Chance for a regbag
				{
					from.SendMessage( 0x35, "You win a bag of reagents!" );
					from.AddToBackpack( new BagOfReagents( 50 ) );
				}
				else if ( roll <= 40 ) // Chance for gold
				{
					from.SendMessage( 0x35, "You win 1500gp!" );
					from.AddToBackpack( new BankCheck( 1500 ) );
				}
				else if ( roll <= 100 ) // Another chance for gold
				{
					from.SendMessage( 0x35, "You win 1000gp!" );
					from.AddToBackpack( new BankCheck( 1000 ) );
				}
				else // Loser!
				{
					from.SendMessage( 0x22, "You lose!" );
				}
			}
			else
			{
				from.SendMessage( 0x22, "You need at least 250gp in your backpack to use this." );
			}
		}
    
		public GamblingStone( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (int) m_GamblePot );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_GamblePot = reader.ReadInt();

					break;
				}
			}
		}
	}
}