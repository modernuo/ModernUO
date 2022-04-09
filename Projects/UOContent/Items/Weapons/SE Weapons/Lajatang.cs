using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x27A7, 0x27F2)]
    [SerializationGenerator(0, false)]
    public partial class Lajatang : BaseKnife
    {
        [Constructible]
        public Lajatang() : base(0x27A7)
        {
            Weight = 12.0;
            Layer = Layer.TwoHanded;
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.DefenseMastery;
        public override WeaponAbility SecondaryAbility => WeaponAbility.FrenziedWhirlwind;

        public override int AosStrengthReq => 65;
        public override int AosMinDamage => 16;
        public override int AosMaxDamage => 18;
        public override int AosSpeed => 32;
        public override float MlSpeed => 3.50f;

        public override int OldStrengthReq => 65;
        public override int OldMinDamage => 16;
        public override int OldMaxDamage => 18;
        public override int OldSpeed => 55;

        public override int DefHitSound => 0x232;
        public override int DefMissSound => 0x238;

        public override int InitMinHits => 90;
        public override int InitMaxHits => 95;

        public override SkillName DefSkill => SkillName.Fencing;
        public override WeaponType DefType => WeaponType.Piercing;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Pierce1H;
    }
}
