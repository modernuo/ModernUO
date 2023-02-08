using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Shaft : Item, ICommodity
{
    [Constructible]
    public Shaft(int amount = 1) : base(0x1BD4)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}
