using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x2D20, 0x2D2C)]
    [SerializationGenerator(0)]
    public partial class ElvenSpellblade : BaseKnife
    {
        [Constructible]
        public ElvenSpellblade() : base(0x2D20)
        {
            Weight = 5.0;
            Layer = Layer.TwoHanded;
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.PsychicAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.BleedAttack;

        public override int AosStrengthReq => 35;
        public override int AosMinDamage => 12;
        public override int AosMaxDamage => 14;
        public override int AosSpeed => 44;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 35;
        public override int OldMinDamage => 12;
        public override int OldMaxDamage => 14;
        public override int OldSpeed => 44;

        public override int DefMissSound => 0x239;

        public override int InitMinHits => 30; // TODO
        public override int InitMaxHits => 60; // TODO
    }
}
