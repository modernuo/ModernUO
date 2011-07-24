using System;

namespace Server.Items
{
	public class DecoCheckers2 : Item
	{

		[Constructable]
		public DecoCheckers2() : base( 0xE1B )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoCheckers2( Serial serial ) : base( serial )
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
