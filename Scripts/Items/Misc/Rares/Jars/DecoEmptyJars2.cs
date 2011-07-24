using System;

namespace Server.Items
{
	public class DecoEmptyJars2 : Item
	{

		[Constructable]
		public DecoEmptyJars2() : base( 0xE46 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoEmptyJars2( Serial serial ) : base( serial )
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
