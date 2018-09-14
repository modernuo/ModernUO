namespace Server.Items
{
	public class AppleTrunkAddon : BaseAddon
	{
		public override BaseAddonDeed Deed => new AppleTrunkDeed();

		[Constructible]
		public AppleTrunkAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0xD98, 1076785 ), 0, 0, 0 );
		}

		public AppleTrunkAddon( Serial serial ) : base( serial )
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

	public class AppleTrunkDeed : BaseAddonDeed
	{
		public override BaseAddon Addon => new AppleTrunkAddon();
		public override int LabelNumber => 1076785; // Apple Trunk

		[Constructible]
		public AppleTrunkDeed()
		{
			LootType = LootType.Blessed;
		}

		public AppleTrunkDeed( Serial serial ) : base( serial )
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
