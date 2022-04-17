using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class BrineShrimp : BaseFish
    {
        [Constructible]
        public BrineShrimp() : base(0x3B11)
        {
        }

        public override int LabelNumber => 1074415; // Brine shrimp
    }
}
