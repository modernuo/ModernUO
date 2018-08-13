using System;
using Server.Items;
using Server.Network;

namespace Server.Items
{
	[FlippableAttribute( 0x1443, 0x1442 )]
	public class TwoHandedAxe : BaseAxe
	{
		public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;
		public override WeaponAbility SecondaryAbility => WeaponAbility.ShadowStrike;

		public override int AosStrengthReq => 40;
		public override int AosMinDamage => 16;
		public override int AosMaxDamage => 17;
		public override int AosSpeed => 31;
		public override float MlSpeed => 3.50f;

		public override int OldStrengthReq => 35;
		public override int OldMinDamage => 5;
		public override int OldMaxDamage => 39;
		public override int OldSpeed => 30;

		public override int InitMinHits => 31;
		public override int InitMaxHits => 90;

		[Constructible]
		public TwoHandedAxe() : base( 0x1443 )
		{
			Weight = 8.0;
		}

		public TwoHandedAxe( Serial serial ) : base( serial )
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
