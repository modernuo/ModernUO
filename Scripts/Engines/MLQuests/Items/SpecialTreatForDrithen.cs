using System;
using Server;

namespace Server.Items
{
	public class SpecialTreatForDrithen : Item
	{
		public override int LabelNumber{ get{ return 1074517; } } // Special Treat for Drithen

		[Constructable]
		public SpecialTreatForDrithen() : base( 0x21B )
		{
			LootType = LootType.Blessed;
			Hue = 0x489;
		}

		public SpecialTreatForDrithen( Serial serial ) : base( serial )
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
