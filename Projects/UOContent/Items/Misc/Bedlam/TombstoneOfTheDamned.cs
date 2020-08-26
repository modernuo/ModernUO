namespace Server.Items
{
    public class TombstoneOfTheDamned : Item
    {
        [Constructible]
        public TombstoneOfTheDamned() : base(Utility.RandomMinMax(0xED7, 0xEDE))
        {
        }

        public TombstoneOfTheDamned(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072123; // Tombstone of the Damned

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
