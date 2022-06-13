using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0xE89, 0xE8a)]
    [SerializationGenerator(0, false)]
    public partial class QuarterStaff : BaseStaff
    {
        [Constructible]
        public QuarterStaff() : base(0xE89) => Weight = 4.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ConcussionBlow;

        public override int AosStrengthReq => 30;
        public override int AosMinDamage => 11;
        public override int AosMaxDamage => 14;
        public override int AosSpeed => 48;
        public override float MlSpeed => 2.25f;

        public override int OldStrengthReq => 30;
        public override int OldMinDamage => 8;
        public override int OldMaxDamage => 28;
        public override int OldSpeed => 48;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 60;
    }
}
