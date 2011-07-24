using System;

namespace Server.Items
{
	public class DecoJars3 : Item
	{

		[Constructable]
		public DecoJars3() : base( 0xE4C )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoJars3( Serial serial ) : base( serial )
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
