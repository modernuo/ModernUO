using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x143D, 0x143C)]
    [SerializationGenerator(0, false)]
    public partial class HammerPick : BaseBashing
    {
        [Constructible]
        public HammerPick() : base(0x143D)
        {
            Weight = 9.0;
            Layer = Layer.OneHanded;
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
        public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;

        public override int AosStrengthReq => 45;
        public override int AosMinDamage => 15;
        public override int AosMaxDamage => 17;
        public override int AosSpeed => 28;
        public override float MlSpeed => 3.75f;

        public override int OldStrengthReq => 35;
        public override int OldMinDamage => 6;
        public override int OldMaxDamage => 33;
        public override int OldSpeed => 30;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 70;
    }
}
