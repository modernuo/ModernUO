namespace Server.Items
{
	public class TableWithOrangeClothAddon : BaseAddon
	{
		public override BaseAddonDeed Deed => new TableWithOrangeClothDeed();

		[Constructible]
		public TableWithOrangeClothAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0x118E, 1076278 ), 0, 0, 0 );
		}

		public TableWithOrangeClothAddon( Serial serial ) : base( serial )
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

	public class TableWithOrangeClothDeed : BaseAddonDeed
	{
		public override BaseAddon Addon => new TableWithOrangeClothAddon();
		public override int LabelNumber => 1076278; // Table With An Orange Tablecloth

		[Constructible]
		public TableWithOrangeClothDeed()
		{
			LootType = LootType.Blessed;
		}

		public TableWithOrangeClothDeed( Serial serial ) : base( serial )
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
