using System;

namespace Server.Items
{
	public class DecoToolKit2 : Item
	{

		[Constructable]
		public DecoToolKit2() : base( 0x1EBB )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoToolKit2( Serial serial ) : base( serial )
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
