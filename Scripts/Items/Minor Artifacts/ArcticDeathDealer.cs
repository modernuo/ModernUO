using System;
using Server;

namespace Server.Items
{
	public class ArcticDeathDealer : WarMace
	{
		public override int LabelNumber{ get{ return 1063481; } }

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		[Constructable]
		public ArcticDeathDealer()
		{
			Hue = 0x480;
			WeaponAttributes.HitHarm = 33;
			WeaponAttributes.HitLowerAttack = 40;
			Attributes.WeaponSpeed = 20;
			Attributes.WeaponDamage = 40;
			WeaponAttributes.ResistColdBonus = 10;
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy )
		{
			cold = 50;
			phys = 50;

			pois = fire = nrgy = 0;
		}

		public ArcticDeathDealer( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}