using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x13FF, 0x13FE)]
    [SerializationGenerator(0, false)]
    public partial class Katana : BaseSword
    {
        [Constructible]
        public Katana() : base(0x13FF) => Weight = 6.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ArmorIgnore;

        public override int AosStrengthReq => 25;
        public override int AosMinDamage => 11;
        public override int AosMaxDamage => 13;
        public override int AosSpeed => 46;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 10;
        public override int OldMinDamage => 5;
        public override int OldMaxDamage => 26;
        public override int OldSpeed => 58;

        public override int DefHitSound => 0x23B;
        public override int DefMissSound => 0x23A;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 90;
    }
}
