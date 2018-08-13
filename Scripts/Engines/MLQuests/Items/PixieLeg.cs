using System;
using Server;

namespace Server.Items
{
	public class PixieLeg : ChickenLeg
	{
		public override int LabelNumber => 1074613; // Pixie Leg

		[Constructible]
		public PixieLeg() : this( 1 )
		{
		}

		[Constructible]
		public PixieLeg( int amount ) : base( amount )
		{
			LootType = LootType.Blessed;
			Hue = 0x1C2;
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
