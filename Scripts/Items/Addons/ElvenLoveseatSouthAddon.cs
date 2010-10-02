using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class ElvenLoveseatSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenLoveseatSouthDeed(); } }

		[Constructable]
		public ElvenLoveseatSouthAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public ElvenLoveseatSouthAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x3089 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x3088 ), 1, 0, 0 );
			Hue = hue;
		}

		public ElvenLoveseatSouthAddon( Serial serial ) : base( serial )
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

	[CraftItemID( 0x2DDF )]
	public class ElvenLoveseatSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenLoveseatSouthAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1072867; } } // elven loveseat (south)

		[Constructable]
		public ElvenLoveseatSouthDeed()
		{
		}

		public ElvenLoveseatSouthDeed( Serial serial ) : base( serial )
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