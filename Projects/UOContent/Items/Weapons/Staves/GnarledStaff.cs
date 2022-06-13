using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x13F8, 0x13F9)]
    [SerializationGenerator(0, false)]
    public partial class GnarledStaff : BaseStaff
    {
        [Constructible]
        public GnarledStaff() : base(0x13F8) => Weight = 3.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.ConcussionBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ForceOfNature;

        public override int AosStrengthReq => 20;
        public override int AosMinDamage => 15;
        public override int AosMaxDamage => 17;
        public override int AosSpeed => 33;
        public override float MlSpeed => 3.25f;

        public override int OldStrengthReq => 20;
        public override int OldMinDamage => 10;
        public override int OldMaxDamage => 30;
        public override int OldSpeed => 33;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 50;
    }
}
