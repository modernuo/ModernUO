using System;

namespace Server.Items
{
	public class DecoFullJars2 : Item
	{

		[Constructable]
		public DecoFullJars2() : base( 0xE4A )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoFullJars2( Serial serial ) : base( serial )
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
