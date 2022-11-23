using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DaemonBlood : BaseReagent, ICommodity
{
    [Constructible]
    public DaemonBlood(int amount = 1) : base(0xF7D, amount)
    {
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}