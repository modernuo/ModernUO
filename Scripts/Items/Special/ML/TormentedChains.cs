namespace Server.Items
{
	public class TormentedChains : Item
	{
		public override string DefaultName => "chains of the tormented";

		[Constructible]
		public TormentedChains() : base( Utility.Random( 6663, 2 ) )
		{
			Weight = 1.0;
		}

		public TormentedChains( Serial serial ) : base( serial )
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

