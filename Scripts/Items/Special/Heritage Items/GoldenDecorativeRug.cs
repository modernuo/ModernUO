using System;

namespace Server.Items
{
	public class GoldenDecorativeRugAddon : BaseAddon
	{
		public override BaseAddonDeed Deed { get { return new GoldenDecorativeRugDeed(); } }

		[Constructable]
		public GoldenDecorativeRugAddon() : base()
		{
			AddComponent( new LocalizedAddonComponent( 0xADB, 1076586 ), 1, 1, 0 );
			AddComponent( new LocalizedAddonComponent( 0xADC, 1076586 ), -1, -1, 0 );
			AddComponent( new LocalizedAddonComponent( 0xADD, 1076586 ), -1, 1, 0 );
			AddComponent( new LocalizedAddonComponent( 0xADE, 1076586 ), 1, -1, 0 );
			AddComponent( new LocalizedAddonComponent( 0xADF, 1076586 ), -1, 0, 0 );
			AddComponent( new LocalizedAddonComponent( 0xAE0, 1076586 ), 0, -1, 0 );
			AddComponent( new LocalizedAddonComponent( 0xAE1, 1076586 ), 1, 0, 0 );
			AddComponent( new LocalizedAddonComponent( 0xAE2, 1076586 ), 0, 1, 0 );
			AddComponent( new LocalizedAddonComponent( 0xADA, 1076586 ), 0, 0, 0 );
		}

		public GoldenDecorativeRugAddon( Serial serial ) : base( serial )
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

	public class GoldenDecorativeRugDeed : BaseAddonDeed
	{
		public override BaseAddon Addon { get { return new GoldenDecorativeRugAddon(); } }
		public override int LabelNumber { get { return 1076586; } } // Golden decorative rug

		[Constructable]
		public GoldenDecorativeRugDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public GoldenDecorativeRugDeed( Serial serial ) : base( serial )
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
