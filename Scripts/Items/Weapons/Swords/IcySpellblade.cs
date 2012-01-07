using System;
using Server.Items;

namespace Server.Items
{
	public class IcySpellblade : ElvenSpellblade
	{
		public override int LabelNumber{ get{ return 1073514; } } // icy spellblade

		[Constructable]
		public IcySpellblade()
		{
			WeaponAttributes.ResistColdBonus = 5;
		}

		public IcySpellblade( Serial serial ) : base( serial )
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
