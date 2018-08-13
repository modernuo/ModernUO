using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	[FlippableAttribute( 0x27A9, 0x27F4 )]
	public class Daisho : BaseSword
	{
		public override WeaponAbility PrimaryAbility => WeaponAbility.Feint;
		public override WeaponAbility SecondaryAbility => WeaponAbility.DoubleStrike;

		public override int AosStrengthReq => 40;
		public override int AosMinDamage => 13;
		public override int AosMaxDamage => 15;
		public override int AosSpeed => 40;
		public override float MlSpeed => 2.75f;

		public override int OldStrengthReq => 40;
		public override int OldMinDamage => 13;
		public override int OldMaxDamage => 15;
		public override int OldSpeed => 40;

		public override int DefHitSound => 0x23B;
		public override int DefMissSound => 0x23A;

		public override int InitMinHits => 45;
		public override int InitMaxHits => 65;

		[Constructible]
		public Daisho() : base( 0x27A9 )
		{
			Weight = 8.0;
			Layer = Layer.TwoHanded;
		}

		public Daisho( Serial serial ) : base( serial )
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
