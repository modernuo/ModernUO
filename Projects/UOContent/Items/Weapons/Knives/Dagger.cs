using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0xF52, 0xF51)]
    [SerializationGenerator(0, false)]
    public partial class Dagger : BaseKnife
    {
        [Constructible]
        public Dagger() : base(0xF52) => Weight = 1.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.InfectiousStrike;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ShadowStrike;

        public override int AosStrengthReq => 10;
        public override int AosMinDamage => 10;
        public override int AosMaxDamage => 11;
        public override int AosSpeed => 56;
        public override float MlSpeed => 2.00f;

        public override int OldStrengthReq => 1;
        public override int OldMinDamage => 3;
        public override int OldMaxDamage => 15;
        public override int OldSpeed => 55;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 40;

        public override SkillName DefSkill => SkillName.Fencing;
        public override WeaponType DefType => WeaponType.Piercing;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Pierce1H;
    }
}
