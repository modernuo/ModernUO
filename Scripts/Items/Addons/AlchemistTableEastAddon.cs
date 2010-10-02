using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class AlchemistTableEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new AlchemistTableEastDeed(); } }

		[Constructable]
		public AlchemistTableEastAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public AlchemistTableEastAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x2DD3 ), 0, 0, 0 );
			Hue = hue;
		}

		public AlchemistTableEastAddon( Serial serial ) : base( serial )
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

	[CraftItemID( 0x2DD3 )]
	public class AlchemistTableEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new AlchemistTableEastAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1073397; } } // alchemist table (east)

		[Constructable]
		public AlchemistTableEastDeed()
		{
		}

		public AlchemistTableEastDeed( Serial serial ) : base( serial )
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