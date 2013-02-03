using System;
using Server;

namespace Server.Items
{
	public class NibbetSatchel : Backpack
	{
		[Constructable]
		public NibbetSatchel()
		{
			Hue = Utility.RandomBrightHue();
			DropItem( new TinkerTools() );

			switch ( Utility.Random( 10 ) )
			{
				case 0: DropItem( new Springs( 3 ) ); break;
				case 1: DropItem( new Axle( 3 ) ); break;
				case 2: DropItem( new Hinge( 3 ) ); break;
				case 3: DropItem( new Key() ); break;
				case 4: DropItem( new Scissors() ); break;
				case 5: DropItem( new BarrelTap( 3 ) ); break;
				case 6: DropItem( new BarrelHoops() ); break;
				case 7: DropItem( new Gears( 3 ) ); break;
				case 8: DropItem( new Lockpick( 3 ) ); break;
				case 9: DropItem( new ClockFrame( 3 ) ); break;

			}
		}

		public NibbetSatchel( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
