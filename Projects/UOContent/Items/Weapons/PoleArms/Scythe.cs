using ModernUO.Serialization;
using Server.Engines.Harvest;

namespace Server.Items
{
    [Flippable(0x26BA, 0x26C4)]
    [SerializationGenerator(0, false)]
    public partial class Scythe : BasePoleArm
    {
        [Constructible]
        public Scythe() : base(0x26BA) => Weight = 5.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ParalyzingBlow;

        public override int AosStrengthReq => 45;
        public override int AosMinDamage => 15;
        public override int AosMaxDamage => 18;
        public override int AosSpeed => 32;
        public override float MlSpeed => 3.50f;

        public override int OldStrengthReq => 45;
        public override int OldMinDamage => 15;
        public override int OldMaxDamage => 18;
        public override int OldSpeed => 32;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 100;

        public override HarvestSystem HarvestSystem => null;
    }
}
