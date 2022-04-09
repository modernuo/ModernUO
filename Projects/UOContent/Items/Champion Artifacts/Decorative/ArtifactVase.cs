using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ArtifactVase : Item
    {
        [Constructible]
        public ArtifactVase() : base(0x0B48)
        {
        }
    }
}
