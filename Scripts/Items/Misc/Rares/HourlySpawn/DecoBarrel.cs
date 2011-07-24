using System;

namespace Server.Items
{
	public class DecoBarrel : Item
	{

		[Constructable]
		public DecoBarrel() : base( 0xE77 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoBarrel( Serial serial ) : base( serial )
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
