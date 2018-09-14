namespace Server.Items
{
	public class HeavyPlateJingasa : BaseArmor
	{
		public override int BasePhysicalResistance => 7;
		public override int BaseFireResistance => 2;
		public override int BaseColdResistance => 2;
		public override int BasePoisonResistance => 2;
		public override int BaseEnergyResistance => 2;

		public override int InitMinHits => 50;
		public override int InitMaxHits => 70;

		public override int AosStrReq => 55;
		public override int OldStrReq => 55;

		public override int ArmorBase => 4;

		public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

		[Constructible]
		public HeavyPlateJingasa() : base( 0x2777 )
		{
			Weight = 5.0;
		}

		public HeavyPlateJingasa( Serial serial ) : base( serial )
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
