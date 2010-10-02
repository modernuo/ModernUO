using System;
using Server;

namespace Server.Items
{
	public class ElvenBedSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenBedSouthDeed(); } }

		[Constructable]
		public ElvenBedSouthAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public ElvenBedSouthAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x3050 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x3051 ), 0, -1, 0 );
			Hue = hue;
		}

		public ElvenBedSouthAddon( Serial serial ) : base( serial )
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

	public class ElvenBedSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenBedSouthAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1072860; } } // elven bed (south)

		[Constructable]
		public ElvenBedSouthDeed()
		{
		}

		public ElvenBedSouthDeed( Serial serial ) : base( serial )
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