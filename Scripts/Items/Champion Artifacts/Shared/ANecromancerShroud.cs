using System;
using Server;

namespace Server.Items
{
	public class ANecromancerShroud : Robe
	{
		public override int LabelNumber => 1094913; // A Necromancer Shroud [Replica]

		public override int BaseColdResistance => 5;

		public override int InitMinHits => 150;
		public override int InitMaxHits => 150;

		public override bool CanFortify => false;

		[Constructible]
		public ANecromancerShroud()
		{
			Hue = 0x455;
		}

		public ANecromancerShroud( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
