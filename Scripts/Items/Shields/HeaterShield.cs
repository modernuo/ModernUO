namespace Server.Items
{
	public class HeaterShield : BaseShield
	{
		public override int BasePhysicalResistance => 0;
		public override int BaseFireResistance => 1;
		public override int BaseColdResistance => 0;
		public override int BasePoisonResistance => 0;
		public override int BaseEnergyResistance => 0;

		public override int InitMinHits => 50;
		public override int InitMaxHits => 65;

		public override int AosStrReq => 90;

		public override int ArmorBase => 23;

		[Constructible]
		public HeaterShield() : base( 0x1B76 )
		{
			Weight = 8.0;
		}

		public HeaterShield( Serial serial ) : base(serial)
		{
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );//version
		}
	}
}
