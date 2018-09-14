namespace Server.Items
{
	public class ProtectorsEssence : Item
	{
		public override int LabelNumber => 1073159; // Protector's Essence

		[Constructible]
		public ProtectorsEssence() : base( 0x23F )
		{
		}

		public ProtectorsEssence( Serial serial ) : base( serial )
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

