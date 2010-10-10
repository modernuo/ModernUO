using System;
using Server;

namespace Server.Items
{
	public class SquirrelStatueEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new SquirrelStatueEastDeed(); } }

		[Constructable]
		public SquirrelStatueEastAddon()
		{
			AddComponent( new AddonComponent( 0x2D10 ), 0, 0, 0 );
		}

		public SquirrelStatueEastAddon( Serial serial ) : base( serial )
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

	public class SquirrelStatueEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new SquirrelStatueEastAddon(); } }
		public override int LabelNumber{ get{ return 1073398; } } // squirrel statue (east)

		[Constructable]
		public SquirrelStatueEastDeed()
		{
		}

		public SquirrelStatueEastDeed( Serial serial ) : base( serial )
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