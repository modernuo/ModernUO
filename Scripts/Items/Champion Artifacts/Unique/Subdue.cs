using System;
using Server;

namespace Server.Items
{
	public class Subdue : Scythe
	{
		public override int LabelNumber{ get{ return 1094930; } } // Subdue [Replica]

		public override int InitMinHits{ get{ return 150; } }
		public override int InitMaxHits{ get{ return 150; } }

		public override bool CanFortify{ get{ return false; } }

		[Constructable]
		public Subdue()
		{
			Attributes.SpellChanneling = 1;
			Attributes.WeaponSpeed = 20;
			Attributes.WeaponDamage = 50;
			Attributes.AttackChance = 10;

			WeaponAttributes.HitLeechMana = 100;
			WeaponAttributes.UseBestSkill = 1;
		}

		public Subdue( Serial serial ) : base( serial )
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
