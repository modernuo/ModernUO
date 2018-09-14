namespace Server.Items
{
	public class MassSleepScroll : SpellScroll
	{
		[Constructible]
		public MassSleepScroll()
			: this( 1 )
		{
		}

		[Constructible]
		public MassSleepScroll( int amount )
			: base( 686, 0x2DA7, amount )
		{
		}

		public MassSleepScroll( Serial serial )
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

			/*int version = */
			reader.ReadInt();
		}
	}
}
