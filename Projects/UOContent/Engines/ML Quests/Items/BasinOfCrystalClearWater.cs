using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BasinOfCrystalClearWater : Item
{
    [Constructible]
    public BasinOfCrystalClearWater() : base(0x1008) => LootType = LootType.Blessed;

    public override int LabelNumber => 1075303; // Basin of Crystal-Clear Water

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
