using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class ElvenLoveseatEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenLoveseatEastDeed(); } }

		[Constructable]
		public ElvenLoveseatEastAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public ElvenLoveseatEastAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x308A ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x308B ), 0, -1, 0 );
			Hue = hue;
		}

		public ElvenLoveseatEastAddon( Serial serial ) : base( serial )
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

	[CraftItemID( 0x2DE0 )]
	public class ElvenLoveseatEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenLoveseatEastAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1073372; } } // elven loveseat (east)

		[Constructable]
		public ElvenLoveseatEastDeed()
		{
		}

		public ElvenLoveseatEastDeed( Serial serial ) : base( serial )
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