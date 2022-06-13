using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class BatWing : BaseReagent, ICommodity
    {
        [Constructible]
        public BatWing(int amount = 1) : base(0xF78, amount)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;
    }
}
