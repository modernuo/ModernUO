using System;

namespace Server.Items
{
	public class DecoUnfinishedBarrel : Item
	{

		[Constructable]
		public DecoUnfinishedBarrel() : base( 0x1EB5 )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoUnfinishedBarrel( Serial serial ) : base( serial )
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
