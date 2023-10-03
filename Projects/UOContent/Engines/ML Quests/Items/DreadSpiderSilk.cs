using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DreadSpiderSilk : Item
{
    [Constructible]
    public DreadSpiderSilk() : base(0xDF8)
    {
        LootType = LootType.Blessed;
        Hue = 0x481;
    }

    public override int LabelNumber => 1075319; // Dread Spider Silk

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
