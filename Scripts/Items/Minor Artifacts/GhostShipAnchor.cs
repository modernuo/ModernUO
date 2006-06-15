using System;
using Server;

namespace Server.Items
{
	public class GhostShipAnchor : Item
	{
		public override int LabelNumber{ get{ return 1070816; } } // Ghost Ship Anchor
		
		[Constructable]
		public GhostShipAnchor() : base( 0x14F7 )
		{
			Hue = 0x47E;
		}

		public GhostShipAnchor( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if( ItemID == 0x1F47 )
				ItemID = 0x14F7;
		}
	}
}