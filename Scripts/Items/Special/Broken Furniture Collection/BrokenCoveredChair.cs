using System;

namespace Server.Items
{
	[Flippable( 0xC17, 0xC18 )]
	public class BrokenCoveredChairComponent : AddonComponent
	{
		public override int LabelNumber => 1076257; // Broken Covered Chair

		public BrokenCoveredChairComponent() : base( 0xC17 )
		{
		}

		public BrokenCoveredChairComponent( Serial serial ) : base( serial )
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

	public class BrokenCoveredChairAddon : BaseAddon
	{
		public override BaseAddonDeed Deed => new BrokenCoveredChairDeed();

		[Constructible]
		public BrokenCoveredChairAddon()
		{
			AddComponent( new BrokenCoveredChairComponent(), 0, 0, 0 );
		}

		public BrokenCoveredChairAddon( Serial serial ) : base( serial )
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

	public class BrokenCoveredChairDeed : BaseAddonDeed
	{
		public override BaseAddon Addon => new BrokenCoveredChairAddon();
		public override int LabelNumber => 1076257; // Broken Covered Chair

		[Constructible]
		public BrokenCoveredChairDeed()
		{
			LootType = LootType.Blessed;
		}

		public BrokenCoveredChairDeed( Serial serial ) : base( serial )
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
