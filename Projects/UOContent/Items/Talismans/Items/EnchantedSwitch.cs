using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EnchantedSwitch : Item
{
    [Constructible]
    public EnchantedSwitch() : base(0x2F5C) => Weight = 1.0;

    public override int LabelNumber => 1072893; // enchanted switch
}
