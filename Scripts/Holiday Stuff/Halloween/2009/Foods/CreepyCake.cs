using System;
using Server;

namespace Server.Items
{
	/* 
	first seen halloween 2009.  subsequently in 2010, 
	2011 and 2012. GM Beggar-only Semi-Rare Treats
	*/

	public class CreepyCake : Food
	{
		public override string DefaultName { get { return "Creepy Cake"; } }

		[Constructable]
		public CreepyCake()
			: base( 0x9e9 )
		{
			Hue = 0x3E4;
		}

		public CreepyCake( Serial serial )
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
