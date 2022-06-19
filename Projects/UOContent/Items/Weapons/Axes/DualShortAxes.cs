using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x8FD, 0x4068)]
    [SerializationGenerator(0)]
    public partial class DualShortAxes : BaseAxe
    {
        [Constructible]
        public DualShortAxes() : base(0x8FD)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;
        public override WeaponAbility SecondaryAbility => WeaponAbility.InfectiousStrike;
        public override int AosStrengthReq => 35;
        public override int AosMinDamage => 14;
        public override int AosMaxDamage => 17;
        public override float MlSpeed => 3.00f;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 110;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
    }
}
