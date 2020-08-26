namespace Server.Items
{
    [Flippable(0x2D32, 0x2D26)]
    public class RuneBlade : BaseSword
    {
        [Constructible]
        public RuneBlade() : base(0x2D32)
        {
            Weight = 7.0;
            Layer = Layer.TwoHanded;
        }

        public RuneBlade(Serial serial) : base(serial)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.Disarm;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Bladeweave;

        public override int AosStrengthReq => 30;
        public override int AosMinDamage => 15;
        public override int AosMaxDamage => 17;
        public override int AosSpeed => 35;
        public override float MlSpeed => 3.00f;

        public override int OldStrengthReq => 30;
        public override int OldMinDamage => 15;
        public override int OldMaxDamage => 17;
        public override int OldSpeed => 35;

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
