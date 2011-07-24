using System;

namespace Server.Items
{
	public class JeweledFiligree : Item
	{
		public override int LabelNumber{ get{ return 1072894; } } // jeweled filigree

		[Constructable]
		public JeweledFiligree() : base( 0x2F5E )
		{
			Weight = 1.0;
		}

		public JeweledFiligree( Serial serial ) : base( serial )
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

