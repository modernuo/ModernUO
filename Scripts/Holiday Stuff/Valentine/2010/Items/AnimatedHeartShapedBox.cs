using System;
using Server;

namespace Server.Items
{
	[FlipableAttribute( 0x49CC, 0x49D0 )]
	public class AnimatedHeartShapedBox : HeartShapedBox
	{
		[Constructable]
		public AnimatedHeartShapedBox()
		{
			ItemID = 0x49CC;
		}

		public AnimatedHeartShapedBox( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
