using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SpidersSilk : BaseReagent, ICommodity
    {
        [Constructible]
        public SpidersSilk(int amount = 1) : base(0xF8D, amount)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;
    }
}
