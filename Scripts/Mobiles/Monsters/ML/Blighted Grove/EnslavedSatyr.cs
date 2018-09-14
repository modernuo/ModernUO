namespace Server.Mobiles
{
	public class EnslavedSatyr : Satyr
	{
		public override string CorpseName => "an enslaved satyr corpse";
		public override string DefaultName => "an enslaved satyr";

		[Constructible]
		public EnslavedSatyr()
		{
		}

		/*
		// TODO: uncomment once added
		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			if ( Utility.RandomDouble() < 0.1 )
				c.DropItem( new ParrotItem() );
		}
		*/

		public EnslavedSatyr( Serial serial )
			: base( serial )
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
