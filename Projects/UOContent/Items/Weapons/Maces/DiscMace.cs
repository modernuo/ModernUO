using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x903, 0x406E)]
    [SerializationGenerator(0)]
    public partial class DiscMace : BaseBashing
    {
        [Constructible]
        public DiscMace() : base(0x903)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;
        public override int AosStrengthReq => 45;
        public override int AosMinDamage => 11;
        public override int AosMaxDamage => 15;
        public override float MlSpeed => 2.75f;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 110;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
    }
}
