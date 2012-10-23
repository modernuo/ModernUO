using System;
using Server;

namespace Server.Items
{
	public class WrappedCandy : CandyCane
	{
		public override int LabelNumber { get { return 1096950; } } /* wrapped candy */ 

		[Constructable]
		public WrappedCandy()
			: this( 1 )
		{
		}

		public WrappedCandy( int amount )
			: base( 0x469e )
		{
			Stackable = true;
		}

		public WrappedCandy( Serial serial )
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
