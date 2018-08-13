using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	[Flippable( 0x102C, 0x102D )]
	public class MouldingPlane : BaseTool
	{
		public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;

		[Constructible]
		public MouldingPlane() : base( 0x102C )
		{
			Weight = 2.0;
		}

		[Constructible]
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
