using System;
using Server;

namespace Server.Items
{
	public class AndricSatchel : Backpack
	{
		[Constructable]
		public AndricSatchel()
		{
			Hue = Utility.RandomBrightHue();
			DropItem( new Feather( 10 ) );
			DropItem( new FletcherTools() );
		}

		public AndricSatchel( Serial serial )
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
