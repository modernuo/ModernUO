using System;

namespace Server.Items
{
	[Flipable( 0x3DAA, 0x3DA9 )]
	public class SuitOfGoldArmorComponent : AddonComponent
	{
		public override int LabelNumber { get { return 1076265; } } // Suit of Gold Armor

		public SuitOfGoldArmorComponent() : base( 0x3DAA )
		{
		}

		public SuitOfGoldArmorComponent( Serial serial ) : base( serial )
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

	public class SuitOfGoldArmorAddon : BaseAddon
	{
		public override BaseAddonDeed Deed { get { return new SuitOfGoldArmorDeed(); } }

		[Constructable]
		public SuitOfGoldArmorAddon() : base()
		{
			AddComponent( new SuitOfGoldArmorComponent(), 0, 0, 0 );
		}

		public SuitOfGoldArmorAddon( Serial serial ) : base( serial )
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

	public class SuitOfGoldArmorDeed : BaseAddonDeed
	{
		public override BaseAddon Addon { get { return new SuitOfGoldArmorAddon(); } }
		public override int LabelNumber { get { return 1076265; } } // Suit of Gold Armor

		[Constructable]
		public SuitOfGoldArmorDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public SuitOfGoldArmorDeed( Serial serial ) : base( serial )
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
