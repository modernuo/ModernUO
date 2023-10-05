using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ReginasLetter : Item
{
    [Constructible]
    public ReginasLetter() : base(0x14ED) => LootType = LootType.Blessed;

    public override int LabelNumber => 1075306; // Regina's Letter

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
