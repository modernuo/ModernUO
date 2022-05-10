using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class AlbinoFrog : BaseFish
    {
        [Constructible]
        public AlbinoFrog() : base(0x3B0D) => Hue = 0x47E;

        public override int LabelNumber => 1073824; // An Albino Frog
    }
}
