using System;
using Server;

namespace Server.Items
{
	public class Globe : Item
	{
		[Constructible]
		public Globe() : base( 0x1047 ) // It isn't flippable
		{
			Weight = 3.0;
		}

		public Globe( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}