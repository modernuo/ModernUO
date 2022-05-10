using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class GraveDust : BaseReagent, ICommodity
    {
        [Constructible]
        public GraveDust(int amount = 1) : base(0xF8F, amount)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;
    }
}
