using System;

namespace Server.Items
{
	public class RunedPrism : Item
	{
		public override int LabelNumber{ get{ return 1073465; } } // runed prism

		[Constructable]
		public RunedPrism() : base( 0x2F57 )
		{
			Weight = 1.0;
		}

		public RunedPrism( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}

