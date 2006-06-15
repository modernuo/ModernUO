using System;
using Server;

namespace Server.Items
{
	public class CandleShort : BaseLight
	{
		public override int LitItemID{ get { return 0x142C; } }
		public override int UnlitItemID{ get { return 0x142F; } }

		[Constructable]
		public CandleShort() : base( 0x142F )
		{
			if ( Burnout )
				Duration = TimeSpan.FromMinutes( 25 );
			else
				Duration = TimeSpan.Zero;

			Burning = false;
			Light = LightType.Circle150;
			Weight = 1.0;
		}

		public CandleShort( Serial serial ) : base( serial )
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