using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x13FD, 0x13FC)]
    [SerializationGenerator(0, false)]
    public partial class HeavyCrossbow : BaseRanged
    {
        [Constructible]
        public HeavyCrossbow() : base(0x13FD)
        {
            Weight = 9.0;
            Layer = Layer.TwoHanded;
        }

        public override int EffectID => 0x1BFE;
        public override Type AmmoType => typeof(Bolt);
        public override Item Ammo => new Bolt();

        public override WeaponAbility PrimaryAbility => WeaponAbility.MovingShot;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Dismount;

        public override int AosStrengthReq => 80;
        public override int AosMinDamage => Core.ML ? 20 : 19;
        public override int AosMaxDamage => Core.ML ? 24 : 20;
        public override int AosSpeed => 22;
        public override float MlSpeed => 5.00f;

        public override int OldStrengthReq => 40;
        public override int OldMinDamage => 11;
        public override int OldMaxDamage => 56;
        public override int OldSpeed => 10;

        public override int DefMaxRange => 8;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 100;
    }
}
