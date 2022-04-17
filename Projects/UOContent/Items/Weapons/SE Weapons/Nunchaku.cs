using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x27AE, 0x27F9)]
    [SerializationGenerator(0, false)]
    public partial class Nunchaku : BaseBashing
    {
        [Constructible]
        public Nunchaku() : base(0x27AE) => Weight = 5.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.Block;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Feint;

        public override int AosStrengthReq => 15;
        public override int AosMinDamage => 11;
        public override int AosMaxDamage => 13;
        public override int AosSpeed => 47;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 15;
        public override int OldMinDamage => 11;
        public override int OldMaxDamage => 13;
        public override int OldSpeed => 47;

        public override int DefHitSound => 0x535;
        public override int DefMissSound => 0x239;

        public override int InitMinHits => 40;
        public override int InitMaxHits => 55;
    }
}
