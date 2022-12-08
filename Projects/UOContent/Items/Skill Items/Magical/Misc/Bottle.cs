using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Bottle : Item, ICommodity
{
    [Constructible]
    public Bottle(int amount = 1) : base(0xF0E)
    {
        Stackable = true;
        Weight = 1.0;
        Amount = amount;
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => Core.ML;
}
