using System;
using Server;

namespace Server.Items
{
	public class FragmentOfAMap : Item
	{
		public override int LabelNumber{ get{ return 1074533; } } // Fragment of a Map

		[Constructable]
		public FragmentOfAMap() : base( 0x14ED )
		{
			LootType = LootType.Blessed;
		}

		public FragmentOfAMap( Serial serial ) : base( serial )
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
