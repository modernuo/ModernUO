using System;
using Server;

namespace Server.Items
{
	public class IcyHeart : Item
	{
		public override int LabelNumber{ get{ return 1073162; } } // Icy Heart

		[Constructable]
		public IcyHeart() : base( 0x24B )
		{
		}

		public IcyHeart( Serial serial ) : base( serial )
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

