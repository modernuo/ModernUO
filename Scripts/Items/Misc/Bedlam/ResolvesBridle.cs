namespace Server.Items
{
	public class ResolvesBridle : Item
	{
		public override int LabelNumber => 1074761; // Resolve's Bridle

		[Constructible]
		public ResolvesBridle() : base( 0x1374 )
		{
		}

		public ResolvesBridle( Serial serial ) : base( serial )
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

