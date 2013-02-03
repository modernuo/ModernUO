using System;
using Server;

namespace Server.Items
{
	public class MirrorOfPurification : Item
	{
		public override int LabelNumber{ get{ return 1075304; } } // Mirror of Purification

		[Constructable]
		public MirrorOfPurification() : base( 0x1008 )
		{
			LootType = LootType.Blessed;
			Hue = 0x530;
		}

		public MirrorOfPurification( Serial serial ) : base( serial )
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
