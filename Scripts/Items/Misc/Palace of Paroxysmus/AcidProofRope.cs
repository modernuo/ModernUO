using System;
using Server;

namespace Server.Items
{
	public class AcidProofRope : Item
	{
		public override int LabelNumber => 1074886; // Acid Proof Rope

		[Constructible]
		public AcidProofRope() : base( 0x20D )
		{
			Hue = 0x3D1; // TODO check
		}

		public AcidProofRope( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}

