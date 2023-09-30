using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PortraitOfTheBride : Item
{
    [Constructible]
    public PortraitOfTheBride() : base(0xE9F) => LootType = LootType.Blessed;

    public override int LabelNumber => 1075300; // Portrait of the Bride

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
