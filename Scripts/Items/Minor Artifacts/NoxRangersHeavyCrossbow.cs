using System;
using Server;

namespace Server.Items
{
	public class NoxRangersHeavyCrossbow : HeavyCrossbow
	{
		public override int LabelNumber{ get{ return 1063485; } }

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		[Constructable]
		public NoxRangersHeavyCrossbow()
		{
			Hue = 0x58C;
			WeaponAttributes.HitLeechStam = 40;
			Attributes.SpellChanneling = 1;
			Attributes.WeaponSpeed = 30;
			Attributes.WeaponDamage = 20;
			WeaponAttributes.ResistPoisonBonus = 10;
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy )
		{
			pois = 50;
			phys = 50;

			fire = cold = nrgy = 0;
		}

		public NoxRangersHeavyCrossbow( Serial serial ) : base( serial )
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