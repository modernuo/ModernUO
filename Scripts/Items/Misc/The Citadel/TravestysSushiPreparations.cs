using System;
using Server;

namespace Server.Items
{
	public class TravestysSushiPreparations : Item
	{
		public override int LabelNumber{ get{ return 1075093; } } // Travesty's Sushi Preparations

		[Constructable]
		public TravestysSushiPreparations() : base( Utility.Random( 0x1E15, 2 ) )
		{
		}

		public TravestysSushiPreparations( Serial serial ) : base( serial )
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

