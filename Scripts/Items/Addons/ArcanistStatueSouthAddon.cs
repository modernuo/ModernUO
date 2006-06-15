using System;
using Server;

namespace Server.Items
{
	public class ArcanistStatueSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ArcanistStatueSouthDeed(); } }

		[Constructable]
		public ArcanistStatueSouthAddon()
		{
			AddComponent( new AddonComponent( 0x2D0F ), 0, 0, 0 );
		}

		public ArcanistStatueSouthAddon( Serial serial ) : base( serial )
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

	public class ArcanistStatueSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ArcanistStatueSouthAddon(); } }
		public override int LabelNumber{ get{ return 1072885; } } // arcanist statue (south)

		[Constructable]
		public ArcanistStatueSouthDeed()
		{
		}

		public ArcanistStatueSouthDeed( Serial serial ) : base( serial )
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