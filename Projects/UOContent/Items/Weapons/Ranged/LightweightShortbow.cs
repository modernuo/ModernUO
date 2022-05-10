using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class LightweightShortbow : MagicalShortbow
    {
        [Constructible]
        public LightweightShortbow() => Balanced = true;

        public override int LabelNumber => 1073510; // lightweight shortbow
    }
}
