using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DeadWood : BaseReagent, ICommodity
{
    [Constructible]
    public DeadWood(int amount = 1) : base(0xF90, amount)
    {
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}