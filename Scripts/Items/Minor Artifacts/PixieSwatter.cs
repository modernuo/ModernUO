using System;
using Server;

namespace Server.Items
{
	public class PixieSwatter : Scepter
	{
		public override int LabelNumber{ get{ return 1070854; } } // Pixie Swatter

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		[Constructable]
		public PixieSwatter()
		{
			Hue = 0x8A;
			WeaponAttributes.HitPoisonArea = 75;
			Attributes.WeaponSpeed = 30;
            
			WeaponAttributes.UseBestSkill = 1;
			WeaponAttributes.ResistFireBonus = 12;
			WeaponAttributes.ResistEnergyBonus = 12;

			Slayer = SlayerName.Fey;
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy )
		{
			fire = 100;

			cold = pois = phys = nrgy = 0;
		}

		public PixieSwatter( Serial serial ) : base( serial )
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