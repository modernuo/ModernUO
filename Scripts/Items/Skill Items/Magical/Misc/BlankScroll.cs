namespace Server.Items
{
	public class BlankScroll : Item, ICommodity
	{
		[Constructible]
		public BlankScroll() : this( 1 )
		{
		}

		[Constructible]
		public BlankScroll( int amount ) : base( 0xEF3 )
		{
			Stackable = true;
			Weight = 1.0;
			Amount = amount;
		}

		int ICommodity.DescriptionNumber => LabelNumber;
		bool ICommodity.IsDeedable => (Core.ML);

		public BlankScroll( Serial serial ) : base( serial )
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