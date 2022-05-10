using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x904, 0x406D)]
    [SerializationGenerator(0)]
    public partial class DualPointedSpear : BaseSpear
    {
        [Constructible]
        public DualPointedSpear() : base(0x904)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;
        public override int AosStrengthReq => 50;
        public override int AosMinDamage => 11;
        public override int AosMaxDamage => 14;
        public override float MlSpeed => 2.25f;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 80;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
    }
}
