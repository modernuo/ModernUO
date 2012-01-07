using System;
using Server.Items;

namespace Server.Items
{
	public class ChargedAssassinSpike : AssassinSpike
	{
		public override int LabelNumber{ get{ return 1073518; } } // charged assassin spike

		[Constructable]
		public ChargedAssassinSpike()
		{
			WeaponAttributes.HitLightning = 10;
		}

		public ChargedAssassinSpike( Serial serial ) : base( serial )
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
