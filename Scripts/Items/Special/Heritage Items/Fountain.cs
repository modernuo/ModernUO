namespace Server.Items
{
	public class FountainAddon : StoneFountainAddon
	{
		public override BaseAddonDeed Deed => new FountainDeed();

		[Constructible]
		public FountainAddon()
		{
		}

		public FountainAddon( Serial serial ) : base( serial )
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

	public class FountainDeed : BaseAddonDeed
	{
		public override BaseAddon Addon => new FountainAddon();
		public override int LabelNumber => 1076283; // Fountain

		[Constructible]
		public FountainDeed()
		{
			LootType = LootType.Blessed;
		}

		public FountainDeed( Serial serial ) : base( serial )
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
