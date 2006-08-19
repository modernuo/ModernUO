using System;
using Server;

namespace Server.Items
{
	public class ElvenWashBasinEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenWashBasinEastDeed(); } }

		[Constructable]
		public ElvenWashBasinEastAddon()
		{
			AddComponent( new AddonComponent( 0x30DF ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x30E0 ), 0, 1, 0 );
		}

		public ElvenWashBasinEastAddon( Serial serial ) : base( serial )
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

	public class ElvenWashBasinEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenWashBasinEastAddon(); } }
		public override int LabelNumber{ get{ return 1073387; } } // elven wash basin (east)

		[Constructable]
		public ElvenWashBasinEastDeed()
		{
		}

		public ElvenWashBasinEastDeed( Serial serial ) : base( serial )
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