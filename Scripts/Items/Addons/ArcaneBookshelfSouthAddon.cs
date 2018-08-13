using System;
using Server;

namespace Server.Items
{
	public class ArcaneBookshelfSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed => new ArcaneBookshelfSouthDeed();

		[Constructible]
		public ArcaneBookshelfSouthAddon()
		{
			AddComponent( new AddonComponent( 0x3087 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x3086 ), 0, 1, 0 );
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
		public override BaseAddon Addon => new ArcaneBookshelfSouthAddon();
		public override int LabelNumber => 1072871; // arcane bookshelf (south)

		[Constructible]
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
