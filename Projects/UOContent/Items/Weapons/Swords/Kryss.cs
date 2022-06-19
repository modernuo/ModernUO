using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x1401, 0x1400)]
    [SerializationGenerator(0, false)]
    public partial class Kryss : BaseSword
    {
        [Constructible]
        public Kryss() : base(0x1401) => Weight = 2.0;

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
    }
}
