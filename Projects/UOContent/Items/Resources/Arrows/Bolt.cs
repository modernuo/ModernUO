using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Bolt : Item, ICommodity
{
    [Constructible]
    public Bolt(int amount = 1) : base(0x1BFB)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}
