using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class AcademicBooksArtifact : BaseDecorationArtifact
    {
        public override int ArtifactRarity => 8;
        public override int LabelNumber => 1071202;  // academic books

        [Constructible]
        public AcademicBooksArtifact() : base(0x1E25) => Hue = 2413;
    }
}
