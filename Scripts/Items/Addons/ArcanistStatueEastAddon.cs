using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class ArcanistStatueEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ArcanistStatueEastDeed(); } }

		[Constructable]
		public ArcanistStatueEastAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public ArcanistStatueEastAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x2D0E ), 0, 0, 0 );
			Hue = hue;
		}

		public ArcanistStatueEastAddon( Serial serial ) : base( serial )
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

	[CraftItemID( 0x2D0E )]
	public class ArcanistStatueEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ArcanistStatueEastAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1072886; } } // arcanist statue (east)

		[Constructable]
		public ArcanistStatueEastDeed()
		{
		}

		public ArcanistStatueEastDeed( Serial serial ) : base( serial )
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