using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class ElvenStoveEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenStoveEastDeed(); } }

		[Constructable]
		public ElvenStoveEastAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public ElvenStoveEastAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x2DDB ), 0, 0, 0 );
			Hue = hue;
		}

		public ElvenStoveEastAddon( Serial serial ) : base( serial )
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

	[CraftItemID( 0x2DDB )]
	public class ElvenStoveEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenStoveEastAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1073395; } } // elven oven (east)

		[Constructable]
		public ElvenStoveEastDeed()
		{
		}

		public ElvenStoveEastDeed( Serial serial ) : base( serial )
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