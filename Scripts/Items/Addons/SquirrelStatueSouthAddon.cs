using System;
using Server;

namespace Server.Items
{
	public class SquirrelStatueSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new SquirrelStatueSouthDeed(); } }

		[Constructable]
		public SquirrelStatueSouthAddon()
		{
			AddComponent( new AddonComponent( 0x2D11 ), 0, 0, 0 );
		}

		public SquirrelStatueSouthAddon( Serial serial ) : base( serial )
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

	public class SquirrelStatueSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new SquirrelStatueSouthAddon(); } }
		public override int LabelNumber{ get{ return 1072884; } } // squirrel statue (south)

		[Constructable]
		public SquirrelStatueSouthDeed()
		{
		}

		public SquirrelStatueSouthDeed( Serial serial ) : base( serial )
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