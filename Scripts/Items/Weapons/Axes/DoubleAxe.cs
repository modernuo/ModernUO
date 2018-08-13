using System;
using Server.Items;
using Server.Network;

namespace Server.Items
{
	[FlippableAttribute( 0xf4b, 0xf4c )]
	public class DoubleAxe : BaseAxe
	{
		public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;
		public override WeaponAbility SecondaryAbility => WeaponAbility.WhirlwindAttack;

		public override int AosStrengthReq => 45;
		public override int AosMinDamage => 15;
		public override int AosMaxDamage => 17;
		public override int AosSpeed => 33;
		public override float MlSpeed => 3.25f;

		public override int OldStrengthReq => 45;
		public override int OldMinDamage => 5;
		public override int OldMaxDamage => 35;
		public override int OldSpeed => 37;

		public override int InitMinHits => 31;
		public override int InitMaxHits => 110;

		[Constructible]
		public DoubleAxe() : base( 0xF4B )
		{
			Weight = 8.0;
		}

		public DoubleAxe( Serial serial ) : base( serial )
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
