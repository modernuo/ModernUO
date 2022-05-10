using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x27A6, 0x27F1)]
    [SerializationGenerator(0, false)]
    public partial class Tetsubo : BaseBashing
    {
        [Constructible]
        public Tetsubo() : base(0x27A6)
        {
            Weight = 8.0;
            Layer = Layer.TwoHanded;
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.FrenziedWhirlwind;
        public override WeaponAbility SecondaryAbility => WeaponAbility.CrushingBlow;

        public override int AosStrengthReq => 35;
        public override int AosMinDamage => 12;
        public override int AosMaxDamage => 14;
        public override int AosSpeed => 45;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 35;
        public override int OldMinDamage => 12;
        public override int OldMaxDamage => 14;
        public override int OldSpeed => 45;

        public override int DefHitSound => 0x233;
        public override int DefMissSound => 0x238;

        public override int InitMinHits => 60;
        public override int InitMaxHits => 65;

        public override WeaponAnimation DefAnimation => WeaponAnimation.Bash2H;
    }
}
