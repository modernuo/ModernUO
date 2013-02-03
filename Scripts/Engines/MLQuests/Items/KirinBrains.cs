using System;
using Server;

namespace Server.Items
{
	public class KirinBrains : Item
	{
		public override int LabelNumber{ get{ return 1074612; } } // Ki-Rin Brains

		[Constructable]
		public KirinBrains() : base( 0x1CF0 )
		{
			LootType = LootType.Blessed;
			Hue = 0xD7;
		}

		public KirinBrains( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // Version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
