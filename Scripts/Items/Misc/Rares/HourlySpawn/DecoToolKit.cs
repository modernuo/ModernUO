using System;

namespace Server.Items
{
	public class DecoToolKit : Item
	{

		[Constructable]
		public DecoToolKit() : base( 0x1EBA )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoToolKit( Serial serial ) : base( serial )
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
