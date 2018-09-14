namespace Server.Items
{
	public class StarSapphire : Item
	{
		public override double DefaultWeight => 0.1;

		[Constructible]
		public StarSapphire() : this( 1 )
		{
		}

		[Constructible]
		public StarSapphire( int amount ) : base( 0xF21 )
		{
			Stackable = true;
			Amount = amount;
		}

		public StarSapphire( Serial serial ) : base( serial )
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