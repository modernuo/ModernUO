using System;

namespace Server.Items
{
	public class DecoPlayingCards : Item
	{

		[Constructable]
		public DecoPlayingCards() : base( 0xFA3 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoPlayingCards( Serial serial ) : base( serial )
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
