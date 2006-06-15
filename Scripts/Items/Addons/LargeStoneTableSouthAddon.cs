using System;
using Server;

namespace Server.Items
{
	public class LargeStoneTableSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new LargeStoneTableSouthDeed(); } }

		public override bool RetainDeedHue{ get{ return true; } }

		[Constructable]
		public LargeStoneTableSouthAddon() : this( 0 )
		{
		}

		[Constructable]
		public LargeStoneTableSouthAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x1205 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x1206 ), 1, 0, 0 );
			AddComponent( new AddonComponent( 0x1204 ), 2, 0, 0 );
			Hue = hue;
		}

		public LargeStoneTableSouthAddon( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class LargeStoneTableSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new LargeStoneTableSouthAddon( this.Hue ); } }
		public override int LabelNumber{ get{ return 1044512; } } // large stone table (South)

		[Constructable]
		public LargeStoneTableSouthDeed()
		{
		}

		public LargeStoneTableSouthDeed( Serial serial ) : base( serial )
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