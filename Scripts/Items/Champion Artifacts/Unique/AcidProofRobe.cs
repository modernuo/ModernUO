using System;
using Server;

namespace Server.Items
{
	public class AcidProofRobe : Robe
	{
		public override int LabelNumber{ get{ return 1095236; } } // Acid-Proof Robe [Replica]

		public override int BaseFireResistance{ get{ return 4; } }

		public override int InitMinHits{ get{ return 150; } }
		public override int InitMaxHits{ get{ return 150; } }

		public override bool CanFortify{ get{ return false; } }

		[Constructable]
		public AcidProofRobe()
		{
			Hue = 0x1;
			LootType = LootType.Blessed;
		}

		public AcidProofRobe( Serial serial ) : base( serial )
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
