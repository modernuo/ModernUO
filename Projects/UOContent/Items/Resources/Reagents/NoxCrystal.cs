using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class NoxCrystal : BaseReagent, ICommodity
    {
        [Constructible]
        public NoxCrystal(int amount = 1) : base(0xF8E, amount)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;
    }
}
