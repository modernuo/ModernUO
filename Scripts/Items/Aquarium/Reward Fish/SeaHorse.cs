using System;
using Server;

namespace Server.Items
{
	public class SeaHorseFish : BaseFish
	{
		public override int LabelNumber => 1074414; // A sea horse

		[Constructible]
		public SeaHorseFish() : base( 0x3B10 )
		{
		}

		public SeaHorseFish( Serial serial ) : base( serial )
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
