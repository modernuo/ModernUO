using System;
using Server.Items;

namespace Server.Items
{
	public class HardenedWildStaff : WildStaff
	{
		public override int LabelNumber{ get{ return 1073552; } } // hardened wild staff

		[Constructable]
		public HardenedWildStaff()
		{
			Attributes.WeaponDamage = 5;
		}

		public HardenedWildStaff( Serial serial ) : base( serial )
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
