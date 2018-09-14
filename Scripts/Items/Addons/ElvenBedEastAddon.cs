namespace Server.Items
{
	public class ElvenBedEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed => new ElvenBedEastDeed();

		[Constructible]
		public ElvenBedEastAddon()
		{
			AddComponent( new AddonComponent( 0x304D ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x304C ), 1, 0, 0 );
		}

		public ElvenBedEastAddon( Serial serial ) : base( serial )
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

	public class ElvenBedEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon => new ElvenBedEastAddon();
		public override int LabelNumber => 1072861; // elven bed (east)

		[Constructible]
		public ElvenBedEastDeed()
		{
		}

		public ElvenBedEastDeed( Serial serial ) : base( serial )
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
