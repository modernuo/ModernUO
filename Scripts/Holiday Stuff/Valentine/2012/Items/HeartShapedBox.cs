using System;
using Server;

namespace Server.Items
{
	[FlipableAttribute( 0x49CA, 0x49CB )]
	public class HeartShapedBox : BaseContainer
	{
		public override int DefaultDropSound { get { return m_DropSound; } }

		[Constructable]
		public HeartShapedBox()
			: base( 0x49CA )
		{
		}

		public override bool OnDragDropInto( Mobile from, Item item, Point3D p )
		{
			PrepareSound( from );
			return base.OnDragDropInto( from, item, p );
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			PrepareSound( from );
			return base.OnDragDrop( from, dropped );
		}

		private static int m_DropSound;

		private static void PrepareSound( Mobile from )
		{
			m_DropSound = from.Female ? 0x430 : 0x320;
		}

		public HeartShapedBox( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
