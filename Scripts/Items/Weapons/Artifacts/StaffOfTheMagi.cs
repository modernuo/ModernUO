using System;
using Server;

namespace Server.Items
{
	public class StaffOfTheMagi : BlackStaff
	{
		public override int LabelNumber{ get{ return 1061600; } } // Staff of the Magi
		public override int ArtifactRarity{ get{ return 11; } }

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		[Constructable]
		public StaffOfTheMagi()
		{
			Hue = 0x481;
			WeaponAttributes.MageWeapon = 30;
			Attributes.SpellChanneling = 1;
			Attributes.CastSpeed = 1;
			Attributes.WeaponDamage = 50;
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy )
		{
			phys = fire = cold = pois = 0;
			nrgy = 100;
		}

		public StaffOfTheMagi( Serial serial ) : base( serial )
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

			if ( WeaponAttributes.MageWeapon == 0 )
				WeaponAttributes.MageWeapon = 30;

			if ( ItemID == 0xDF1 )
				ItemID = 0xDF0;
		}
	}
}