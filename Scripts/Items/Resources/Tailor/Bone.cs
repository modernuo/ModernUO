namespace Server.Items
{
	public class Bone : Item, ICommodity
	{
		int ICommodity.DescriptionNumber => LabelNumber;
		bool ICommodity.IsDeedable => true;

		[Constructible]
		public Bone() : this( 1 )
		{
		}

		[Constructible]
		public Bone( int amount ) : base( 0xf7e )
		{
			Stackable = true;
			Amount = amount;
			Weight = 1.0;
		}

		public Bone( Serial serial ) : base( serial )
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