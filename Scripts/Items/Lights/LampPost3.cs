using System;
using Server;

namespace Server.Items
{
	public class LampPost3 : BaseLight
	{
		public override int LitItemID{ get { return 0xB24; } }
		public override int UnlitItemID{ get { return 0xB25; } }
		
		[Constructable]
		public LampPost3() : base( 0xb25 )
		{
			Movable = false;
			Duration = TimeSpan.Zero; // Never burnt out
			Burning = false;
			Light = LightType.Circle300;
			Weight = 40.0;
		}

		public LampPost3( Serial serial ) : base( serial )
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