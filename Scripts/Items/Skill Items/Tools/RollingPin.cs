using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class RollingPin : BaseTool
	{
		public override CraftSystem CraftSystem{ get{ return DefCooking.CraftSystem; } }

		[Constructable]
		public RollingPin() : base( 0x1043 )
		{
			Weight = 1.0;
		}

		[Constructable]
		public RollingPin( int uses ) : base( uses, 0x1043 )
		{
			Weight = 1.0;
		}

		public RollingPin( Serial serial ) : base( serial )
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