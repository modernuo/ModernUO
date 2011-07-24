using System;

namespace Server.Items
{
	public class DecoEmptyJars4 : Item
	{

		[Constructable]
		public DecoEmptyJars4() : base( 0xE45 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoEmptyJars4( Serial serial ) : base( serial )
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
