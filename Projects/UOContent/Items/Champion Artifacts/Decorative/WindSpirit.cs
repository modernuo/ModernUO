using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class WindSpirit : Item
    {
        [Constructible]
        public WindSpirit() : base(0x1F1F)
        {
        }

        public override int LabelNumber => 1094925; // Wind Spirit [Replica]
    }
}
