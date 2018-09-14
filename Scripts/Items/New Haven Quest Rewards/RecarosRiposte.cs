namespace Server.Items
{
	public class RecarosRiposte : WarFork
	{
		public override int LabelNumber => 1078195; // Recaro's Riposte

		[Constructible]
		public RecarosRiposte()
		{
			LootType = LootType.Blessed;

			Attributes.AttackChance = 5;
			Attributes.WeaponSpeed = 10;
			Attributes.WeaponDamage = 25;
		}

		public RecarosRiposte( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}
