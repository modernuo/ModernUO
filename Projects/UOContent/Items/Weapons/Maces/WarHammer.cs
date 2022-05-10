using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x1439, 0x1438)]
    [SerializationGenerator(0, false)]
    public partial class WarHammer : BaseBashing
    {
        [Constructible]
        public WarHammer() : base(0x1439)
        {
            Weight = 10.0;
            Layer = Layer.TwoHanded;
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.WhirlwindAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.CrushingBlow;

        public override int AosStrengthReq => 95;
        public override int AosMinDamage => 17;
        public override int AosMaxDamage => 18;
        public override int AosSpeed => 28;
        public override float MlSpeed => 3.75f;

        public override int OldStrengthReq => 40;
        public override int OldMinDamage => 8;
        public override int OldMaxDamage => 36;
        public override int OldSpeed => 31;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 110;

        public override WeaponAnimation DefAnimation => WeaponAnimation.Bash2H;
    }
}
