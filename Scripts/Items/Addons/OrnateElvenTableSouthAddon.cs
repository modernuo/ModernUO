using System;
using Server;

namespace Server.Items
{
	public class OrnateElvenTableSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new OrnateElvenTableSouthDeed(); } }

		[Constructable]
		public OrnateElvenTableSouthAddon()
		{
			AddComponent( new AddonComponent( 0x308F ), 0, 1, 0 );
			AddComponent( new AddonComponent( 0x3090 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x3091 ), 0, -1, 0 );
		}

		public OrnateElvenTableSouthAddon( Serial serial ) : base( serial )
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

	public class OrnateElvenTableSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new OrnateElvenTableSouthAddon(); } }
		public override int LabelNumber{ get{ return 1072869; } } // ornate table (south)

		[Constructable]
		public OrnateElvenTableSouthDeed()
		{
		}

		public OrnateElvenTableSouthDeed( Serial serial ) : base( serial )
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