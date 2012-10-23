using System;
using Server;

namespace Server.Items
{
	/* 
	first seen halloween 2009.  subsequently in 2010, 
	2011 and 2012. GM Beggar-only Semi-Rare Treats
	*/

	public class PumpkinPizza : CheesePizza
	{
		public override string DefaultName { get { return "Pumpkin Pizza"; } }

		[Constructable]
		public PumpkinPizza( )
			: base()
		{
			Hue = 0xF3;
		}

		public PumpkinPizza( Serial serial )
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
