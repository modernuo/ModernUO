namespace Server.Items
{
    public class AnOldNecklace : Necklace
    {
        [Constructible]
        public AnOldNecklace() => Hue = 0x222;

        public AnOldNecklace(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075525; // an old necklace

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
