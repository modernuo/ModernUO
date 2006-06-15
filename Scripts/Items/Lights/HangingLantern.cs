using System;
using Server;

namespace Server.Items
{
	public class HangingLantern : BaseLight
	{
		public override int LitItemID{ get { return 0xA1A; } }
		public override int UnlitItemID{ get { return 0xA1D; } }
		
		[Constructable]
		public HangingLantern() : base( 0xA1D )
		{
			Movable = false;
			Duration = TimeSpan.Zero; // Never burnt out
			Burning = false;
			Light = LightType.Circle300;
			Weight = 40.0;
		}

		public HangingLantern( Serial serial ) : base( serial )
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