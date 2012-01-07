using System;
using Server.Items;

namespace Server.Items
{
	public class SapphireMace : DiamondMace
	{
		public override int LabelNumber{ get{ return 1073531; } } // sapphire mace

		[Constructable]
		public SapphireMace()
		{
			WeaponAttributes.ResistEnergyBonus = 5;
		}

		public SapphireMace( Serial serial ) : base( serial )
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
