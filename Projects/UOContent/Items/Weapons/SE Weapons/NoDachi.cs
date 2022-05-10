using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x27A2, 0x27ED)]
    [SerializationGenerator(0, false)]
    public partial class NoDachi : BaseSword
    {
        [Constructible]
        public NoDachi() : base(0x27A2)
        {
            Weight = 10.0;
            Layer = Layer.TwoHanded;
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.RidingSwipe;

        public override int AosStrengthReq => 40;
        public override int AosMinDamage => 16;
        public override int AosMaxDamage => 18;
        public override int AosSpeed => 35;
        public override float MlSpeed => 3.50f;

        public override int OldStrengthReq => 40;
        public override int OldMinDamage => 16;
        public override int OldMaxDamage => 18;
        public override int OldSpeed => 35;

        public override int DefHitSound => 0x23B;
        public override int DefMissSound => 0x23A;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 90;
    }
}
