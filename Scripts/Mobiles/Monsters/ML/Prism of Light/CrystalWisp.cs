using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class CrystalWisp : Wisp
	{
		[Constructable]
		public CrystalWisp()
		{
			Name = "a crystal wisp";
			Hue = 0x482;

			PackArcaneScroll( 0, 1 );
		}

		public CrystalWisp( Serial serial )
			: base( serial )
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
