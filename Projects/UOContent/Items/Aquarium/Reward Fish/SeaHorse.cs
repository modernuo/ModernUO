using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SeaHorseFish : BaseFish
    {
        [Constructible]
        public SeaHorseFish() : base(0x3B10)
        {
        }

        public override int LabelNumber => 1074414; // A sea horse
    }
}
