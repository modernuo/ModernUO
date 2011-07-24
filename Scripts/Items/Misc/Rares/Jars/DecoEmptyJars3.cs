using System;

namespace Server.Items
{
	public class DecoEmptyJars3 : Item
	{

		[Constructable]
		public DecoEmptyJars3() : base( 0xE44 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoEmptyJars3( Serial serial ) : base( serial )
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
