using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TaintedTreeSample : Item // On OSI the base class is Kindling, and it's ignitable...
{
    [Constructible]
    public TaintedTreeSample() : base(0xDE2)
    {
        LootType = LootType.Blessed;
        Hue = 0x9D;
    }

    public override int LabelNumber => 1074997; // Tainted Tree Sample

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
