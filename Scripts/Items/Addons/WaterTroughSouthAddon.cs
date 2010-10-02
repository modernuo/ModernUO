using System;
using Server;

namespace Server.Items
{
	public class WaterTroughSouthAddon : BaseAddon, IWaterSource
	{
		public override BaseAddonDeed Deed{ get{ return new WaterTroughSouthDeed(); } }

		[Constructable]
		public WaterTroughSouthAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public WaterTroughSouthAddon( int hue )
		{
			AddComponent( new AddonComponent( 0xB43 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0xB44 ), 1, 0, 0 );
			Hue = hue;
		}

		public WaterTroughSouthAddon( Serial serial ) : base( serial )
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

	public class WaterTroughSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new WaterTroughSouthAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1044350; } } // water trough (south)

		[Constructable]
		public WaterTroughSouthDeed()
		{
		}

		public WaterTroughSouthDeed( Serial serial ) : base( serial )
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