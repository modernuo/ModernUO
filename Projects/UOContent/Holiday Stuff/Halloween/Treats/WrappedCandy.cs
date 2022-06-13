using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class WrappedCandy : CandyCane
    {
        [Constructible]
        public WrappedCandy(int amount = 1) : base(0x469e) => Stackable = true;

        public override int LabelNumber => 1096950; /* wrapped candy */
    }
}
