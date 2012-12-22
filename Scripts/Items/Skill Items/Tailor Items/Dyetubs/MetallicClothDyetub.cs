using System;

namespace Server.Items
{
	public class MetallicClothDyetub : DyeTub
	{
		public override int LabelNumber { get { return 1152920; } } // Metallic Cloth ... 

		public override bool MetallicHues { get { return true; } }

		[Constructable]
		public MetallicClothDyetub()
		{
			LootType = LootType.Blessed;
		}

		public MetallicClothDyetub( Serial serial )
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