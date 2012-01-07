using System;
using Server.Items;

namespace Server.Items
{
	public class DarkglowScimitar : RadiantScimitar
	{
		public override int LabelNumber{ get{ return 1073542; } } // darkglow scimitar

		[Constructable]
		public DarkglowScimitar()
		{
			WeaponAttributes.HitDispel = 10;
		}

		public DarkglowScimitar( Serial serial ) : base( serial )
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
