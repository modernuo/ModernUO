using System;
using Server;

namespace Server.Items
{
	public class ABauble : Item
	{
		public override int LabelNumber{ get{ return 1073137; } } // A bauble

		[Constructable]
		public ABauble() : base( 0x23B )
		{
			LootType = LootType.Blessed;
		}

		public ABauble( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // Version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
