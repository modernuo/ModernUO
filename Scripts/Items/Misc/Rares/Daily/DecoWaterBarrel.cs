using System;

namespace Server.Items
{
	public class DecoWaterBarrel : Item
	{

		[Constructable]
		public DecoWaterBarrel() : base( 0x154D )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoWaterBarrel( Serial serial ) : base( serial )
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
