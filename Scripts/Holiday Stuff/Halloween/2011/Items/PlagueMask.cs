using System;
using Server;
using Server.Misc;

namespace Server.Items
{
	public class PlagueMask : Lantern
	{
		public override string DefaultName { get { return "Plague Mask"; } }

		[Constructable]
		public PlagueMask()
			: base( Utility.RandomBool() ? 0x4A8E : 0x4A8F )
		{
		}

		public PlagueMask( Serial serial )
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
