using ModernUO.Serialization;

namespace Server.Factions;

[SerializationGenerator(0, false)]
public partial class FactionTrapRemovalKit : Item
{
    [EncodedInt]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _charges;

    [Constructible]
    public FactionTrapRemovalKit() : base(7867)
    {
        LootType = LootType.Blessed;
        _charges = 25;
    }

    public override int LabelNumber => 1041508; // a faction trap removal kit

    public void ConsumeCharge(Mobile consumer)
    {
        if (--Charges <= 0)
        {
            Delete();
            consumer?.SendLocalizedMessage(1042531); // You have used all of the parts in your trap removal kit.
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        // NOTE: OSI does not list uses remaining; intentional difference
        list.Add(1060584, Charges); // uses remaining: ~1_val~
    }
}
