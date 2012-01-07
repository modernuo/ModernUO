using System;
using Server.Items;

namespace Server.Items
{
	public class ArcanistsWildStaff : WildStaff
	{
		public override int LabelNumber{ get{ return 1073549; } } // arcanist's wild staff

		[Constructable]
		public ArcanistsWildStaff()
		{
			Attributes.BonusMana = 3;
			Attributes.WeaponDamage = 3;
		}

		public ArcanistsWildStaff( Serial serial ) : base( serial )
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
