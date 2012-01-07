using System;
using Server;

namespace Server.Items
{
	public class TaintedSeeds : Item
	{
		public override int LabelNumber{ get{ return 1074233; } } // Tainted Seeds

		[Constructable]
		public TaintedSeeds() : base( 0xDFA )
		{
			LootType = LootType.Blessed;
			Hue = 0x48; // TODO check
		}

		public TaintedSeeds( Serial serial ) : base( serial )
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

