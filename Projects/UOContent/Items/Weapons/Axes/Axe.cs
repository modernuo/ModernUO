using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0xF49, 0xF4a)]
    [SerializationGenerator(0, false)]
    public partial class Axe : BaseAxe
    {
        [Constructible]
        public Axe() : base(0xF49) => Weight = 4.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Dismount;

        public override int AosStrengthReq => 35;
        public override int AosMinDamage => 14;
        public override int AosMaxDamage => 16;
        public override int AosSpeed => 37;
        public override float MlSpeed => 3.00f;

        public override int OldStrengthReq => 35;
        public override int OldMinDamage => 6;
        public override int OldMaxDamage => 33;
        public override int OldSpeed => 37;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 110;
    }
}
