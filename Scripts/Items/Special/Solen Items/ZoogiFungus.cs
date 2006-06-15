using System;
using Server;

namespace Server.Items
{
	public class ZoogiFungus : Item
	{
		[Constructable]
		public ZoogiFungus() : this( 1 )
		{
		}

		[Constructable]
		public ZoogiFungus( int amount ) : base( 0x26B7 )
		{
			Stackable = true;
			Weight = 0.1;
			Amount = amount;
		}

		

		public ZoogiFungus( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}