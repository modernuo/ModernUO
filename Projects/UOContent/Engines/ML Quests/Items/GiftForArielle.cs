using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GiftForArielle : BaseContainer
{
    [Constructible]
    public GiftForArielle() : base(0x1882) => Hue = 0x2C4;

    public override int LabelNumber => 1074356; // gift for arielle
    public override int DefaultGumpID => 0x41;

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
