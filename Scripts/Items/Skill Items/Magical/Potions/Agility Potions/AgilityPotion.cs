using System;
using Server;

namespace Server.Items
{
	public class AgilityPotion : BaseAgilityPotion
	{
		public override int DexOffset => 10;
		public override TimeSpan Duration => TimeSpan.FromMinutes( 2.0 );

		[Constructible]
		public AgilityPotion() : base( PotionEffect.Agility )
		{
		}

		public AgilityPotion( Serial serial ) : base( serial )
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
