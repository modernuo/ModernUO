using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class WaterTile : Item
    {
        [Constructible]
        public WaterTile() : base(0x346E)
        {
        }
    }
}
