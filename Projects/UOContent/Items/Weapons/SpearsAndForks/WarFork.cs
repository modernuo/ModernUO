using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x1405, 0x1404)]
    [SerializationGenerator(0, false)]
    public partial class WarFork : BaseSpear
    {
        [Constructible]
        public WarFork() : base(0x1405) => Weight = 9.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

        public override int AosStrengthReq => 45;
        public override int AosMinDamage => 12;
        public override int AosMaxDamage => 13;
        public override int AosSpeed => 43;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 35;
        public override int OldMinDamage => 4;
        public override int OldMaxDamage => 32;
        public override int OldSpeed => 45;

        public override int DefHitSound => 0x236;
        public override int DefMissSound => 0x238;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 110;

        public override WeaponAnimation DefAnimation => WeaponAnimation.Pierce1H;
    }
}
