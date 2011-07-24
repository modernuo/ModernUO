using System;

namespace Server.Items
{
	public class DecoBarrel2 : Item
	{

		[Constructable]
		public DecoBarrel2() : base( 0xFAE )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoBarrel2( Serial serial ) : base( serial )
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
