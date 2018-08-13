using System;
using Server.Items;

namespace Server.Items
{
	public class CorruptedRuneBlade : RuneBlade
	{
		public override int LabelNumber => 1073540; // Corrupted Rune Blade

		[Constructible]
		public CorruptedRuneBlade()
		{
			WeaponAttributes.ResistPhysicalBonus = -5;
			WeaponAttributes.ResistPoisonBonus = 12;
		}

		public CorruptedRuneBlade( Serial serial ) : base( serial )
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
