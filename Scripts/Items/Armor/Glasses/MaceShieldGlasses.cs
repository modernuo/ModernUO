using System;
using Server;

namespace Server.Items
{
	public class MaceShieldGlasses : ElvenGlasses
	{
		public override int LabelNumber{ get{ return 1073381; } } //Mace And Shield Reading Glasses

		public override int BasePhysicalResistance{ get{ return 25; } }
		public override int BaseFireResistance{ get{ return 10; } }
		public override int BaseColdResistance{ get{ return 10; } }
		public override int BasePoisonResistance{ get{ return 10; } }
		public override int BaseEnergyResistance{ get{ return 10; } }

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		[Constructable]
		public MaceShieldGlasses()
		{
			WeaponAttributes.HitLowerDefend = 30;
			Attributes.BonusStr = 10;
			Attributes.BonusDex = 5;

			Hue = 0x1DD;
		}
		public MaceShieldGlasses( Serial serial ) : base( serial )
		{
		}
		
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 1 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			if ( version == 0 && Hue == 0 )
				Hue = 0x1DD;
		}
	}
}
