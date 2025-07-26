using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TribalBerry : Item
{
    [Constructible]
    public TribalBerry(int amount = 1) : base(0x9D0)
    {
        Stackable = true;
        Amount = amount;
        Hue = 6;
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1040001; // tribal berry
}
