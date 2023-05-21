using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x11EA, 0x11EB)]
[SerializationGenerator(0, false)]
public partial class Sand : Item, ICommodity
{
    [Constructible]
    public Sand(int amount = 1) : base(0x11EA)
    {
        Stackable = Core.ML;
        Weight = 1.0;
    }

    public override int LabelNumber => 1044626; // sand
    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}
