using System;
using Server;

namespace Server.Items
{
	public class SeerRobe : BaseSuit
	{
		[Constructable]
		public SeerRobe() : base( AccessLevel.Seer, 0x1D3, 0x204F )
		{
		}

		public SeerRobe( Serial serial ) : base( serial )
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