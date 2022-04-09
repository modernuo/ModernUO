using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class StrippedSosarianSwill : BaseFish
    {
        [Constructible]
        public StrippedSosarianSwill() : base(0x3B0A)
        {
        }

        public override int LabelNumber => 1074594; // Stripped Sosarian Swill
    }
}
