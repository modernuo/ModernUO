using System;
using Server;

namespace Server.Items
{
	public class HelmOfSwiftness : WingedHelm
	{
		public override int LabelNumber{ get{ return 1075037; } } // Helm of Swiftness
		
		public override int BasePhysicalResistance{ get{ return 6; } }
		public override int BaseFireResistance{ get{ return 5; } }
		public override int BaseColdResistance{ get{ return 6; } }
		public override int BasePoisonResistance{ get{ return 6; } }
		public override int BaseEnergyResistance{ get{ return 8; } }

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		[Constructable]
		public HelmOfSwiftness() : base()
		{
			Hue = 0x592;
			
			Attributes.BonusInt = 5;
			Attributes.CastSpeed = 1;
			Attributes.CastRecovery = 2;
			ArmorAttributes.MageArmor = 1;
		}

		public HelmOfSwiftness( Serial serial ) : base( serial )
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
