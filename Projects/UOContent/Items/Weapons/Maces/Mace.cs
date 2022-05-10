using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0xF5C, 0xF5D)]
    [SerializationGenerator(0, false)]
    public partial class Mace : BaseBashing
    {
        [Constructible]
        public Mace() : base(0xF5C) => Weight = 14.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.ConcussionBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

        public override int AosStrengthReq => 45;
        public override int AosMinDamage => 12;
        public override int AosMaxDamage => 14;
        public override int AosSpeed => 40;
        public override float MlSpeed => 2.75f;

        public override int OldStrengthReq => 20;
        public override int OldMinDamage => 8;
        public override int OldMaxDamage => 32;
        public override int OldSpeed => 30;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 70;
    }
}
