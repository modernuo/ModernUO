using System;

namespace Server.Items
{
	public class DecoKeg : Item
	{

		[Constructable]
		public DecoKeg() : base( 0xE7F )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoKeg( Serial serial ) : base( serial )
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
