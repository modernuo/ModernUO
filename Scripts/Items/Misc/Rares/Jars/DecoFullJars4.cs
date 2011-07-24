using System;

namespace Server.Items
{
	public class DecoFullJars4 : Item
	{

		[Constructable]
		public DecoFullJars4() : base( 0xE49 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoFullJars4( Serial serial ) : base( serial )
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
