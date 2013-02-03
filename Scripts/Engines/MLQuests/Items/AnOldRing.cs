using System;
using Server;

namespace Server.Items
{
	public class AnOldRing : GoldRing
	{
		public override int LabelNumber{ get{ return 1075524; } } // an old ring

		[Constructable]
		public AnOldRing() : base()
		{
			Hue = 0x222;
		}

		public AnOldRing( Serial serial ) : base( serial )
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
