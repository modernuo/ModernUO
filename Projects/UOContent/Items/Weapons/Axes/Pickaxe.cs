using ModernUO.Serialization;
using Server.Engines.Harvest;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0xE86, 0xE85)]
    public partial class Pickaxe : BaseAxe, IUsesRemaining
    {
        [Constructible]
        public Pickaxe() : base(0xE86)
        {
            Weight = 11.0;
            UsesRemaining = 50;
            ShowUsesRemaining = true;
        }

        public override HarvestSystem HarvestSystem => Mining.System;

        public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

        public override int AosStrengthReq => 50;
        public override int AosMinDamage => 13;
        public override int AosMaxDamage => 15;
        public override int AosSpeed => 35;
        public override float MlSpeed => 3.00f;

        public override int OldStrengthReq => 25;
        public override int OldMinDamage => 1;
        public override int OldMaxDamage => 15;
        public override int OldSpeed => 35;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 60;

        public override WeaponAnimation DefAnimation => WeaponAnimation.Slash1H;
    }
}
