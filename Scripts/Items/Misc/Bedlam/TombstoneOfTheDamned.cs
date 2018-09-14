namespace Server.Items
{
	public class TombstoneOfTheDamned : Item
	{
		public override int LabelNumber => 1072123; // Tombstone of the Damned

		[Constructible]
		public TombstoneOfTheDamned() : base( Utility.RandomMinMax( 0xED7, 0xEDE ) )
		{
		}

		public TombstoneOfTheDamned( Serial serial ) : base( serial )
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

