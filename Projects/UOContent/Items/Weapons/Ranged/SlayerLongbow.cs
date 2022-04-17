using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SlayerLongbow : ElvenCompositeLongbow
    {
        [Constructible]
        public SlayerLongbow() => Slayer2 = (SlayerName)Utility.RandomMinMax(1, 27);

        public override int LabelNumber => 1073506; // slayer longbow
    }
}
