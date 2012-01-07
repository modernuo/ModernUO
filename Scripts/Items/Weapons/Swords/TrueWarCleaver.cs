using System;
using Server.Items;

namespace Server.Items
{
	public class TrueWarCleaver : WarCleaver
	{
		public override int LabelNumber{ get{ return 1073528; } } // true war cleaver

		[Constructable]
		public TrueWarCleaver()
		{
			Attributes.WeaponDamage = 4;
			Attributes.RegenHits = 2;
		}

		public TrueWarCleaver( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}
