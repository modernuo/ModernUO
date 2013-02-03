using System;
using Server;

namespace Server.Items
{
	public class HargroveSatchel : Backpack
	{
		[Constructable]
		public HargroveSatchel()
		{
			Hue = Utility.RandomBrightHue();
			DropItem( new Gold( 15 ) );
			DropItem( new Hatchet() );
		}

		public HargroveSatchel( Serial serial )
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
