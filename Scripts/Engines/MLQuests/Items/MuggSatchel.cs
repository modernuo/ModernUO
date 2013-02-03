using System;
using Server;

namespace Server.Items
{
	public class MuggSatchel : Backpack
	{
		[Constructable]
		public MuggSatchel()
		{
			Hue = Utility.RandomBrightHue();
			DropItem( new Pickaxe() );
			DropItem( new Pickaxe() );
		}

		public MuggSatchel( Serial serial )
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
