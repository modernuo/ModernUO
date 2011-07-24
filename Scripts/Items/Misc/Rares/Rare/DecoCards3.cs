using System;

namespace Server.Items
{
	public class DecoCards3 : Item
	{

		[Constructable]
		public DecoCards3() : base( 0xE15 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoCards3( Serial serial ) : base( serial )
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
