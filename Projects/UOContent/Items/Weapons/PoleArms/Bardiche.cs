using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0xF4D, 0xF4E)]
    [SerializationGenerator(0, false)]
    public partial class Bardiche : BasePoleArm
    {
        [Constructible]
        public Bardiche() : base(0xF4D) => Weight = 7.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.ParalyzingBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Dismount;

        public override int AosStrengthReq => 45;
        public override int AosMinDamage => 17;
        public override int AosMaxDamage => 18;
        public override int AosSpeed => 28;
        public override float MlSpeed => 3.75f;

        public override int OldStrengthReq => 40;
        public override int OldMinDamage => 5;
        public override int OldMaxDamage => 43;
        public override int OldSpeed => 26;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 100;
    }
}
