using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class RedDartFish : BaseFish
    {
        [Constructible]
        public RedDartFish() : base(0x3B00)
        {
        }

        public override int LabelNumber => 1073834; // A Red Dart Fish
    }
}
