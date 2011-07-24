using System;

namespace Server.Items
{
	public class DecoLockpicks : Item
	{

		[Constructable]
		public DecoLockpicks() : base( 0x14FE )
		{
			Movable = true;
			Stackable = false;
		}

		public DecoLockpicks( Serial serial ) : base( serial )
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
