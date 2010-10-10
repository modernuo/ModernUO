using System;
using Server;

namespace Server.Items
{
	public class ElvenDresserEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenDresserEastDeed(); } }

		[Constructable]
		public ElvenDresserEastAddon()
		{
			AddComponent( new AddonComponent( 0x30E4 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x30E3 ), 0, -1, 0 );
		}

		public ElvenDresserEastAddon( Serial serial ) : base( serial )
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

	public class ElvenDresserEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenDresserEastAddon(); } }
		public override int LabelNumber{ get{ return 1073388; } } // elven dresser (east)

		[Constructable]
		public ElvenDresserEastDeed()
		{
		}

		public ElvenDresserEastDeed( Serial serial ) : base( serial )
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