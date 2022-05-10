using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0xF5E, 0xF5F)]
    [SerializationGenerator(0, false)]
    public partial class Broadsword : BaseSword
    {
        [Constructible]
        public Broadsword() : base(0xF5E) => Weight = 6.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ArmorIgnore;

        public override int AosStrengthReq => 30;
        public override int AosMinDamage => 14;
        public override int AosMaxDamage => 15;
        public override int AosSpeed => 33;
        public override float MlSpeed => 3.25f;

        public override int OldStrengthReq => 25;
        public override int OldMinDamage => 5;
        public override int OldMaxDamage => 29;
        public override int OldSpeed => 45;

        public override int DefHitSound => 0x237;
        public override int DefMissSound => 0x23A;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 100;
    }
}
