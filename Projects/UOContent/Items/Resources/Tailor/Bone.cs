using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Bone : Item, ICommodity
{
    [Constructible]
    public Bone(int amount = 1) : base(0xf7e)
    {
        Stackable = true;
        Amount = amount;
        Weight = 1.0;
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}