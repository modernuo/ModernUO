using System;
using Server;

namespace Server.Items
{
	public class FoldedSteelGlasses : ElvenGlasses
	{
		public override int LabelNumber => 1073380; //Folded Steel Reading Glasses

		public override int BasePhysicalResistance => 20;
		public override int BaseFireResistance => 10;
		public override int BaseColdResistance => 10;
		public override int BasePoisonResistance => 10;
		public override int BaseEnergyResistance => 10;

		public override int InitMinHits => 255;
		public override int InitMaxHits => 255;

		[Constructible]
		public FoldedSteelGlasses()
		{
			Attributes.BonusStr = 8;
			Attributes.NightSight = 1;
			Attributes.DefendChance = 15;

			Hue = 0x47E;
		}
		public FoldedSteelGlasses( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 1 );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			if ( version == 0 && Hue == 0 )
				Hue = 0x47E;
		}
	}
}
