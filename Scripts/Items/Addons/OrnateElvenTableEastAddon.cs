using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class OrnateElvenTableEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new OrnateElvenTableEastDeed(); } }

		[Constructable]
		public OrnateElvenTableEastAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public OrnateElvenTableEastAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x308F ), 0, 1, 0 );
			AddComponent( new AddonComponent( 0x3090 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x3091 ), 0, -1, 0 );
			Hue = hue;
		}

		public OrnateElvenTableEastAddon( Serial serial ) : base( serial )
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

	[CraftItemID( 0x2DE2 )]
	public class OrnateElvenTableEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new OrnateElvenTableEastAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1073384; } } // ornate table (east)

		[Constructable]
		public OrnateElvenTableEastDeed()
		{
		}

		public OrnateElvenTableEastDeed( Serial serial ) : base( serial )
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