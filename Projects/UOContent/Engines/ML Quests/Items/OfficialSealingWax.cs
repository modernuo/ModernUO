using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class OfficialSealingWax : Item
{
    [Constructible]
    public OfficialSealingWax() : base(0x1426)
    {
        LootType = LootType.Blessed;
        Hue = 0x84;
    }

    public override int LabelNumber => 1072744; // Official Sealing Wax

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
