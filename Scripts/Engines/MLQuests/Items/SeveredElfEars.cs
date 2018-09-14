namespace Server.Items
{
	[Flippable(0x312D, 0x312E)]
	public class SeveredElfEars : Item
	{
		[Constructible]
		public SeveredElfEars() : this( 1 )
		{
		}

		[Constructible]
		public SeveredElfEars( int amount ) : base( Utility.RandomList( 0x312D, 0x312E ) )
		{
			Stackable = true;
			Amount = amount;
		}

		public SeveredElfEars( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // Version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
