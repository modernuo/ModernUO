using System;

namespace Server.Items
{
	public class CherryBlossomTrunkAddon : BaseAddon
	{
		public override BaseAddonDeed Deed { get { return new CherryBlossomTrunkDeed(); } }

		[Constructable]
		public CherryBlossomTrunkAddon() : base()
		{
			AddComponent( new LocalizedAddonComponent( 0x26EE, 1076784 ), 0, 0, 0 );
		}

		public CherryBlossomTrunkAddon( Serial serial ) : base( serial )
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

	public class CherryBlossomTrunkDeed : BaseAddonDeed
	{
		public override BaseAddon Addon { get { return new CherryBlossomTrunkAddon(); } }
		public override int LabelNumber { get { return 1076784; } } // Cherry Blossom Trunk

		[Constructable]
		public CherryBlossomTrunkDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public CherryBlossomTrunkDeed( Serial serial ) : base( serial )
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
