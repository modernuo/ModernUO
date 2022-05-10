using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0xF43, 0xF44)]
    public partial class Hatchet : BaseAxe
    {
        [Constructible]
        public Hatchet() : base(0xF43) => Weight = 4.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

        public override int AosStrengthReq => 20;
        public override int AosMinDamage => 13;
        public override int AosMaxDamage => 15;
        public override int AosSpeed => 41;
        public override float MlSpeed => 2.75f;

        public override int OldStrengthReq => 15;
        public override int OldMinDamage => 2;
        public override int OldMaxDamage => 17;
        public override int OldSpeed => 40;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 80;
    }
}
