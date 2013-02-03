using System;
using Server;

namespace Server.Items
{
	public class PixieLeg : Item
	{
		public override int LabelNumber{ get{ return 1074613; } } // Pixie Leg

		[Constructable]
		public PixieLeg() : this( 1 )
		{
		}

		[Constructable]
		public PixieLeg( int amount ) : base( 0x1608 )
		{
			LootType = LootType.Blessed;
			Hue = 0x1C2;
			Stackable = true;
			Amount = amount;
		}

		public PixieLeg( Serial serial ) : base( serial )
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
