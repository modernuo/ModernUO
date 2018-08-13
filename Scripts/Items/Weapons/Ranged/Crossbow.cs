using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	[FlippableAttribute( 0xF50, 0xF4F )]
	public class Crossbow : BaseRanged
	{
		public override int EffectID => 0x1BFE;
		public override Type AmmoType => typeof( Bolt );
		public override Item Ammo => new Bolt();

		public override WeaponAbility PrimaryAbility => WeaponAbility.ConcussionBlow;
		public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;

		public override int AosStrengthReq => 35;
		public override int AosMinDamage => 18;
		public override int AosMaxDamage => Core.ML ? 22 : 20;
		public override int AosSpeed => 24;
		public override float MlSpeed => 4.50f;

		public override int OldStrengthReq => 30;
		public override int OldMinDamage => 8;
		public override int OldMaxDamage => 43;
		public override int OldSpeed => 18;

		public override int DefMaxRange => 8;

		public override int InitMinHits => 31;
		public override int InitMaxHits => 80;

		[Constructible]
		public Crossbow() : base( 0xF50 )
		{
			Weight = 7.0;
			Layer = Layer.TwoHanded;
		}

		public Crossbow( Serial serial ) : base( serial )
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
