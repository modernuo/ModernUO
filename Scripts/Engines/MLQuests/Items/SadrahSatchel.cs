using System;
using Server;

namespace Server.Items
{
	public class SadrahSatchel : Backpack
	{
		[Constructable]
		public SadrahSatchel()
		{
			Hue = Utility.RandomBrightHue();
			DropItem( new Bloodmoss( 10 ) );
			DropItem( new MortarPestle() );
		}

		public SadrahSatchel( Serial serial )
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
