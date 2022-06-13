using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x143B, 0x143A)]
    [SerializationGenerator(0, false)]
    public partial class Maul : BaseBashing
    {
        [Constructible]
        public Maul() : base(0x143B) => Weight = 10.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ConcussionBlow;

        public override int AosStrengthReq => 45;
        public override int AosMinDamage => 14;
        public override int AosMaxDamage => 16;
        public override int AosSpeed => 32;
        public override float MlSpeed => 3.50f;

        public override int OldStrengthReq => 20;
        public override int OldMinDamage => 10;
        public override int OldMaxDamage => 30;
        public override int OldSpeed => 30;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 70;
    }
}
