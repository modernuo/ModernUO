using System;

namespace Server.Items
{
    [Flippable(0x26C2, 0x26CC)]
    public class CompositeBow : BaseRanged
    {
        [Constructible]
        public CompositeBow() : base(0x26C2) => Weight = 5.0;

        public CompositeBow(Serial serial) : base(serial)
        {
        }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
