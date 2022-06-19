using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x27A5, 0x27F0)]
    [SerializationGenerator(0, false)]
    public partial class Yumi : BaseRanged
    {
        [Constructible]
        public Yumi() : base(0x27A5)
        {
            Weight = 9.0;
            Layer = Layer.TwoHanded;
        }

        public override int EffectID => 0xF42;
        public override Type AmmoType => typeof(Arrow);
        public override Item Ammo => new Arrow();

        public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorPierce;
        public override WeaponAbility SecondaryAbility => WeaponAbility.DoubleShot;

        public override int AosStrengthReq => 35;
        public override int AosMinDamage => Core.ML ? 16 : 18;
        public override int AosMaxDamage => 20;
        public override int AosSpeed => 25;
        public override float MlSpeed => 4.5f;

        public override int OldStrengthReq => 35;
        public override int OldMinDamage => 18;
        public override int OldMaxDamage => 20;
        public override int OldSpeed => 25;

        public override int DefMaxRange => 10;

        public override int InitMinHits => 55;
        public override int InitMaxHits => 60;

        public override WeaponAnimation DefAnimation => WeaponAnimation.ShootBow;
    }
}
