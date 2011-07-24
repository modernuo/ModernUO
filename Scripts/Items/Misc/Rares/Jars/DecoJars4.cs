using System;

namespace Server.Items
{
	public class DecoJars4 : Item
	{

		[Constructable]
		public DecoJars4() : base( 0xE4D )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoJars4( Serial serial ) : base( serial )
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
