using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x8FE, 0x4072)]
    [SerializationGenerator(0)]
    public partial class BloodBlade : BaseSword
    {
        [Constructible]
        public BloodBlade() : base(0x8FE)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ParalyzingBlow;
        public override int AosStrengthReq => 10;
        public override int AosMinDamage => 10;
        public override int AosMaxDamage => 12;
        public override float MlSpeed => 2.00f;

        public override int DefHitSound => 0x23C;
        public override int DefMissSound => 0x238;
        public override int InitMinHits => 31;
        public override int InitMaxHits => 90;
        public override SkillName DefSkill => SkillName.Fencing;
        public override WeaponType DefType => WeaponType.Piercing;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Pierce1H;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
    }
}
