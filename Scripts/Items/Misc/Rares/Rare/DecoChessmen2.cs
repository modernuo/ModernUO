using System;

namespace Server.Items
{
	public class DecoChessmen2 : Item
	{

		[Constructable]
		public DecoChessmen2() : base( 0xE12 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoChessmen2( Serial serial ) : base( serial )
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
