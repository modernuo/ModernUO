using System;
using Server;

namespace Server.Items
{
	[Flipable]
	public class ShojiLantern : BaseLight
	{
		public override int LitItemID{ get { return 0x24BB; } }
		public override int UnlitItemID{ get { return 0x24BC; } }
		
		[Constructable]
		public ShojiLantern() : base( 0x24BC )
		{
			Movable = true;
			Duration = TimeSpan.Zero; // Never burnt out
			Burning = false;
			Light = LightType.Circle150;
			Weight = 3.0;
		}

		public ShojiLantern( Serial serial ) : base( serial )
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