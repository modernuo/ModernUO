namespace Server.Items
{
    public class VesperReefTiger : BaseFish
    {
        [Constructible]
        public VesperReefTiger() : base(0x3B08)
        {
        }

        public VesperReefTiger(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073836; // A Vesper Reef Tiger

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
