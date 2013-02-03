using System;
using Server;

namespace Server.Items
{
	public class AsandosSatchel : Backpack
	{
		[Constructable]
		public AsandosSatchel()
		{
			Hue = Utility.RandomBrightHue();
			DropItem( new SackFlour() );
			DropItem( new Skillet() );
		}

		public AsandosSatchel( Serial serial )
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
