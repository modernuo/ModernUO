using System;
using Server;

namespace Server.Items
{
	public class PlagueBeastGland : Item
	{
		public override string DefaultName => "a healthy gland";

		[Constructible]
		public PlagueBeastGland() : base( 0x1CEF )
		{
			Weight = 1.0;
			Hue = 0x6;
		}

		public PlagueBeastGland( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}
