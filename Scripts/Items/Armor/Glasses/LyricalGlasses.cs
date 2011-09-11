using System;
using Server;

namespace Server.Items
{
	public class LyricalGlasses : ElvenGlasses
	{
		public override int LabelNumber{ get{ return 1073382; } } //Lyrical Reading Glasses

		public override int BasePhysicalResistance{ get{ return 10; } }
		public override int BaseFireResistance{ get{ return 10; } }
		public override int BaseColdResistance{ get{ return 10; } }
		public override int BasePoisonResistance{ get{ return 10; } }
		public override int BaseEnergyResistance{ get{ return 10; } }

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		private AosWeaponAttributes m_AosWeaponAttributes;

		[Constructable]
		public LyricalGlasses()
		{
			WeaponAttributes.HitLowerDefend = 20;
			Attributes.NightSight = 1;
			Attributes.ReflectPhysical = 15;
		}
		public LyricalGlasses( Serial serial ) : base( serial )
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
