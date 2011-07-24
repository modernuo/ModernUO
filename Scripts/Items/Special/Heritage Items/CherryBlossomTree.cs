using System;

namespace Server.Items
{
	public class CherryBlossomTreeAddon : BaseAddon
	{
		public override BaseAddonDeed Deed { get { return new CherryBlossomTreeDeed(); } }

		[Constructable]
		public CherryBlossomTreeAddon() : base()
		{
			AddComponent( new LocalizedAddonComponent( 0x26EE, 1076268 ), 0, 0, 0 );
			AddComponent( new LocalizedAddonComponent( 0x3122, 1076268 ), 0, 0, 0 );
		}

		public CherryBlossomTreeAddon( Serial serial ) : base( serial )
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

	public class CherryBlossomTreeDeed : BaseAddonDeed
	{
		public override BaseAddon Addon { get { return new CherryBlossomTreeAddon(); } }
		public override int LabelNumber { get { return 1076268; } } // Cherry Blossom Tree

		[Constructable]
		public CherryBlossomTreeDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public CherryBlossomTreeDeed( Serial serial ) : base( serial )
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
