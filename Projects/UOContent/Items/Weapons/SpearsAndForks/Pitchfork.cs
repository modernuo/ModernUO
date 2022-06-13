using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0xE87, 0xE88)]
    [SerializationGenerator(0, false)]
    public partial class Pitchfork : BaseSpear
    {
        [Constructible]
        public Pitchfork() : base(0xE87) => Weight = 11.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Dismount;

        public override int AosStrengthReq => 55;
        public override int AosMinDamage => 13;
        public override int AosMaxDamage => 14;
        public override int AosSpeed => 43;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 15;
        public override int OldMinDamage => 4;
        public override int OldMaxDamage => 16;
        public override int OldSpeed => 45;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 60;
    }
}
