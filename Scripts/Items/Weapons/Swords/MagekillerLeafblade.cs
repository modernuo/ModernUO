using System;
using Server.Items;

namespace Server.Items
{
	public class MagekillerLeafblade : Leafblade
	{
		public override int LabelNumber{ get{ return 1073523; } } // maagekiller leafblade

		[Constructable]
		public MagekillerLeafblade()
		{
			WeaponAttributes.HitLeechMana = 16;
		}

		public MagekillerLeafblade( Serial serial ) : base( serial )
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
