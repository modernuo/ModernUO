using System;
using Server.Items;

namespace Server.Items
{
	public class StuddedHiroSode : BaseArmor
	{
		public override int BasePhysicalResistance => 2;
		public override int BaseFireResistance => 4;
		public override int BaseColdResistance => 3;
		public override int BasePoisonResistance => 3;
		public override int BaseEnergyResistance => 4;

		public override int InitMinHits => 45;
		public override int InitMaxHits => 55;

		public override int AosStrReq => 30;
		public override int OldStrReq => 30;

		public override int ArmorBase => 3;

		public override ArmorMaterialType MaterialType => ArmorMaterialType.Studded;
		public override CraftResource DefaultResource => CraftResource.RegularLeather;

		[Constructible]
		public StuddedHiroSode() : base( 0x277F )
		{
			Weight = 1.0;
		}

		public StuddedHiroSode( Serial serial ) : base( serial )
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
