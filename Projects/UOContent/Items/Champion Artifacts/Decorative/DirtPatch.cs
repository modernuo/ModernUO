using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class DirtPatch : Item
    {
        [Constructible]
        public DirtPatch() : base(0x0913)
        {
        }
    }
}
