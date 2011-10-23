using System;
using Server;

namespace Server.Items
{
	public class PurpleFrog : BaseFish
	{		
		public override int LabelNumber{ get{ return 1073823; } } // A Purple Frog
		
		[Constructable]
		public PurpleFrog() : base( 0x3B0D )
		{
			Hue = 0x4FA;
		}

		public PurpleFrog( Serial serial ) : base( serial )
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
