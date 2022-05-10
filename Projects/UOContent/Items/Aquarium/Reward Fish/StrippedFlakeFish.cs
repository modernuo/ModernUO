using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class StrippedFlakeFish : BaseFish
    {
        [Constructible]
        public StrippedFlakeFish() : base(0x3B0A)
        {
        }

        public override int LabelNumber => 1074595; // Stripped Flake Fish
    }
}
