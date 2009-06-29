using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	public class SkullPole : Item
	{
		[Constructable]
		public SkullPole() : base( 0x2204 )
		{
			Weight = 5;
		}

		public SkullPole( Serial serial ) : base( serial )
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
