using System;

namespace Server.Items
{
	public class DecoHalfEmptyJar : Item
	{

		[Constructable]
		public DecoHalfEmptyJar() : base( 0x1007 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoHalfEmptyJar( Serial serial ) : base( serial )
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
