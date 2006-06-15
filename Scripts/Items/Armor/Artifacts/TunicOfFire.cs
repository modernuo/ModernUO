using System;
using Server;

namespace Server.Items
{
	public class TunicOfFire : ChainChest
	{
		public override int LabelNumber{ get{ return 1061099; } } // Tunic of Fire
		public override int ArtifactRarity{ get{ return 11; } }

		public override int BasePhysicalResistance{ get{ return 24; } }
		public override int BaseFireResistance{ get{ return 34; } }

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		[Constructable]
		public TunicOfFire()
		{
			Hue = 0x54F;
			ArmorAttributes.SelfRepair = 5;
			Attributes.NightSight = 1;
			Attributes.ReflectPhysical = 15;
		}

		public TunicOfFire( Serial serial ) : base( serial )
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

			if ( version < 1 )
			{
				if ( Hue == 0x54E )
					Hue = 0x54F;

				if ( Attributes.NightSight == 0 )
					Attributes.NightSight = 1;

				PhysicalBonus = 0;
				FireBonus = 0;
			}
		}
	}
}