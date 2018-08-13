using System;
using Server;

namespace Server.Items
{
	public class PolarBearMask : BearMask
	{
		public override int LabelNumber => 1070637;

		public override int BasePhysicalResistance => 15;
		public override int BaseColdResistance => 21;

		public override int InitMinHits => 255;
		public override int InitMaxHits => 255;

		[Constructible]
		public PolarBearMask()
		{
			Hue = 0x481;

			ClothingAttributes.SelfRepair = 3;

			Attributes.RegenHits = 2;
			Attributes.NightSight = 1;
		}

		public PolarBearMask( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 2 );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version < 2 )
			{
				Resistances.Physical = 0;
				Resistances.Cold = 0;
			}

			if ( Attributes.NightSight == 0 )
				Attributes.NightSight = 1;
		}
	}
}
