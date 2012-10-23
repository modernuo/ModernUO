using System;
using Server;

namespace Server.Items
{
	public class ChairInAGhostCostume : Item
	{
		public override double DefaultWeight { get { return 5; } }

		[Constructable]
		public ChairInAGhostCostume()
			: base( 0x3F26 )
		{
		}

		public ChairInAGhostCostume( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( ( int )0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
