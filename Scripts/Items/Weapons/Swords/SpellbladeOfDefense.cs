using System;
using Server.Items;

namespace Server.Items
{
	public class SpellbladeOfDefense : ElvenSpellblade
	{
		public override int LabelNumber{ get{ return 1073516; } } // spellblade of defense

		[Constructable]
		public SpellbladeOfDefense()
		{
			Attributes.DefendChance = 5;
		}

		public SpellbladeOfDefense( Serial serial ) : base( serial )
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
