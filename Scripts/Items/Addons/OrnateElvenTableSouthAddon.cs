using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class OrnateElvenTableSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new OrnateElvenTableSouthDeed(); } }

		[Constructable]
		public OrnateElvenTableSouthAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public OrnateElvenTableSouthAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x308E ), -1, 0, 0 );
			AddComponent( new AddonComponent( 0x308D ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x308C ), 1, 0, 0 );
			Hue = hue;
		}

		public OrnateElvenTableSouthAddon( Serial serial ) : base( serial )
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

	[CraftItemID( 0x2DE1 )]
	public class OrnateElvenTableSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new OrnateElvenTableSouthAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1072869; } } // ornate table (south)

		[Constructable]
		public OrnateElvenTableSouthDeed()
		{
		}

		public OrnateElvenTableSouthDeed( Serial serial ) : base( serial )
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