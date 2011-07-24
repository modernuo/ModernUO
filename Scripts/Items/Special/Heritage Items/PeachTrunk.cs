using System;

namespace Server.Items
{
	public class PeachTrunkAddon : BaseAddon
	{
		public override BaseAddonDeed Deed { get { return new PeachTrunkDeed(); } }

		[Constructable]
		public PeachTrunkAddon() : base()
		{
			AddComponent( new LocalizedAddonComponent( 0xD9C, 1076786 ), 0, 0, 0 );
		}

		public PeachTrunkAddon( Serial serial ) : base( serial )
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

	public class PeachTrunkDeed : BaseAddonDeed
	{
		public override BaseAddon Addon { get { return new PeachTrunkAddon(); } }
		public override int LabelNumber { get { return 1076786; } } // Peach Trunk

		[Constructable]
		public PeachTrunkDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public PeachTrunkDeed( Serial serial ) : base( serial )
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
