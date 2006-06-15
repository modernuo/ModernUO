using System;
using Server;

namespace Server.Items
{
	public class ElvenForgeAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenForgeDeed(); } }

		[Constructable]
		public ElvenForgeAddon()
		{
			AddComponent( new AddonComponent( 0x2DD8 ), 0, 0, 0 );
		}

		public ElvenForgeAddon( Serial serial ) : base( serial )
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

	public class ElvenForgeDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenForgeAddon(); } }
		public override int LabelNumber{ get{ return 1072875; } } // squirrel statue (east)

		[Constructable]
		public ElvenForgeDeed()
		{
		}

		public ElvenForgeDeed( Serial serial ) : base( serial )
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