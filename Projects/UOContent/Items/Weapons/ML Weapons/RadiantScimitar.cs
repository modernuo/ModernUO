namespace Server.Items
{
    [Flippable(0x2D33, 0x2D27)]
    public class RadiantScimitar : BaseSword
    {
        [Constructible]
        public RadiantScimitar() : base(0x2D33) => Weight = 9.0;

        public RadiantScimitar(Serial serial) : base(serial)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.WhirlwindAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Bladeweave;

        public override int AosStrengthReq => 20;
        public override int AosMinDamage => 12;
        public override int AosMaxDamage => 14;
        public override int AosSpeed => 43;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 20;
        public override int OldMinDamage => 12;
        public override int OldMaxDamage => 14;
        public override int OldSpeed => 43;

        public override int DefHitSound => 0x23B;
        public override int DefMissSound => 0x239;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 60;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
