using System;
using Server;

namespace Server.Items
{
	public class LargeStoneTableEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new LargeStoneTableEastDeed(); } }

		public override bool RetainDeedHue{ get{ return true; } }

		[Constructable]
		public LargeStoneTableEastAddon() : this( 0 )
		{
		}

		[Constructable]
		public LargeStoneTableEastAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x1202 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x1203 ), 0, 1, 0 );
			AddComponent( new AddonComponent( 0x1201 ), 0, 2, 0 );
			Hue = hue;
		}

		public LargeStoneTableEastAddon( Serial serial ) : base( serial )
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

	public class LargeStoneTableEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new LargeStoneTableEastAddon( this.Hue ); } }
		public override int LabelNumber{ get{ return 1044511; } } // large stone table (east)

		[Constructable]
		public LargeStoneTableEastDeed()
		{
		}

		public LargeStoneTableEastDeed( Serial serial ) : base( serial )
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