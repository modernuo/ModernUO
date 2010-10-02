using System;
using Server;

namespace Server.Items
{
	public class ArcaneBookshelfSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ArcaneBookshelfSouthDeed(); } }

		[Constructable]
		public ArcaneBookshelfSouthAddon() : this( 0 )
		{
		}
		
		[Constructable]
		public ArcaneBookshelfSouthAddon( int hue )
		{
			AddComponent( new AddonComponent( 0x3087 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x3086 ), 0, 1, 0 );
			Hue = hue;
		}

		public ArcaneBookshelfSouthAddon( Serial serial ) : base( serial )
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

	public class ArcaneBookshelfSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ArcaneBookshelfSouthAddon( Hue ); } }
		public override int LabelNumber{ get{ return 1072871; } } // arcane bookshelf (south)

		[Constructable]
		public ArcaneBookshelfSouthDeed()
		{
		}

		public ArcaneBookshelfSouthDeed( Serial serial ) : base( serial )
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