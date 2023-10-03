using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SignedTuitionReimbursementForm : Item
{
    [Constructible]
    public SignedTuitionReimbursementForm() : base(0x14F0) => LootType = LootType.Blessed;

    public override int LabelNumber => 1074614; // Signed Tuition Reimbursement Form

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
