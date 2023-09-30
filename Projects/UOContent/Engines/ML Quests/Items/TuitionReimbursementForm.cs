using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TuitionReimbursementForm : Item
{
    [Constructible]
    public TuitionReimbursementForm() : base(0xE3A) => LootType = LootType.Blessed;

    public override int LabelNumber => 1074610; // Tuition Reimbursement Form (in triplicate)

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
