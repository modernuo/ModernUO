using System;
using Server;

namespace Server.Items
{
	public class ElvenLoveseatSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenLoveseatSouthDeed(); } }

		[Constructable]
		public ElvenLoveseatSouthAddon()
		{
			AddComponent( new AddonComponent( 0x308A ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x308B ), 0, -1, 0 );
		}

		public ElvenLoveseatSouthAddon( Serial serial ) : base( serial )
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

	public class ElvenLoveseatSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenLoveseatSouthAddon(); } }
		public override int LabelNumber{ get{ return 1072867; } } // elven loveseat (south)

		[Constructable]
		public ElvenLoveseatSouthDeed()
		{
		}

		public ElvenLoveseatSouthDeed( Serial serial ) : base( serial )
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