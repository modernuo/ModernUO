using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EnchantedSwitch : Item
{
    [Constructible]
    public EnchantedSwitch() : base(0x2F5C)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1072893; // enchanted switch
}
