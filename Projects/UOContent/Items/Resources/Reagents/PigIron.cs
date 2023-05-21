using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PigIron : BaseReagent, ICommodity
{
    [Constructible]
    public PigIron(int amount = 1) : base(0xF8A, amount)
    {
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}