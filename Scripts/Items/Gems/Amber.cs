namespace Server.Items
{
	public class Amber : Item
	{
		public override double DefaultWeight => 0.1;

		[Constructible]
		public Amber() : this( 1 )
		{
		}

		[Constructible]
		public Amber( int amount ) : base( 0xF25 )
		{
			Stackable = true;
			Amount = amount;
		}

		public Amber( Serial serial ) : base( serial )
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