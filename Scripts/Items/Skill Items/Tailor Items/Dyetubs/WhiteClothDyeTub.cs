using Server;
using System;

namespace Server.Items /* High seas, loot from merchant ship's hold, also a "uncommon" loot item */
{
	public class WhiteClothDyeTub : DyeTub
	{
		public override int LabelNumber { get { return 1149984; } } // White Cloth Dye Tub

		public override bool Redyable { get { return false; } }

		[Constructable]
		public WhiteClothDyeTub()
		{
			DyedHue = Hue = 0x9C2;
		}

		public WhiteClothDyeTub( Serial serial )
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