using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x906, 0x406F)]
    [TypeAlias("Server.Items.SerpentStoneStaff")]
    [SerializationGenerator(0)]
    public partial class SerpentstoneStaff : BaseStaff
    {
        [Constructible]
        public SerpentstoneStaff() : base(0x906)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Dismount;
        public override int AosStrengthReq => 35;
        public override int AosMinDamage => 16;
        public override int AosMaxDamage => 19;
        public override float MlSpeed => 3.50f;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 50;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
    }
}
