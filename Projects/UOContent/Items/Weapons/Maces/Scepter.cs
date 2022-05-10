using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x26BC, 0x26C6)]
    [SerializationGenerator(0, false)]
    public partial class Scepter : BaseBashing
    {
        [Constructible]
        public Scepter() : base(0x26BC) => Weight = 8.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;

        public override int AosStrengthReq => 40;
        public override int AosMinDamage => 14;
        public override int AosMaxDamage => 17;
        public override int AosSpeed => 30;
        public override float MlSpeed => 3.50f;

        public override int OldStrengthReq => 40;
        public override int OldMinDamage => 14;
        public override int OldMaxDamage => 17;
        public override int OldSpeed => 30;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 110;
    }
}
