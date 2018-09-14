namespace Server.Items
{
	public class FierySpellblade : ElvenSpellblade
	{
		public override int LabelNumber => 1073515; // fiery spellblade

		[Constructible]
		public FierySpellblade()
		{
			WeaponAttributes.ResistFireBonus = 5;
		}

		public FierySpellblade( Serial serial ) : base( serial )
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
