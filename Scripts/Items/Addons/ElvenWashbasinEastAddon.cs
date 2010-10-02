using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class ElvenWashBasinEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenWashBasinEastDeed(); } }

		[Constructable]
		public ElvenWashBasinEastAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public ElvenWashBasinEastAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x30DF ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x30E0 ), 0, 1, 0 );
			Hue = hue;
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

	[CraftItemID( 0x2D0C )]
	public class ElvenWashBasinEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenWashBasinEastAddon( Hue ); } }
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