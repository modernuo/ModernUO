using System;

namespace Server.Items
{
	[Flipable( 0xC24, 0xC25 )]
	public class BrokenChestOfDrawersComponent : AddonComponent
	{
		public override int LabelNumber { get { return 1076261; } } // Broken Chest of Drawers

		public BrokenChestOfDrawersComponent() : base( 0xC24 )
		{
		}

		public BrokenChestOfDrawersComponent( Serial serial ) : base( serial )
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

	public class BrokenChestOfDrawersAddon : BaseAddon
	{
		public override BaseAddonDeed Deed { get { return new BrokenChestOfDrawersDeed(); } }

		[Constructable]
		public BrokenChestOfDrawersAddon() : base()
		{
			AddComponent( new BrokenChestOfDrawersComponent(), 0, 0, 0 );
		}

		public BrokenChestOfDrawersAddon( Serial serial ) : base( serial )
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

	public class BrokenChestOfDrawersDeed : BaseAddonDeed
	{
		public override BaseAddon Addon { get { return new BrokenChestOfDrawersAddon(); } }
		public override int LabelNumber { get { return 1076261; } } // Broken Chest of Drawers

		[Constructable]
		public BrokenChestOfDrawersDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public BrokenChestOfDrawersDeed( Serial serial ) : base( serial )
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
