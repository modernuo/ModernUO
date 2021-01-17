namespace Server.Items
{
    public class AndrosGratitude : SmithHammer
    {
        [Constructible]
        public AndrosGratitude() : base(10) => LootType = LootType.Blessed;

        public AndrosGratitude(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075345; // Andros Gratitude

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // Version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
