using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Bloodmoss : BaseReagent, ICommodity
    {
        [Constructible]
        public Bloodmoss(int amount = 1) : base(0xF7B, amount)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;
    }
}
