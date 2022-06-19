using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x27A8, 0x27F3)]
    [SerializationGenerator(0, false)]
    public partial class Bokuto : BaseSword
    {
        [Constructible]
        public Bokuto() : base(0x27A8) => Weight = 7.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.Feint;
        public override WeaponAbility SecondaryAbility => WeaponAbility.NerveStrike;

        public override int AosStrengthReq => 20;
        public override int AosMinDamage => 9;
        public override int AosMaxDamage => 11;
        public override int AosSpeed => 53;
        public override float MlSpeed => 2.00f;

        public override int OldStrengthReq => 20;
        public override int OldMinDamage => 9;
        public override int OldMaxDamage => 11;
        public override int OldSpeed => 53;

        public override int DefHitSound => 0x536;
        public override int DefMissSound => 0x23A;

        public override int InitMinHits => 25;
        public override int InitMaxHits => 50;
    }
}
