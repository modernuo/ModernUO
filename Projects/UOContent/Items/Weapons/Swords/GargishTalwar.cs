using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x908, 0x4075)]
    [SerializationGenerator(0)]
    public partial class GargishTalwar : BaseSword
    {
        [Constructible]
        public GargishTalwar() : base(0x908)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.WhirlwindAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Dismount;
        public override int AosStrengthReq => 40;
        public override int AosMinDamage => 16;
        public override int AosMaxDamage => 19;
        public override float MlSpeed => 3.50f;

        public override int DefHitSound => 0x237;
        public override int DefMissSound => 0x238;
        public override int InitMinHits => 31;
        public override int InitMaxHits => 80;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
    }
}
