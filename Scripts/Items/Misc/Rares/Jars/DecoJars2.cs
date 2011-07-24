using System;

namespace Server.Items
{
	public class DecoJars2 : Item
	{

		[Constructable]
		public DecoJars2() : base( 0xE4E )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoJars2( Serial serial ) : base( serial )
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
