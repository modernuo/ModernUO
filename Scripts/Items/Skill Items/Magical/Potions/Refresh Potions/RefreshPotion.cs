using System;
using Server;

namespace Server.Items
{
	public class RefreshPotion : BaseRefreshPotion
	{
		public override double Refresh{ get{ return 0.25; } }

		[Constructable]
		public RefreshPotion() : base( PotionEffect.Refresh )
		{
		}

		public RefreshPotion( Serial serial ) : base( serial )
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