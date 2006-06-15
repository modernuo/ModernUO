using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class SewingKit : BaseTool
	{
		public override CraftSystem CraftSystem{ get{ return DefTailoring.CraftSystem; } }

		[Constructable]
		public SewingKit() : base( 0xF9D )
		{
			Weight = 2.0;
		}

		[Constructable]
		public SewingKit( int uses ) : base( uses, 0xF9D )
		{
			Weight = 2.0;
		}

		public SewingKit( Serial serial ) : base( serial )
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