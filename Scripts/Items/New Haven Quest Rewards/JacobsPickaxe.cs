using System;
using Server.Mobiles;

namespace Server.Items
{
	public class JacobsPickaxe : Pickaxe
	{
		public override int LabelNumber{ get{ return 1077758; } } // Jacob's Pickaxe

		// TODO: Recharges 1 use every 5 minutes.  Doesn't break when it reaches 0, you get a system message "You must wait a moment for it to recharge" 1072306 if you attempt to use it with no uses remaining.

		[Constructable]
		public JacobsPickaxe()
		{
			UsesRemaining = 20;
			LootType = LootType.Blessed;

			SkillBonuses.SetValues( 0, SkillName.Mining, 10.0 );
		}

		public JacobsPickaxe( Serial serial )
			: base( serial )
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
