using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ReginasRing : SilverRing
{
    [Constructible]
    public ReginasRing() => LootType = LootType.Blessed;

    public override int LabelNumber => 1075305; // Regina's Ring

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
