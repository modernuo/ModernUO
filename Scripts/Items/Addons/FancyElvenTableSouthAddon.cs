using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class FancyElvenTableSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new FancyElvenTableSouthDeed(); } }

		[Constructable]
		public FancyElvenTableSouthAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public FancyElvenTableSouthAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x3094 ), -1, 0, 0 );
			AddComponent( new AddonComponent( 0x3093 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x3092 ), 1, 0, 0 );
			Hue = hue;
		}

		public FancyElvenTableSouthAddon( Serial serial ) : base( serial )
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

	[CraftItemID( 0x2DE7 )]
	public class FancyElvenTableSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new FancyElvenTableSouthAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1073385; } } // hardwood table (south)

		[Constructable]
		public FancyElvenTableSouthDeed()
		{
		}

		public FancyElvenTableSouthDeed( Serial serial ) : base( serial )
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