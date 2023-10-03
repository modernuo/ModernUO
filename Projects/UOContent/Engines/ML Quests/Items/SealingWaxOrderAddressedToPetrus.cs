using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SealingWaxOrderAddressedToPetrus : Item
{
    [Constructible]
    public SealingWaxOrderAddressedToPetrus() : base(0xEBF) => LootType = LootType.Blessed;

    public override int LabelNumber => 1073132; // Sealing Wax Order addressed to Petrus

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
