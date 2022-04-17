using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x26C1, 0x26CB)]
    [SerializationGenerator(0, false)]
    public partial class CrescentBlade : BaseSword
    {
        [Constructible]
        public CrescentBlade() : base(0x26C1) => Weight = 1.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;
        public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;

        public override int AosStrengthReq => 55;
        public override int AosMinDamage => 11;
        public override int AosMaxDamage => 14;
        public override int AosSpeed => 47;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 55;
        public override int OldMinDamage => 11;
        public override int OldMaxDamage => 14;
        public override int OldSpeed => 47;

        public override int DefHitSound => 0x23B;
        public override int DefMissSound => 0x23A;

        public override int InitMinHits => 51;
        public override int InitMaxHits => 80;
    }
}
