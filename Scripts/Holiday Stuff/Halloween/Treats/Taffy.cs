using System;
using Server;

namespace Server.Items
{
	public class Taffy : CandyCane
	{
		public override int LabelNumber { get { return 1096949; } } /* taffy */

		[Constructable]
		public Taffy()
			: this( 1 )
		{
		}

		public Taffy( int amount )
			: base( 0x469D )
		{
			Stackable = true;
		}

		public Taffy( Serial serial )
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
