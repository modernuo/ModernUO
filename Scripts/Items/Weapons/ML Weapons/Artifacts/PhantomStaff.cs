using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	public class PhantomStaff : WildStaff
	{
		public override int LabelNumber{ get{ return 1072919; } } // Phantom Staff

		[Constructable]
		public PhantomStaff()
		{
			Hue = 0x1;
			Attributes.RegenHits = 2;
			Attributes.NightSight = 1;
			Attributes.WeaponSpeed = 20;
			Attributes.WeaponDamage = 60;
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy )
		{
			phys = fire = nrgy = 0;
			cold = pois = 50;
		}

		public PhantomStaff( Serial serial ) : base( serial )
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