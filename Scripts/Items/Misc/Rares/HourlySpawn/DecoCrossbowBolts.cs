using System;

namespace Server.Items
{
	public class DecoCrossbowBolts : Item
	{

		[Constructable]
		public DecoCrossbowBolts() : base( 0x1BFC )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoCrossbowBolts( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
