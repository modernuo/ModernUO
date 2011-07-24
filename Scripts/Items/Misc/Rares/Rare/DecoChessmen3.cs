using System;

namespace Server.Items
{
	public class DecoChessmen3 : Item
	{

		[Constructable]
		public DecoChessmen3() : base( 0xE14 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoChessmen3( Serial serial ) : base( serial )
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
