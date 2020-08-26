namespace Server.Items
{
    [Flippable(0x315C, 0x315D)]
    public class HornOfTheDreadhorn : Item
    {
        [Constructible]
        public HornOfTheDreadhorn() : base(0x315C)
        {
        }

        public HornOfTheDreadhorn(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072089; // Horn of the Dread

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
