using System;
using Server;

namespace Server.Items
{
	public class CounselorRobe : BaseSuit
	{
		[Constructable]
		public CounselorRobe() : base( AccessLevel.Counselor, 0x3, 0x204F )
		{
		}

		public CounselorRobe( Serial serial ) : base( serial )
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