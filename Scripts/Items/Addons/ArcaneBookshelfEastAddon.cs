using System;
using Server;

namespace Server.Items
{
	public class ArcaneBookshelfEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed => new ArcaneBookshelfEastDeed();

		[Constructible]
		public ArcaneBookshelfEastAddon()
		{
			AddComponent( new AddonComponent( 0x3084 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x3085 ), -1, 0, 0 );
		}

		public ArcaneBookshelfEastAddon( Serial serial ) : base( serial )
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

	public class ArcaneBookshelfEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon => new ArcaneBookshelfEastAddon();
		public override int LabelNumber => 1073371; // arcane bookshelf (east)

		[Constructible]
		public ArcaneBookshelfEastDeed()
		{
		}

		public ArcaneBookshelfEastDeed( Serial serial ) : base( serial )
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
