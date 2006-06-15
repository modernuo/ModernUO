using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	[FlipableAttribute( 0x1022, 0x1023 )]
	public class FletcherTools : BaseTool
	{
		public override CraftSystem CraftSystem{ get{ return DefBowFletching.CraftSystem; } }

		[Constructable]
		public FletcherTools() : base( 0x1022 )
		{
			Weight = 2.0;
		}

		[Constructable]
		public FletcherTools( int uses ) : base( uses, 0x1022 )
		{
			Weight = 2.0;
		}

		public FletcherTools( Serial serial ) : base( serial )
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

			if ( Weight == 1.0 )
				Weight = 2.0;
		}
	}
}