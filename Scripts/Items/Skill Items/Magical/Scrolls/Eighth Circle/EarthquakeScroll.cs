namespace Server.Items
{
	public class EarthquakeScroll : SpellScroll
	{
		[Constructible]
		public EarthquakeScroll() : this( 1 )
		{
		}

		[Constructible]
		public EarthquakeScroll( int amount ) : base( 56, 0x1F65, amount )
		{
		}

		public EarthquakeScroll( Serial serial ) : base( serial )
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