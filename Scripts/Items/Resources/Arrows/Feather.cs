namespace Server.Items
{
	public class Feather : Item, ICommodity
	{
		int ICommodity.DescriptionNumber => LabelNumber;
		bool ICommodity.IsDeedable => true;

		public override double DefaultWeight => 0.1;

		[Constructible]
		public Feather() : this( 1 )
		{
		}

		[Constructible]
		public Feather( int amount ) : base( 0x1BD1 )
		{
			Stackable = true;
			Amount = amount;
		}

		public Feather( Serial serial ) : base( serial )
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