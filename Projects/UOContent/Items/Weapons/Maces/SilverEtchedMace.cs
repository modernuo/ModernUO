using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SilverEtchedMace : DiamondMace
    {
        [Constructible]
        public SilverEtchedMace() => Slayer = SlayerName.Exorcism;

        public override int LabelNumber => 1073532; // silver-etched mace
    }
}
