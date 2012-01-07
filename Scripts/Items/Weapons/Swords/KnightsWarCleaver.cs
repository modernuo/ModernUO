using System;
using Server.Items;

namespace Server.Items
{
	public class KnightsWarCleaver : WarCleaver
	{
		public override int LabelNumber{ get{ return 1073525; } } // knight's war cleaver

		[Constructable]
		public KnightsWarCleaver()
		{
			Attributes.RegenHits = 3;
		}

		public KnightsWarCleaver( Serial serial ) : base( serial )
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
