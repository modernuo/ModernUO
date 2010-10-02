using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class ElvenWashBasinSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenWashBasinSouthDeed(); } }

		[Constructable]
		public ElvenWashBasinSouthAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public ElvenWashBasinSouthAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x30E1 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x30E2 ), 1, 0, 0 );
			Hue = hue;
		}

		public ElvenWashBasinSouthAddon( Serial serial ) : base( serial )
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

	[CraftItemID( 0x2D0B )]
	public class ElvenWashBasinSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenWashBasinSouthAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1072865; } } // elven wash basin (south)

		[Constructable]
		public ElvenWashBasinSouthDeed()
		{
		}

		public ElvenWashBasinSouthDeed( Serial serial ) : base( serial )
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