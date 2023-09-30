using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NotarizedApplication : Item
{
    [Constructible]
    public NotarizedApplication() : base(0x14EF) => LootType = LootType.Blessed;

    public override int LabelNumber => 1073135; // Notarized Application

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
