using System;
using Server;

namespace Server.Items
{
	public class Candelabra : BaseLight
	{
		public override int LitItemID{ get { return 0xB1D; } }
		public override int UnlitItemID{ get { return 0xA27; } }

		[Constructable]
		public Candelabra() : base( 0xA27 )
		{
			Duration = TimeSpan.Zero; // Never burnt out
			Burning = false;
			Light = LightType.Circle225;
			Weight = 3.0;
		}

		public Candelabra( Serial serial ) : base( serial )
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