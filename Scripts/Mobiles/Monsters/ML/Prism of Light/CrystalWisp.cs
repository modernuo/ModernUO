using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class CrystalWisp : Wisp
	{
		public override string DefaultName { get { return "a crystal wisp"; } }

		[Constructible]
		public CrystalWisp()
		{
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
