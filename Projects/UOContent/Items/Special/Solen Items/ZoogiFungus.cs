using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class ZoogiFungus : Item, ICommodity
{
    [Constructible]
    public ZoogiFungus(int amount = 1) : base(0x26B7)
    {
        Stackable = true;
        Weight = 0.1;
        Amount = amount;
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => Core.ML;
}
