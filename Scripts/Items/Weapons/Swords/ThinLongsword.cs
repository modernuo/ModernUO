using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	[FlippableAttribute( 0x13B8, 0x13B7 )]
	public class ThinLongsword : BaseSword
	{
		public override int AosStrengthReq => 35;
		public override int AosMinDamage => 15;
		public override int AosMaxDamage => 16;
		public override int AosSpeed => 30;
		public override float MlSpeed => 3.50f;

		public override int OldStrengthReq => 25;
		public override int OldMinDamage => 5;
		public override int OldMaxDamage => 33;
		public override int OldSpeed => 35;

		public override int DefHitSound => 0x237;
		public override int DefMissSound => 0x23A;

		public override int InitMinHits => 31;
		public override int InitMaxHits => 110;

		[Constructible]
		public ThinLongsword() : base( 0x13B8 )
		{
			Weight = 1.0;
		}

		public ThinLongsword( Serial serial ) : base( serial )
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
