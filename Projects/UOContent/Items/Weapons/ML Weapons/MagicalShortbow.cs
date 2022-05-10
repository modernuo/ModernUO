using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x2D2B, 0x2D1F)]
    [SerializationGenerator(0)]
    public partial class MagicalShortbow : BaseRanged
    {
        [Constructible]
        public MagicalShortbow() : base(0x2D2B) => Weight = 6.0;

        public override int EffectID => 0xF42;
        public override Type AmmoType => typeof(Arrow);
        public override Item Ammo => new Arrow();

        public override WeaponAbility PrimaryAbility => WeaponAbility.LightningArrow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.PsychicAttack;

        public override int AosStrengthReq => 45;
        public override int AosMinDamage => 9;
        public override int AosMaxDamage => 13;
        public override int AosSpeed => 38;
        public override float MlSpeed => 3.00f;

        public override int OldStrengthReq => 45;
        public override int OldMinDamage => 9;
        public override int OldMaxDamage => 13;
        public override int OldSpeed => 38;

        public override int DefMaxRange => 10;

        public override int InitMinHits => 41;
        public override int InitMaxHits => 90;
    }
}
