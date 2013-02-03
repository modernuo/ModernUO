using System;
using Server;

namespace Server.Items
{
	public class AnOldNecklace : Necklace
	{
		public override int LabelNumber{ get{ return 1075525; } } // an old necklace

		[Constructable]
		public AnOldNecklace() : base()
		{
			Hue = 0x222;
		}

		public AnOldNecklace( Serial serial ) : base( serial )
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
