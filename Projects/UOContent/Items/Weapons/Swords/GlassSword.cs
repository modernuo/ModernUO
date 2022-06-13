using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x90C, 0x4073)]
    [SerializationGenerator(0)]
    public partial class GlassSword : BaseSword
    {
        [Constructible]
        public GlassSword() : base(0x90C) => Weight = 6.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;
        public override int AosStrengthReq => 20;
        public override int AosMinDamage => 11;
        public override int AosMaxDamage => 15;
        public override float MlSpeed => 2.75f;

        public override int DefHitSound => 0x23B;
        public override int DefMissSound => 0x23A;
        public override int InitMinHits => 31;
        public override int InitMaxHits => 90;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
    }
}
