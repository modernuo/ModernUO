using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	public class Pier : Item
	{
		[Constructable]
		public Pier() : base( 0x03AE )
		{
		}

		public Pier( Serial serial ) : base( serial )
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
