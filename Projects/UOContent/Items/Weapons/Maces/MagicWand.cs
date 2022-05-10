using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class MagicWand : BaseBashing
    {
        [Constructible]
        public MagicWand() : base(0xDF2) => Weight = 1.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.Dismount;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

        public override int AosStrengthReq => 5;
        public override int AosMinDamage => 9;
        public override int AosMaxDamage => 11;
        public override int AosSpeed => 40;
        public override float MlSpeed => 2.75f;

        public override int OldStrengthReq => 0;
        public override int OldMinDamage => 2;
        public override int OldMaxDamage => 6;
        public override int OldSpeed => 35;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 110;
    }
}
