namespace Server.Items
{
	public class CherryBlossomTrunkAddon : BaseAddon
	{
		public override BaseAddonDeed Deed => new CherryBlossomTrunkDeed();

		[Constructible]
		public CherryBlossomTrunkAddon()
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
		public override BaseAddon Addon => new CherryBlossomTrunkAddon();
		public override int LabelNumber => 1076784; // Cherry Blossom Trunk

		[Constructible]
		public CherryBlossomTrunkDeed()
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
