using System;

namespace Server.Items
{
	public class DecoBrokenChair : Item
	{

		[Constructable]
		public DecoBrokenChair() : base( 0xC19 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoBrokenChair( Serial serial ) : base( serial )
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
