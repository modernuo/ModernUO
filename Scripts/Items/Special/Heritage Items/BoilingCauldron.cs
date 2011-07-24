using System;

namespace Server.Items
{
	[Flipable( 0x2068, 0x207A )]
	public class BoilingCauldronAddon : BaseAddonContainer
	{
		public override BaseAddonContainerDeed Deed { get { return new BoilingCauldronDeed(); } }
		public override int LabelNumber { get { return 1076267; } } // Boiling Cauldron
		public override int DefaultGumpID { get { return 0x9; } }
		public override int DefaultDropSound { get { return 0x42; } }

		[Constructable]
		public BoilingCauldronAddon() : base( 0x2068 )
		{
			AddComponent( new LocalizedContainerComponent( 0xFAC, 1076267 ), 0, 0, 0 );
			AddComponent( new LocalizedContainerComponent( 0x970, 1076267 ), 0, 0, 8 );
		}

		public BoilingCauldronAddon( Serial serial ) : base( serial )
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

	public class BoilingCauldronDeed : BaseAddonContainerDeed
	{
		public override BaseAddonContainer Addon { get { return new BoilingCauldronAddon(); } }
		public override int LabelNumber { get { return 1076267; } } // Boiling Cauldron

		[Constructable]
		public BoilingCauldronDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public BoilingCauldronDeed( Serial serial ) : base( serial )
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
