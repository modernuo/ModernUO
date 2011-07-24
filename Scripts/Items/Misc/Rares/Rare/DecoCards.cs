using System;

namespace Server.Items
{
	public class DecoCards : Item
	{

		[Constructable]
		public DecoCards() : base( 0xE19 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoCards( Serial serial ) : base( serial )
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
