namespace Server.Items
{
	[FlippableAttribute( 0x1401, 0x1400 )]
	public class Kryss : BaseSword
	{
		public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
		public override WeaponAbility SecondaryAbility => WeaponAbility.InfectiousStrike;

		public override int AosStrengthReq => 10;
		public override int AosMinDamage => 10;
		public override int AosMaxDamage => 12;
		public override int AosSpeed => 53;
		public override float MlSpeed => 2.00f;

		public override int OldStrengthReq => 10;
		public override int OldMinDamage => 3;
		public override int OldMaxDamage => 28;
		public override int OldSpeed => 53;

		public override int DefHitSound => 0x23C;
		public override int DefMissSound => 0x238;

		public override int InitMinHits => 31;
		public override int InitMaxHits => 90;

		public override SkillName DefSkill => SkillName.Fencing;
		public override WeaponType DefType => WeaponType.Piercing;
		public override WeaponAnimation DefAnimation => WeaponAnimation.Pierce1H;

		[Constructible]
		public Kryss() : base( 0x1401 )
		{
			Weight = 2.0;
		}

		public Kryss( Serial serial ) : base( serial )
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

			if ( Weight == 1.0 )
				Weight = 2.0;
		}
	}
}
