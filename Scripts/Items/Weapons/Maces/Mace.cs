using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	[FlippableAttribute( 0xF5C, 0xF5D )]
	public class Mace : BaseBashing
	{
		public override WeaponAbility PrimaryAbility => WeaponAbility.ConcussionBlow;
		public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

		public override int AosStrengthReq => 45;
		public override int AosMinDamage => 12;
		public override int AosMaxDamage => 14;
		public override int AosSpeed => 40;
		public override float MlSpeed => 2.75f;

		public override int OldStrengthReq => 20;
		public override int OldMinDamage => 8;
		public override int OldMaxDamage => 32;
		public override int OldSpeed => 30;

		public override int InitMinHits => 31;
		public override int InitMaxHits => 70;

		[Constructible]
		public Mace() : base( 0xF5C )
		{
			Weight = 14.0;
		}

		public Mace( Serial serial ) : base( serial )
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
