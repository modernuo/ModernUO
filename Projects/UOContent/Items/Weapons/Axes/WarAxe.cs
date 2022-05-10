using ModernUO.Serialization;
using Server.Engines.Harvest;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x13B0, 0x13AF)]
    public partial class WarAxe : BaseAxe
    {
        [Constructible]
        public WarAxe() : base(0x13B0) => Weight = 8.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
        public override WeaponAbility SecondaryAbility => WeaponAbility.BleedAttack;

        public override int AosStrengthReq => 35;
        public override int AosMinDamage => 14;
        public override int AosMaxDamage => 15;
        public override int AosSpeed => 33;
        public override float MlSpeed => 3.25f;

        public override int OldStrengthReq => 35;
        public override int OldMinDamage => 9;
        public override int OldMaxDamage => 27;
        public override int OldSpeed => 40;

        public override int DefHitSound => 0x233;
        public override int DefMissSound => 0x239;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 80;

        public override SkillName DefSkill => SkillName.Macing;
        public override WeaponType DefType => WeaponType.Bashing;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Bash1H;

        public override HarvestSystem HarvestSystem => null;
    }
}
