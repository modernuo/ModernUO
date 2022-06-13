using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Taffy : CandyCane
    {
        [Constructible]
        public Taffy(int amount = 1) : base(0x469D) => Stackable = true;

        public override int LabelNumber => 1096949; /* taffy */
    }
}
