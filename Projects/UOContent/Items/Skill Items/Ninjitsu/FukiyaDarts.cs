using System;
using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FukiyaDarts : Item, ICraftable, INinjaAmmo
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _usesRemaining;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Poison _poison;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _poisonCharges;

    [Constructible]
    public FukiyaDarts(int amount = 1) : base(0x2806)
    {
        Weight = 1.0;
        _usesRemaining = amount;
    }

    public FukiyaDarts(Serial serial) : base(serial)
    {
    }

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    )
    {
        if (quality == 2)
        {
            UsesRemaining *= 2;
        }

        return quality;
    }

    bool IUsesRemaining.ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060584, _usesRemaining); // uses remaining: ~1_val~

        if (_poison != null && _poisonCharges > 0)
        {
            list.Add(1062412 + _poison.Level, _poisonCharges);
        }
    }
}
