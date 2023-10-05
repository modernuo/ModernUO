using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SealedNotesForJamal : Item
{
    [Constructible]
    public SealedNotesForJamal() : base(0xEF9) => LootType = LootType.Blessed;

    public override int LabelNumber => 1074998; // Sealed Notes For Jamal
    public override double DefaultWeight => 1.0;

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
