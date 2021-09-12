namespace Server.Items
{
    public class AcademicBooksArtifact : BaseDecorationArtifact
    {
        public override int ArtifactRarity => 8;
        public override int LabelNumber => 1071202;  // academic books


        [Constructible]
        public AcademicBooksArtifact() : base(0x1E25)
        {
            Hue = 2413;
        }

        public AcademicBooksArtifact(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadEncodedInt();
        }
    }
}
