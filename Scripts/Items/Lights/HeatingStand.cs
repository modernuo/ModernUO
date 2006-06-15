using System;
using Server;

namespace Server.Items
{
	public class HeatingStand : BaseLight
	{
		public override int LitItemID{ get { return 0x184A; } }
		public override int UnlitItemID{ get { return 0x1849; } }

		[Constructable]
		public HeatingStand() : base( 0x1849 )
		{
			if ( Burnout )
				Duration = TimeSpan.FromMinutes( 25 );
			else
				Duration = TimeSpan.Zero;

			Burning = false;
			Light = LightType.Empty;
			Weight = 1.0;
		}

		public override void Ignite()
		{
			base.Ignite();

			if ( ItemID == LitItemID )
				Light = LightType.Circle150;
			else if ( ItemID == UnlitItemID )
				Light = LightType.Empty;
		}

		public override void Douse()
		{
			base.Douse();

			if ( ItemID == LitItemID )
				Light = LightType.Circle150;
			else if ( ItemID == UnlitItemID )
				Light = LightType.Empty;
		}

		public HeatingStand( Serial serial ) : base( serial )
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