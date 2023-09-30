using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FragmentOfAMapDelivery : Item
{
    [Constructible]
    public FragmentOfAMapDelivery() : base(0x14ED) => LootType = LootType.Blessed;

    public override int LabelNumber => 1074533; // Fragment of a Map

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
