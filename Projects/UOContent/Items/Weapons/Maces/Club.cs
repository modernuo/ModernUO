using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x13b4, 0x13b3)]
    [SerializationGenerator(0, false)]
    public partial class Club : BaseBashing
    {
        [Constructible]
        public Club() : base(0x13B4) => Weight = 9.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.ShadowStrike;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Dismount;

        public override int AosStrengthReq => 40;
        public override int AosMinDamage => 11;
        public override int AosMaxDamage => 13;
        public override int AosSpeed => 44;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 10;
        public override int OldMinDamage => 8;
        public override int OldMaxDamage => 24;
        public override int OldSpeed => 40;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 40;
    }
}
