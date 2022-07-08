using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x905, 0x4070)]
    [SerializationGenerator(0)]
    public partial class GlassStaff : BaseStaff
    {
        [Constructible]
        public GlassStaff() : base(0x905) =>
            Weight = 4.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;
        public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;
        public override int AosStrengthReq => 20;
        public override int AosMinDamage => 11;
        public override int AosMaxDamage => 14;
        public override float MlSpeed => 2.25f;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 70;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
    }
}
