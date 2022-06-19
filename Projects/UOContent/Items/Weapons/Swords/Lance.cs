using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x26C0, 0x26CA)]
    [SerializationGenerator(0, false)]
    public partial class Lance : BaseSword
    {
        [Constructible]
        public Lance() : base(0x26C0) => Weight = 12.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.Dismount;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ConcussionBlow;

        public override int AosStrengthReq => 95;
        public override int AosMinDamage => 17;
        public override int AosMaxDamage => 18;
        public override int AosSpeed => 24;
        public override float MlSpeed => 4.50f;

        public override int OldStrengthReq => 95;
        public override int OldMinDamage => 17;
        public override int OldMaxDamage => 18;
        public override int OldSpeed => 24;

        public override int DefHitSound => 0x23C;
        public override int DefMissSound => 0x238;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 110;

        public override SkillName DefSkill => SkillName.Fencing;
        public override WeaponType DefType => WeaponType.Piercing;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Pierce1H;
    }
}
