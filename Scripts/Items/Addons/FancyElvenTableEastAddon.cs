using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class FancyElvenTableEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new FancyElvenTableEastDeed(); } }

		[Constructable]
		public FancyElvenTableEastAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public FancyElvenTableEastAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x3095 ), 0, 1, 0 );
			AddComponent( new AddonComponent( 0x3096 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x3097 ), 0, -1, 0 );
			Hue = hue;
		}

		public FancyElvenTableEastAddon( Serial serial ) : base( serial )
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

	[CraftItemID( 0x2DE8 )]
	public class FancyElvenTableEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new FancyElvenTableEastAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1073386; } } // hardwood table (east)

		[Constructable]
		public FancyElvenTableEastDeed()
		{
		}

		public FancyElvenTableEastDeed( Serial serial ) : base( serial )
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