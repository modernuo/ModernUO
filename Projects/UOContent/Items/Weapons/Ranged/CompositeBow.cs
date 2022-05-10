using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x26C2, 0x26CC)]
    [SerializationGenerator(0, false)]
    public partial class CompositeBow : BaseRanged
    {
        [Constructible]
        public CompositeBow() : base(0x26C2) => Weight = 5.0;

        public override int EffectID => 0xF42;
        public override Type AmmoType => typeof(Arrow);
        public override Item Ammo => new Arrow();

        public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
        public override WeaponAbility SecondaryAbility => WeaponAbility.MovingShot;

        public override int AosStrengthReq => 45;
        public override int AosMinDamage => Core.ML ? 13 : 15;
        public override int AosMaxDamage => 17;
        public override int AosSpeed => 25;
        public override float MlSpeed => 4.00f;

        public override int OldStrengthReq => 45;
        public override int OldMinDamage => 15;
        public override int OldMaxDamage => 17;
        public override int OldSpeed => 25;

        public override int DefMaxRange => 10;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 70;

        public override WeaponAnimation DefAnimation => WeaponAnimation.ShootBow;
    }
}
