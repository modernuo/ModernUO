using System;

namespace Server.Items
{
	public class DecoBrokenChair2 : Item
	{

		[Constructable]
		public DecoBrokenChair2() : base( 0xC1A )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoBrokenChair2( Serial serial ) : base( serial )
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
