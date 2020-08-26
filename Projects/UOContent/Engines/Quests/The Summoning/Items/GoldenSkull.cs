namespace Server.Engines.Quests.Doom
{
    public class GoldenSkull : Item
    {
        [Constructible]
        public GoldenSkull() : base(Utility.Random(0x1AE2, 3))
        {
            Weight = 1.0;
            Hue = 0x8A5;
            LootType = LootType.Blessed;
        }

        public GoldenSkull(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061619; // a golden skull

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
