using System;

namespace Server.Items
{
	public class DecoEmptyTub : Item
	{

		[Constructable]
		public DecoEmptyTub() : base( 0xE83 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoEmptyTub( Serial serial ) : base( serial )
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
