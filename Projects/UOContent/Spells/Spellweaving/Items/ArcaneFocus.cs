using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ArcaneFocus : TransientItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _strengthBonus;

    [Constructible]
    public ArcaneFocus() : this(TimeSpan.FromHours(1), 1)
    {
    }

    [Constructible]
    public ArcaneFocus(int lifeSpan, int strengthBonus) : this(TimeSpan.FromSeconds(lifeSpan), strengthBonus)
    {
    }

    public ArcaneFocus(TimeSpan lifeSpan, int strengthBonus) : base(0x3155, lifeSpan)
    {
        LootType = LootType.Blessed;
        _strengthBonus = strengthBonus;
    }

    public override int LabelNumber => 1032629; // Arcane Focus

    public override TextDefinition InvalidTransferMessage => 1073480; // Your arcane focus disappears.
    public override bool Nontransferable => true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060485, _strengthBonus); // strength bonus ~1_val~
    }
}
