using System;
using Server;
using Server.Network;

namespace Server.Items
{
	public class FarmableOnion : FarmableCrop
	{
		public static int GetCropID()
		{
			return 3183;
		}

		public override Item GetCropObject()
		{
			Onion onion = new Onion();

			onion.ItemID = Utility.Random( 3181, 2 );

			return onion;
		}

		public override int GetPickedID()
		{
			return 3254;
		}

		[Constructable]
		public FarmableOnion() : base( GetCropID() )
		{
		}

		public FarmableOnion( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}