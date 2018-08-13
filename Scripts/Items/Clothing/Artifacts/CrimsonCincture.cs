using System;

namespace Server.Items
{
    public class CrimsonCincture : HalfApron, ITokunoDyable
	{
		public override int LabelNumber => 1075043; // Crimson Cincture

		[Constructible]
		public CrimsonCincture()
		{
			Hue = 0x485;

			Attributes.BonusDex = 5;
			Attributes.BonusHits = 10;
			Attributes.RegenHits = 2;
		}

		public CrimsonCincture( Serial serial ) : base( serial )
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

