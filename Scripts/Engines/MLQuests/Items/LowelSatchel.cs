using System;
using Server;

namespace Server.Items
{
	public class LowelSatchel : Backpack
	{
		[Constructable]
		public LowelSatchel()
		{
			Hue = Utility.RandomBrightHue();
			DropItem( new Board( 10 ) );
			DropItem( new DovetailSaw() );
		}

		public LowelSatchel( Serial serial )
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
