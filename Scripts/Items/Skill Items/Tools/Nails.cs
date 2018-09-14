using Server.Engines.Craft;

namespace Server.Items
{
	[Flippable( 0x102E, 0x102F )]
	public class Nails : BaseTool
	{
		public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;

		[Constructible]
		public Nails() : base( 0x102E )
		{
			Weight = 2.0;
		}

		[Constructible]
		public Nails( int uses ) : base( uses, 0x102C )
		{
			Weight = 2.0;
		}

		public Nails( Serial serial ) : base( serial )
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
