using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpiritBottle : Item
{
    [Constructible]
    public SpiritBottle() : base(0xEFB) => LootType = LootType.Blessed;

    public override int LabelNumber => 1075283; // Spirit bottle

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
