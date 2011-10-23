using System;
using Server;

namespace Server.Items
{
	public class BrineShrimp : BaseFish
	{		
		public override int LabelNumber{ get{ return 1074415; } } // Brine shrimp
		
		[Constructable]
		public BrineShrimp() : base( 0x3B11 )
		{
		}

		public BrineShrimp( Serial serial ) : base( serial )
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
