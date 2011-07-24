using System;

namespace Server.Items
{
	public class DecoFullJars3 : Item
	{

		[Constructable]
		public DecoFullJars3() : base( 0xE48 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoFullJars3( Serial serial ) : base( serial )
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
