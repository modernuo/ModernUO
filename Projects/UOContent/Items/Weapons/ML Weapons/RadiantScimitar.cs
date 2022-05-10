using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x2D33, 0x2D27)]
    [SerializationGenerator(0)]
    public partial class RadiantScimitar : BaseSword
    {
        [Constructible]
        public RadiantScimitar() : base(0x2D33) => Weight = 9.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.WhirlwindAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Bladeweave;

        public override int AosStrengthReq => 20;
        public override int AosMinDamage => 12;
        public override int AosMaxDamage => 14;
        public override int AosSpeed => 43;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 20;
        public override int OldMinDamage => 12;
        public override int OldMaxDamage => 14;
        public override int OldSpeed => 43;

        public override int DefHitSound => 0x23B;
        public override int DefMissSound => 0x239;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 60;
    }
}
