using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	[Flipable( 0x102C, 0x102D )]
	public class MouldingPlane : BaseTool
	{
		public override CraftSystem CraftSystem{ get{ return DefCarpentry.CraftSystem; } }

		[Constructable]
		public MouldingPlane() : base( 0x102C )
		{
			Weight = 2.0;
		}

		[Constructable]
		public MouldingPlane( int uses ) : base( uses, 0x102C )
		{
			Weight = 2.0;
		}

		public MouldingPlane( Serial serial ) : base( serial )
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