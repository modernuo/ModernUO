using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x90B, 0x4074)]
    [SerializationGenerator(0)]
    public partial class DreadSword : BaseSword
    {
        [Constructible]
        public DreadSword() : base(0x90B)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ConcussionBlow;
        public override int AosStrengthReq => 35;
        public override int AosMinDamage => 14;
        public override int AosMaxDamage => 18;
        public override float MlSpeed => 3.50f;

        public override int DefHitSound => 0x237;
        public override int DefMissSound => 0x23A;
        public override int InitMinHits => 31;
        public override int InitMaxHits => 110;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
    }
}
