using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CompletedTuitionReimbursementForm : Item
{
    [Constructible]
    public CompletedTuitionReimbursementForm() : base(0x14F0) => LootType = LootType.Blessed;

    public override int LabelNumber => 1074625; // Completed Tuition Reimbursement Form

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
