using System;
using System.Collections.Generic;

namespace Server.Items.Holiday
{
	public class RockingHorse : Item
	{
		public RockingHorse() : base ( 0x4214 )
		{
			LootType = LootType.Blessed;

			Weight = 30;
		}

		public RockingHorse( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( ( int )0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
