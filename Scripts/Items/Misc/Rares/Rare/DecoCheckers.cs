using System;

namespace Server.Items
{
	public class DecoCheckers : Item
	{

		[Constructable]
		public DecoCheckers() : base( 0xE1A )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoCheckers( Serial serial ) : base( serial )
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
