using System;
using Server;

namespace Server.Items.Holiday
{
	public class PaintedEvilClownMask : BasePaintedMask
	{
		public override string MaskName { get { return "Evil Clown Mask"; } }

		[Constructable]
		public PaintedEvilClownMask( )
			: base( 0x4a90 )
		{
		}

		public PaintedEvilClownMask( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( ( int )0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
