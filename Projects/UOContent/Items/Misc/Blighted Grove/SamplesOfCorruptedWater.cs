namespace Server.Items
{
    public class SamplesOfCorruptedWater : Item
    {
        [Constructible]
        public SamplesOfCorruptedWater() : base(0xEFE) => LootType = LootType.Blessed;

        public SamplesOfCorruptedWater(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074999; // samples of corrupted water

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
