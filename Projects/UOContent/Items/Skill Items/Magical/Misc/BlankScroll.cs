using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BlankScroll : Item, ICommodity
{
    [Constructible]
    public BlankScroll(int amount = 1) : base(0xEF3)
    {
        Stackable = true;
        Weight = 1.0;
        Amount = amount;
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => Core.ML;
}
