using System;

namespace Server.Items
{
	public class DecoEmptyJar : Item
	{

		[Constructable]
		public DecoEmptyJar() : base( 0x1005 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoEmptyJar( Serial serial ) : base( serial )
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
