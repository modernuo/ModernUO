using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LavaTile : Item
    {
        [Constructible]
        public LavaTile() : base(0x12EE)
        {
        }
    }
}
