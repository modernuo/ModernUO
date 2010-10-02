using System;
using Server;

namespace Server.Items
{
	public class SmallForgeAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new SmallForgeDeed(); } }

		[Constructable]
		public SmallForgeAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public SmallForgeAddon( int hue )
		{
			AddComponent( new ForgeComponent( 0xFB1 ), 0, 0, 0 );
			Hue = hue;
		}

		public SmallForgeAddon( Serial serial ) : base( serial )
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

	public class SmallForgeDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new SmallForgeAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1044330; } } // small forge

		[Constructable]
		public SmallForgeDeed()
		{
		}

		public SmallForgeDeed( Serial serial ) : base( serial )
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