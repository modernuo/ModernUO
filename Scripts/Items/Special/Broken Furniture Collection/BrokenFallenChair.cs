using System;

namespace Server.Items
{
	[Flipable( 0xC19, 0xC1a )]
	public class BrokenFallenChairComponent : AddonComponent
	{
		public override int LabelNumber { get { return 1076264; } } // Broken Fallen Chair

		public BrokenFallenChairComponent() : base( 0xC19 )
		{
		}

		public BrokenFallenChairComponent( Serial serial ) : base( serial )
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

	public class BrokenFallenChairAddon : BaseAddon
	{
		public override BaseAddonDeed Deed { get { return new BrokenFallenChairDeed(); } }

		[Constructable]
		public BrokenFallenChairAddon() : base()
		{
			AddComponent( new BrokenFallenChairComponent(), 0, 0, 0 );
		}

		public BrokenFallenChairAddon( Serial serial ) : base( serial )
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

	public class BrokenFallenChairDeed : BaseAddonDeed
	{
		public override BaseAddon Addon { get { return new BrokenFallenChairAddon(); } }
		public override int LabelNumber { get { return 1076264; } } // Broken Fallen Chair

		[Constructable]
		public BrokenFallenChairDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public BrokenFallenChairDeed( Serial serial ) : base( serial )
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
