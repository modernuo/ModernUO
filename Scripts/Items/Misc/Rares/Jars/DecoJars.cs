using System;

namespace Server.Items
{
	public class DecoJars : Item
	{

		[Constructable]
		public DecoJars() : base( 0xE4F )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoJars( Serial serial ) : base( serial )
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
