using System;
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
	public class PuzzleStatue : Item
	{
		[Constructable]
		public PuzzleStatue( int itemID ) : base( itemID )
		{
			Hue = 0x44E;
			Movable = false;
		}

		public PuzzleStatue( Serial serial ) : base( serial )
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
