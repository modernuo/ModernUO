using System;

namespace Server.Items
{
	public class DecoBucket : Item
	{

		[Constructable]
		public DecoBucket() : base( 0x14E0 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoBucket( Serial serial ) : base( serial )
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
