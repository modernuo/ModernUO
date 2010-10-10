using System;
using Server;

namespace Server.Items
{
	public class PentagramAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new PentagramDeed(); } }

		[Constructable]
		public PentagramAddon()
		{
			AddComponent( new AddonComponent( 0xFE7 ), -1, -1, 0 );
			AddComponent( new AddonComponent( 0xFE8 ),  0, -1, 0 );
			AddComponent( new AddonComponent( 0xFEB ),  1, -1, 0 );
			AddComponent( new AddonComponent( 0xFE6 ), -1,  0, 0 );
			AddComponent( new AddonComponent( 0xFEA ),  0,  0, 0 );
			AddComponent( new AddonComponent( 0xFEE ),  1,  0, 0 );
			AddComponent( new AddonComponent( 0xFE9 ), -1,  1, 0 );
			AddComponent( new AddonComponent( 0xFEC ),  0,  1, 0 );
			AddComponent( new AddonComponent( 0xFED ),  1,  1, 0 );
		}

		public PentagramAddon( Serial serial ) : base( serial )
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

	public class PentagramDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new PentagramAddon(); } }
		public override int LabelNumber{ get{ return 1044328; } } // pentagram

		[Constructable]
		public PentagramDeed()
		{
		}

		public PentagramDeed( Serial serial ) : base( serial )
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