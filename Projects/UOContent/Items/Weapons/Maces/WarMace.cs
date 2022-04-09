using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x1407, 0x1406)]
    [SerializationGenerator(0, false)]
    public partial class WarMace : BaseBashing
    {
        [Constructible]
        public WarMace() : base(0x1407) => Weight = 17.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;

        public override int AosStrengthReq => 80;
        public override int AosMinDamage => 16;
        public override int AosMaxDamage => 17;
        public override int AosSpeed => 26;
        public override float MlSpeed => 4.00f;

        public override int OldStrengthReq => 30;
        public override int OldMinDamage => 10;
        public override int OldMaxDamage => 30;
        public override int OldSpeed => 32;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 110;
    }
}
