using System;

namespace Server.Items
{
	public class DecoIngots : Item
	{

		[Constructable]
		public DecoIngots() : base( 0x1BF2 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoIngots( Serial serial ) : base( serial )
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
