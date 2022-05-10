using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x2D25, 0x2D31)]
    [SerializationGenerator(0)]
    public partial class WildStaff : BaseStaff
    {
        [Constructible]
        public WildStaff() : base(0x2D25) => Weight = 8.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.Block;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ForceOfNature;

        public override int AosStrengthReq => 15;
        public override int AosMinDamage => 10;
        public override int AosMaxDamage => 12;
        public override int AosSpeed => 48;
        public override float MlSpeed => 2.25f;

        public override int OldStrengthReq => 15;
        public override int OldMinDamage => 10;
        public override int OldMaxDamage => 12;
        public override int OldSpeed => 48;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 60;
    }
}
