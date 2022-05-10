using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class TrueRadiantScimitar : RadiantScimitar
    {
        [Constructible]
        public TrueRadiantScimitar() => Attributes.NightSight = 1;

        public override int LabelNumber => 1073541; // true radiant scimitar
    }
}
