using System;
using Server;
using Server.Engines.Harvest;

namespace Server.Items
{
	public class SturdyShovel : BaseHarvestTool
	{
		public override int LabelNumber{ get{ return 1045125; } } // sturdy shovel
		public override HarvestSystem HarvestSystem{ get{ return Mining.System; } }

		[Constructable]
		public SturdyShovel() : this( 180 )
		{
		}

		[Constructable]
		public SturdyShovel( int uses ) : base( uses, 0xF39 )
		{
			Weight = 5.0;
			Hue = 0x973;
		}

		public SturdyShovel( Serial serial ) : base( serial )
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