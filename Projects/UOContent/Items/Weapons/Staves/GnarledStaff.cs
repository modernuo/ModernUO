namespace Server.Items
{
    [Flippable(0x13F8, 0x13F9)]
    public class GnarledStaff : BaseStaff
    {
        [Constructible]
        public GnarledStaff() : base(0x13F8) => Weight = 3.0;

        public GnarledStaff(Serial serial) : base(serial)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.ConcussionBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ParalyzingBlow;

        public override int AosStrengthReq => 20;
        public override int AosMinDamage => 15;
        public override int AosMaxDamage => 17;
        public override int AosSpeed => 33;
        public override float MlSpeed => 3.25f;

        public override int OldStrengthReq => 20;
        public override int OldMinDamage => 10;
        public override int OldMaxDamage => 30;
        public override int OldSpeed => 33;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 50;

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
