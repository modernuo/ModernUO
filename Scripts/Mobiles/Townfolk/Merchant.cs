using System;
using Server;
using Server.Items;
using EDI=Server.Mobiles.EscortDestinationInfo;

namespace Server.Mobiles
{
	public class Merchant : BaseEscortable
	{
		[Constructable]
		public Merchant()
		{
			Title = "the merchant";
			SetSkill( SkillName.ItemID, 55.0, 78.0 );
			SetSkill( SkillName.ArmsLore, 55, 78 );
		}

		public override bool CanTeach { get { return true; } }
		public override bool ClickTitle { get { return false; } } // Do not display 'the merchant' when single-clicking

		private static int GetRandomHue()
		{
			switch( Utility.Random( 6 ) )
			{
				default:
				case 0: return 0;
				case 1: return Utility.RandomBlueHue();
				case 2: return Utility.RandomGreenHue();
				case 3: return Utility.RandomRedHue();
				case 4: return Utility.RandomYellowHue();
				case 5: return Utility.RandomNeutralHue();
			}

		}

		public override void InitOutfit()
		{
			if( Female )
				AddItem( new PlainDress() );
			else
				AddItem( new Shirt( GetRandomHue() ) );

			int lowHue = GetRandomHue();

			AddItem( new ThighBoots() );

			if( Female )
				AddItem( new FancyDress( lowHue ) );
			else
				AddItem( new FancyShirt( lowHue ) );
			AddItem( new LongPants( lowHue ) );

			if( !Female )
				AddItem( new BodySash( lowHue ) );



			//if ( !Female )
			//AddItem( new Longsword() );

			Utility.AssignRandomHair( this );

			PackGold( 200, 250 );
		}

		public Merchant( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}