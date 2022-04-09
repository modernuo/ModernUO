using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SwampTile : Item
    {
        [Constructible]
        public SwampTile() : base(0x320D)
        {
        }
    }
}
