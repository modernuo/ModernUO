namespace Server.Items
{
<<<<<<< HEAD
    public class AcademicBooksArtifact : BaseDecorationArtifact
=======
    [Serializable(0)]
    public partial class AcademicBooksArtifact : BaseDecorationArtifact
>>>>>>> 990d151ef302b70bb21d4b3e94b8df73ad7c9ef8
    {
        public override int ArtifactRarity => 8;
        public override int LabelNumber => 1071202;  // academic books

<<<<<<< HEAD

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
=======
        [Constructible]
        public AcademicBooksArtifact() : base(0x1E25) => Hue = 2413;
>>>>>>> 990d151ef302b70bb21d4b3e94b8df73ad7c9ef8
    }
}
