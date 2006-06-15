using System;
using Server;

namespace Server.Items
{
	public class LampPost1 : BaseLight
	{
		public override int LitItemID{ get { return 0xB20; } }
		public override int UnlitItemID{ get { return 0xB21; } }
		
		[Constructable]
		public LampPost1() : base( 0xB21 )
		{
			Movable = false;
			Duration = TimeSpan.Zero; // Never burnt out
			Burning = false;
			Light = LightType.Circle300;
			Weight = 40.0;
		}

		public LampPost1( Serial serial ) : base( serial )
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