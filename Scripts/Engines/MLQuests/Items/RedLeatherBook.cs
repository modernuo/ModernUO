using System;
using Server;

namespace Server.Items
{
	public class RedLeatherBook : BlueBook
	{
		[Constructable]
		public RedLeatherBook()
		{
			Hue = 0x485;
		}

		public RedLeatherBook( Serial serial )
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
