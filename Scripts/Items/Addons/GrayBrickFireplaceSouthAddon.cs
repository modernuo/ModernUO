using System;
using Server;

namespace Server.Items
{
	public class GrayBrickFireplaceSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new GrayBrickFireplaceSouthDeed(); } }

		[Constructable]
		public GrayBrickFireplaceSouthAddon()
		{
			AddComponent( new AddonComponent( 0x94B ), -1, 0, 0 );
			AddComponent( new AddonComponent( 0x945 ), 0, 0, 0 );
		}

		public GrayBrickFireplaceSouthAddon( Serial serial ) : base( serial )
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

	public class GrayBrickFireplaceSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new GrayBrickFireplaceSouthAddon(); } }
		public override int LabelNumber{ get{ return 1061847; } } // grey brick fireplace (south)

		[Constructable]
		public GrayBrickFireplaceSouthDeed()
		{
		}

		public GrayBrickFireplaceSouthDeed( Serial serial ) : base( serial )
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