namespace Server.Items
{
    public class EternallyCorruptTree : Item
    {
        [Constructible]
        public EternallyCorruptTree() : base(0x20FA) => Hue = Utility.RandomMinMax(0x899, 0x8B0);

        public EternallyCorruptTree(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072093; // Eternally Corrupt Tree

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
