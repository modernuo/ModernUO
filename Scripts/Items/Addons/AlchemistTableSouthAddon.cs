using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class AlchemistTableSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new AlchemistTableSouthDeed(); } }

		[Constructable]
		public AlchemistTableSouthAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public AlchemistTableSouthAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x2DD4 ), 0, 0, 0 );
			Hue = hue;
		}

		public AlchemistTableSouthAddon( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	[CraftItemID( 0x2DD4 )]
	public class AlchemistTableSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new AlchemistTableSouthAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1073396; } } // alchemist table (south)

		[Constructable]
		public AlchemistTableSouthDeed()
		{
		}

		public AlchemistTableSouthDeed( Serial serial ) : base( serial )
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