using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class PumpkinScarecrow : Item
    {
        [Constructible]
        public PumpkinScarecrow()
            : base(Utility.RandomBool() ? 0x469B : 0x469C)
        {
        }

        public override int LabelNumber => 1096947;
    }
}
