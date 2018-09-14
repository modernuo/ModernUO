namespace Server.Items
{
	public class MinotaurHedge : Item
	{
		public override string DefaultName => "minotaur hedge";

		[Constructible]
		public MinotaurHedge() : base( Utility.Random( 3215, 4 ) )
		{
			Weight = 1.0;
		}

		public MinotaurHedge( Serial serial ) : base( serial )
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

