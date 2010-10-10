using System;
using Server;

namespace Server.Items
{
	public class LargeForgeSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new LargeForgeSouthDeed(); } }

		[Constructable]
		public LargeForgeSouthAddon()
		{
			AddComponent( new ForgeComponent( 0x197A ), 0, 0, 0 );
			AddComponent( new ForgeComponent( 0x197E ), 1, 0, 0 );
			AddComponent( new ForgeComponent( 0x19A2 ), 2, 0, 0 );
			AddComponent( new ForgeComponent( 0x199E ), 3, 0, 0 );
		}

		public LargeForgeSouthAddon( Serial serial ) : base( serial )
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

	public class LargeForgeSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new LargeForgeSouthAddon(); } }
		public override int LabelNumber{ get{ return 1044332; } } // large forge (south)

		[Constructable]
		public LargeForgeSouthDeed()
		{
		}

		public LargeForgeSouthDeed( Serial serial ) : base( serial )
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