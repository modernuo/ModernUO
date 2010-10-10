using System;
using Server;

namespace Server.Items
{
	public class ElvenDresserSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenDresserSouthDeed(); } }

		[Constructable]
		public ElvenDresserSouthAddon()
		{
			AddComponent( new AddonComponent( 0x30E5 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x30E6 ), 1, 0, 0 );
		}

		public ElvenDresserSouthAddon( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class ElvenDresserSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenDresserSouthAddon(); } }
		public override int LabelNumber{ get{ return 1072864; } } // elven dresser (south)

		[Constructable]
		public ElvenDresserSouthDeed()
		{
		}

		public ElvenDresserSouthDeed( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}