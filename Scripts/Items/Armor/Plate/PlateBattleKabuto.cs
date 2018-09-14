namespace Server.Items
{
	public class PlateBattleKabuto : BaseArmor
	{
		public override int BasePhysicalResistance => 6;
		public override int BaseFireResistance => 2;
		public override int BaseColdResistance => 2;
		public override int BasePoisonResistance => 2;
		public override int BaseEnergyResistance => 3;

		public override int InitMinHits => 60;
		public override int InitMaxHits => 65;

		public override int AosStrReq => 70;
		public override int OldStrReq => 70;

		public override int ArmorBase => 3;

		public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

		[Constructible]
		public PlateBattleKabuto() : base( 0x2785 )
		{
			Weight = 6.0;
		}

		public PlateBattleKabuto( Serial serial ) : base( serial )
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
