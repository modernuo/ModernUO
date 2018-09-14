namespace Server.Engines.BulkOrders
{
	public class BOBFilter
	{
		public bool IsDefault => ( Type == 0 && Quality == 0 && Material == 0 && Quantity == 0 );

		public void Clear()
		{
			Type = 0;
			Quality = 0;
			Material = 0;
			Quantity = 0;
		}

		public int Type { get; set; }

		public int Quality { get; set; }

		public int Material { get; set; }

		public int Quantity { get; set; }

		public BOBFilter()
		{
		}

		public BOBFilter( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 1:
				{
					Type = reader.ReadEncodedInt();
					Quality = reader.ReadEncodedInt();
					Material = reader.ReadEncodedInt();
					Quantity = reader.ReadEncodedInt();

					break;
				}
			}
		}

		public void Serialize( GenericWriter writer )
		{
			if ( IsDefault )
			{
				writer.WriteEncodedInt( 0 ); // version
			}
			else
			{
				writer.WriteEncodedInt( 1 ); // version

				writer.WriteEncodedInt( Type );
				writer.WriteEncodedInt( Quality );
				writer.WriteEncodedInt( Material );
				writer.WriteEncodedInt( Quantity );
			}
		}
	}
}