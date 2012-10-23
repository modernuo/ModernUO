using System;
using Server;

namespace Server.Items
{
	public class MrPlainsCookies : Food
	{
		public override string DefaultName{ get { return "Mr Plain's Cookies"; } }

		[Constructable]
		public MrPlainsCookies( )
			: base( 0x160C )
		{
			this.Weight = 1.0;
			this.FillFactor = 4;
			Hue = 0xF4;
		}

		public MrPlainsCookies( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( ( int )0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
