using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0xEC3, 0xEC2)]
    [SerializationGenerator(0, false)]
    public partial class Cleaver : BaseKnife
    {
        [Constructible]
        public Cleaver() : base(0xEC3) => Weight = 2.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.InfectiousStrike;

        public override int AosStrengthReq => 10;
        public override int AosMinDamage => 11;
        public override int AosMaxDamage => 13;
        public override int AosSpeed => 46;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 10;
        public override int OldMinDamage => 2;
        public override int OldMaxDamage => 13;
        public override int OldSpeed => 40;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 50;
    }
}
