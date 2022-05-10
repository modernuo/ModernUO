using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SulfurousAsh : BaseReagent, ICommodity
    {
        [Constructible]
        public SulfurousAsh(int amount = 1) : base(0xF8C, amount)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;
    }
}
