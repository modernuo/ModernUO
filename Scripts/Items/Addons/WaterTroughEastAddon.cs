using System;
using Server;

namespace Server.Items
{
	public class WaterTroughEastAddon : BaseAddon, IWaterSource
	{
		public override BaseAddonDeed Deed{ get{ return new WaterTroughEastDeed(); } }

		[Constructable]
		public WaterTroughEastAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public WaterTroughEastAddon( int hue )
		{
			AddComponent( new AddonComponent( 0xB41 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0xB42 ), 0, 1, 0 );
			Hue = hue;
		}

		public WaterTroughEastAddon( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

		public int Quantity
		{
			get{ return 500; }
			set{}
		}
	}

	public class WaterTroughEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new WaterTroughEastAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1044349; } } // water trough (east)

		[Constructable]
		public WaterTroughEastDeed()
		{
		}

		public WaterTroughEastDeed( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}