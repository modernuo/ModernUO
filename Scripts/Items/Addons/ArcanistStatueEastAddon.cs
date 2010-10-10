using System;
using Server;

namespace Server.Items
{
	public class ArcanistStatueEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ArcanistStatueEastDeed(); } }

		[Constructable]
		public ArcanistStatueEastAddon()
		{
			AddComponent( new AddonComponent( 0x2D0E ), 0, 0, 0 );
		}

		public ArcanistStatueEastAddon( Serial serial ) : base( serial )
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

	public class ArcanistStatueEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ArcanistStatueEastAddon(); } }
		public override int LabelNumber{ get{ return 1072886; } } // arcanist statue (east)

		[Constructable]
		public ArcanistStatueEastDeed()
		{
		}

		public ArcanistStatueEastDeed( Serial serial ) : base( serial )
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