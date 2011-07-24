using System;

namespace Server.Items
{
	public class DecoTray : Item
	{

		[Constructable]
		public DecoTray() : base( 0x992 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoTray( Serial serial ) : base( serial )
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
