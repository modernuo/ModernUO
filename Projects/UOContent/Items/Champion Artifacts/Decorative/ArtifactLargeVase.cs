using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ArtifactLargeVase : Item
    {
        [Constructible]
        public ArtifactLargeVase() : base(0x0B47)
        {
        }
    }
}
