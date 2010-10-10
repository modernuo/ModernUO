using System;
using Server;

namespace Server.Items
{
	public class ParrotPerchAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ParrotPerchDeed(); } }

		[Constructable]
		public ParrotPerchAddon()
		{
			AddComponent( new AddonComponent( 0x2FF4 ), 0, 0, 0 );
		}

		public ParrotPerchAddon( Serial serial ) : base( serial )
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

	public class ParrotPerchDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ParrotPerchAddon(); } }
		public override int LabelNumber{ get{ return 1072617; } } // parrot perch

		[Constructable]
		public ParrotPerchDeed()
		{
		}

		public ParrotPerchDeed( Serial serial ) : base( serial )
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