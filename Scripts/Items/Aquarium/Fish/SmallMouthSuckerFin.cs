using System;
using Server;

namespace Server.Items
{
	public class SmallMouthSuckerFin : BaseFish
	{		
		public override int LabelNumber{ get{ return 1074590; } } // Small Mouth Sucker Fin
		
		[Constructable]
		public SmallMouthSuckerFin() : base( 0x3B01 )
		{
		}

		public SmallMouthSuckerFin( Serial serial ) : base( serial )
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
