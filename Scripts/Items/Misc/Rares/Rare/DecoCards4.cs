using System;

namespace Server.Items
{
	public class DecoCards4 : Item
	{

		[Constructable]
		public DecoCards4() : base( 0xE17 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoCards4( Serial serial ) : base( serial )
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
