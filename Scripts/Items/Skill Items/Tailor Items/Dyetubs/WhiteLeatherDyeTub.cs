using Server;
using System;

namespace Server.Items
{
	public class WhiteLeatherDyeTub : LeatherDyeTub /* OSI UO 13th anniv gift, from redeemable gift tickets */
	{
		public override int LabelNumber { get { return 1149900; } } // White Leather Dye Tub

		public override bool Redyable { get { return false; } }

		[Constructable]
		public WhiteLeatherDyeTub()
		{
			DyedHue = Hue = 0x9C2;
			LootType = LootType.Blessed;
		}


		public WhiteLeatherDyeTub( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( ( int )1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}