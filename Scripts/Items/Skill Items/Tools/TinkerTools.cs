using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	[Flipable( 0x1EB8, 0x1EB9 )]
	public class TinkerTools : BaseTool
	{
		public override CraftSystem CraftSystem{ get{ return DefTinkering.CraftSystem; } }

		[Constructable]
		public TinkerTools() : base( 0x1EB8 )
		{
			Weight = 1.0;
		}

		[Constructable]
		public TinkerTools( int uses ) : base( uses, 0x1EB8 )
		{
			Weight = 1.0;
		}

		public TinkerTools( Serial serial ) : base( serial )
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