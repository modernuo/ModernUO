using System;
using Server;

namespace Server.Items
{
	[Flipable]
	public class RoundPaperLantern : BaseLight
	{
		public override int LitItemID{ get { return 0x24C9; } }
		public override int UnlitItemID{ get { return 0x24CA; } }
		
		[Constructable]
		public RoundPaperLantern() : base( 0x24CA )
		{
			Movable = true;
			Duration = TimeSpan.Zero; // Never burnt out
			Burning = false;
			Light = LightType.Circle150;
			Weight = 3.0;
		}

		public RoundPaperLantern( Serial serial ) : base( serial )
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