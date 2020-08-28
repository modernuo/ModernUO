namespace Server.Items
{
    public class SpecialTreatForDrithen : Item
    {
        [Constructible]
        public SpecialTreatForDrithen() : base(0x21B)
        {
            LootType = LootType.Blessed;
            Hue = 0x489;
        }

        public SpecialTreatForDrithen(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074517; // Special Treat for Drithen

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
