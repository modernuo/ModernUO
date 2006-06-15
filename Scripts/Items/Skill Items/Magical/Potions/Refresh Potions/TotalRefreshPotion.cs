using System;
using Server;

namespace Server.Items
{
	public class TotalRefreshPotion : BaseRefreshPotion
	{
		public override double Refresh{ get{ return 1.0; } }

		[Constructable]
		public TotalRefreshPotion() : base( PotionEffect.RefreshTotal )
		{
		}

		public TotalRefreshPotion( Serial serial ) : base( serial )
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