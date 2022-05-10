using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x13B2, 0x13B1)]
    [SerializationGenerator(0, false)]
    public partial class Bow : BaseRanged
    {
        [Constructible]
        public Bow() : base(0x13B2)
        {
            Weight = 6.0;
            Layer = Layer.TwoHanded;
        }

        public override int EffectID => 0xF42;
        public override Type AmmoType => typeof(Arrow);
        public override Item Ammo => new Arrow();

        public override WeaponAbility PrimaryAbility => WeaponAbility.ParalyzingBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;

        public override int AosStrengthReq => 30;
        public override int AosMinDamage => Core.ML ? 15 : 16;
        public override int AosMaxDamage => Core.ML ? 19 : 18;
        public override int AosSpeed => 25;
        public override float MlSpeed => 4.25f;

        public override int OldStrengthReq => 20;
        public override int OldMinDamage => 9;
        public override int OldMaxDamage => 41;
        public override int OldSpeed => 20;

        public override int DefMaxRange => 10;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 60;

        public override WeaponAnimation DefAnimation => WeaponAnimation.ShootBow;
    }
}
