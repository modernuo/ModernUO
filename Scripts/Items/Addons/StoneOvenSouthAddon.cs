using System;
using Server;

namespace Server.Items
{
	public class StoneOvenSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new StoneOvenSouthDeed(); } }

		[Constructable]
		public StoneOvenSouthAddon()
		{
			AddComponent( new AddonComponent( 0x931 ), -1, 0, 0 );
			AddComponent( new AddonComponent( 0x930 ), 0, 0, 0 );
		}

		public StoneOvenSouthAddon( Serial serial ) : base( serial )
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

	public class StoneOvenSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new StoneOvenSouthAddon(); } }
		public override int LabelNumber{ get{ return 1044346; } } // stone oven (south)

		[Constructable]
		public StoneOvenSouthDeed()
		{
		}

		public StoneOvenSouthDeed( Serial serial ) : base( serial )
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