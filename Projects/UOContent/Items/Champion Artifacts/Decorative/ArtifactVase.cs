namespace Server.Items
{
    public class ArtifactVase : Item
    {
        [Constructible]
        public ArtifactVase() : base(0x0B48)
        {
        }

        public ArtifactVase(Serial serial) : base(serial)
        {
        }

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
