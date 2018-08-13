using System;

namespace Server.Items
{
	[Flippable( 0x1E34, 0x1E35 )]
	public class ScarecrowComponent : AddonComponent
	{
		public override int LabelNumber => 1076608; // Scarecrow

		public ScarecrowComponent() : base( 0x1E34 )
		{
		}

		public ScarecrowComponent( Serial serial ) : base( serial )
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

	public class ScarecrowAddon : BaseAddon
	{
		public override BaseAddonDeed Deed => new ScarecrowDeed();

		[Constructible]
		public ScarecrowAddon()
		{
			AddComponent( new ScarecrowComponent(), 0, 0, 0 );
		}

		public ScarecrowAddon( Serial serial ) : base( serial )
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

	public class ScarecrowDeed : BaseAddonDeed
	{
		public override BaseAddon Addon => new ScarecrowAddon();
		public override int LabelNumber => 1076608; // Scarecrow

		[Constructible]
		public ScarecrowDeed()
		{
			LootType = LootType.Blessed;
		}

		public ScarecrowDeed( Serial serial ) : base( serial )
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
