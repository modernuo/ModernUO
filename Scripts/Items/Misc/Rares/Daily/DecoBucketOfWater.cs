using System;

namespace Server.Items
{
	public class DecoBucketOfWater : Item
	{

		[Constructable]
		public DecoBucketOfWater() : base( 0x2004 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoBucketOfWater( Serial serial ) : base( serial )
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
