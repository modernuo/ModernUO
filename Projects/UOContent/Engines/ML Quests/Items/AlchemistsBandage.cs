using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AlchemistsBandage : Item
{
    [Constructible]
    public AlchemistsBandage() : base(0xE21)
    {
        LootType = LootType.Blessed;
        Hue = 0x482;
    }

    public override int LabelNumber => 1075452; // Alchemist's Bandage

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
