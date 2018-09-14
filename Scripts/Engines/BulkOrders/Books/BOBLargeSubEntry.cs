using System;

namespace Server.Engines.BulkOrders
{
	public class BOBLargeSubEntry
	{
		public Type ItemType { get; }

		public int AmountCur { get; }

		public int Number { get; }

		public int Graphic { get; }

		public BOBLargeSubEntry( LargeBulkEntry lbe )
		{
			ItemType = lbe.Details.Type;
			AmountCur = lbe.Amount;
			Number = lbe.Details.Number;
			Graphic = lbe.Details.Graphic;
		}

		public BOBLargeSubEntry( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 0:
				{
					string type = reader.ReadString();

					if ( type != null )
						ItemType = ScriptCompiler.FindTypeByFullName( type );

					AmountCur = reader.ReadEncodedInt();
					Number = reader.ReadEncodedInt();
					Graphic = reader.ReadEncodedInt();

					break;
				}
			}
		}

		public void Serialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( 0 ); // version

			writer.Write( ItemType == null ? null : ItemType.FullName );

			writer.WriteEncodedInt( (int) AmountCur );
			writer.WriteEncodedInt( (int) Number );
			writer.WriteEncodedInt( (int) Graphic );
		}
	}
}