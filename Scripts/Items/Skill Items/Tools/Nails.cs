using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	[Flipable( 0x102E, 0x102F )]
	public class Nails : BaseTool
	{
		public override CraftSystem CraftSystem{ get{ return DefCarpentry.CraftSystem; } }

		[Constructable]
		public Nails() : base( 0x102E )
		{
			Weight = 2.0;
		}

		[Constructable]
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