using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class DragonsBlood : BaseReagent, ICommodity
    {
        [Constructible]
        public DragonsBlood(int amount = 1) : base(0x4077, amount)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => Core.ML;
    }
}
