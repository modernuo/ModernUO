using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	public class ParrotPerchAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ParrotPerchDeed(); } }

		[Constructable]
		public ParrotPerchAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public ParrotPerchAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x2FB6 ), 0, 0, 0 );
			Hue = hue;
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

	[CraftItemID( 0x2FB6 )]
	public class ParrotPerchDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ParrotPerchAddon( Hue ); } }
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