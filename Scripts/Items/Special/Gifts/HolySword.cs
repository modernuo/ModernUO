namespace Server.Items
{
	public class HolySword : Longsword
	{
		public override int LabelNumber => 1062921; // The Holy Sword

		public override int InitMinHits => 255;
		public override int InitMaxHits => 255;

		[Constructible]
		public HolySword()
		{
			Hue = 0x482;
			LootType = LootType.Blessed;

			Slayer = SlayerName.Silver;

			Attributes.WeaponDamage = 40;
			WeaponAttributes.SelfRepair = 10;
			WeaponAttributes.LowerStatReq = 100;
			WeaponAttributes.UseBestSkill = 1;
		}

		public HolySword( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}
